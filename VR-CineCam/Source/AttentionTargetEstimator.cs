using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SignalExtractor;

public enum AttentionTargetType
{
    None,
    Enemy,
    Interactable,
    Object,
    Environment,
    Unknown
}

[System.Serializable]
public struct AttentionTargetState
{
    public GameObject targetObject;
    public AttentionTargetType targetType;
    public float confidence;
    public Vector3 targetPosition;
    public bool isValid;
}

public static class TargetSemanticUtility
{
    public static AttentionTargetType Classify(GameObject obj)
    {
        if (obj == null) return AttentionTargetType.None;

        if (obj.CompareTag("Enemy")) return AttentionTargetType.Enemy;
        if (obj.CompareTag("Interactable")) return AttentionTargetType.Interactable;
        if (obj.CompareTag("Object")) return AttentionTargetType.Object;
        if (obj.CompareTag("Environment")) return AttentionTargetType.Environment;

        return AttentionTargetType.Unknown;
    }
}

[DefaultExecutionOrder(-40)]
public class AttentionTargetEstimator : MonoBehaviour
{
    [Header("References")]
    public SignalExtractor signalExtractor;
    public SituationEstimator situationEstimator;

    [Header("Stability")]
    [Range(0f, 1f)] public float switchThreshold = 0.05f;
    [Range(0f, 2f)] public float minHoldTime = 0.5f;
    [Tooltip("새 타겟이 이 시간(초) 동안 연속으로 우세해야 전환. 뭉친 적 사이 에임 흔들림 방지.")]
    [Range(0f, 2f)] public float dwellTime = 0.3f;

    [Header("Fallback Raycast")]
    public Transform centerEyeAnchor;
    public float gazeRayDistance = 20f;
    public LayerMask gazeRayMask = ~0;

    [Header("Intent Filters")]
    [Tooltip("에임 레이캐스트가 타겟으로 인정되기 위한 최소 트리거 값.\n" +
             "이 값 미만이면 재그립 등 의도치 않은 레이캐스트를 무시.")]
    [Range(0f, 1f)] public float aimTriggerThreshold = 0.15f;

    [Tooltip("컨트롤러 각속도(rad/s)가 이 값 이상이면 흔들림으로 판단해 에임 신뢰도 감소.\n" +
             "0 = 비활성화. 권장: 2~4.")]
    public float aimShakeMaxAngular = 3.0f;

    [Header("Gaze Ray Filter")]
    [Tooltip("Gaze ray로 Interactable/Object를 어텐션 타겟으로 인정하는 최소 그립 값. 0 = 필터 없음.")]
    [Range(0f, 1f)] public float gazeInteractableGripThreshold = 0.3f;

    [Header("Sustained Aim Targeting")]
    [Tooltip("타겟이 없을 때 이 시간(초) 이상 같은 적을 겨누면 트리거 없이도 타겟으로 인정.")]
    public float sustainedAimTime = 2.5f;

    [Header("Soft Aim (콜라이더 경계 흔들림 방지)")]
    [Tooltip("레이캐스트 미스여도 에임으로 인정하는 최대 각도 (도). 클수록 느슨하게 감지.")]
    [Range(3f, 45f)] public float softAimConeAngle = 15f;
    [Tooltip("소프트 에임 탐색 최대 거리")]
    public float softAimRange = 40f;
    [Tooltip("Enemy 레이어 마스크 (Inspector에서 Enemy 레이어 선택)")]
    public LayerMask enemyLayerMask;

    [Header("Ablation")]
    [Tooltip("실험용: true로 설정하면 의도 추정 없이 항상 가장 가까운 적을 타겟으로 선택 (NoIntention 조건).")]
    public bool forceNearestEnemy = false;

    [Header("Output")]
    public AttentionTargetState CurrentAttention;

    private AttentionTargetState _current;
    private float _lastSwitchTime = -999f;

    private AttentionTargetState _dwellCandidate;
    private float _dwellStartTime = -999f;

    private GameObject _sustainedAimTarget;
    private float _sustainedAimStartTime = -999f;

    private void Update()
    {
        if (signalExtractor == null) return;

        AttentionTargetState best = forceNearestEnemy
            ? EstimateNearestEnemy()
            : EstimateBestTarget();

        // 현재 타겟이 죽었으면 hold time 없이 즉시 해제
        // → SmoothDamp 속도 오버슈트로 인한 카메라 출렁임 방지
        if (_current.isValid && _current.targetObject != null &&
            _current.targetObject.CompareTag("Dead"))
        {
            _current = default;
            _lastSwitchTime = Time.time;
            _dwellCandidate = default;
        }

        bool shouldSwitch = false;

        if (!_current.isValid && best.isValid)
        {
            shouldSwitch = true;
        }
        else if (_current.isValid && !best.isValid)
        {
            if (Time.time - _lastSwitchTime > minHoldTime)
                shouldSwitch = true;
        }
        else if (_current.isValid && best.isValid)
        {
            if (_current.targetObject != best.targetObject)
            {
                float scoreDiff = best.confidence - _current.confidence;

                if (scoreDiff >= switchThreshold)
                {
                    if (_dwellCandidate.targetObject != best.targetObject)
                    {
                        // 새 후보 등장 → 드웰 타이머 시작
                        _dwellCandidate = best;
                        _dwellStartTime = Time.time;
                    }
                    else if (Time.time - _dwellStartTime >= dwellTime &&
                             Time.time - _lastSwitchTime > minHoldTime)
                    {
                        // 드웰 시간 동안 꾸준히 우세 → 전환 승인
                        shouldSwitch = true;
                    }
                }
                else
                {
                    // 점수 우위가 사라지면 드웰 리셋
                    _dwellCandidate = default;
                }
            }
            else
            {
                _current = best;
                _dwellCandidate = default;
            }
        }

        if (shouldSwitch)
        {
            _current = best;
            _lastSwitchTime = Time.time;
            _dwellCandidate = default;
        }

        CurrentAttention = _current;
    }

    private AttentionTargetState EstimateBestTarget()
    {
        VRSignalFrame f = signalExtractor.CurrentFrame;

        AttentionTargetState best = new AttentionTargetState
        {
            targetObject = null,
            targetType = AttentionTargetType.None,
            confidence = 0f,
            targetPosition = Vector3.zero,
            isValid = false
        };

        // 컨트롤러 흔들림 정도 (0=완전 안정, 1=최대 흔들림)
        float shake = aimShakeMaxAngular > 0f
            ? Mathf.Clamp01(f.rightHand.localAngularVelocity.magnitude / aimShakeMaxAngular)
            : 0f;
        float stabilityScale = 1f - shake;

        // 1) Aim ray: 트리거 미달 시 무시 (의도치 않은 레이캐스트 방지)
        if (f.rightHand.rayHit.hasHit &&
            f.rightHand.rayHit.hitObject != null &&
            f.rightHand.triggerValue >= aimTriggerThreshold)
        {
            var candidate = BuildCandidateFromRayHit(
                f.rightHand.rayHit.hitObject,
                f.rightHand.rayHit.hitPoint,
                f.rightHand.rayHit.distance,
                isAim: true,
                isGaze: false,
                f);

            // 흔들림이 클수록 신뢰도 감소 (근접 적 간 목표 흔들림 방지)
            candidate.confidence = Mathf.Clamp01(candidate.confidence * stabilityScale);

            if (candidate.confidence > best.confidence)
                best = candidate;
        }

        // 1b) 왼손 ray: NPC(Interactable) 조준 감지 — 트리거 문턱 없음
        //     왼손으로 NPC를 가리키는 것만으로 attention target 등록 → Side 샷 선제 전환
        if (f.leftHand.rayHit.hasHit && f.leftHand.rayHit.hitObject != null)
        {
            var leftCandidate = BuildCandidateFromRayHit(
                f.leftHand.rayHit.hitObject,
                f.leftHand.rayHit.hitPoint,
                f.leftHand.rayHit.distance,
                isAim: true,
                isGaze: false,
                f,
                useLeftHandGrip: true);

            if (leftCandidate.confidence > best.confidence)
                best = leftCandidate;
        }

        // 2) Gaze ray 후보
        RaycastHit hit;
        if (centerEyeAnchor != null &&
            Physics.Raycast(centerEyeAnchor.position, centerEyeAnchor.forward,
                out hit, gazeRayDistance, gazeRayMask, QueryTriggerInteraction.Ignore))
        {
            var candidate = BuildCandidateFromRayHit(
                hit.collider.gameObject,
                hit.point,
                hit.distance,
                isAim: false,
                isGaze: true,
                f);

            if (candidate.confidence > best.confidence)
                best = candidate;
        }

        // 3) Soft aim: 레이캐스트 미스여도 컨트롤러 방향과 적 방향의 각도가
        //    softAimConeAngle 이내면 연속 점수로 후보 생성.
        //    직접 레이캐스트(aimFeature=1.0)보다 최대값이 낮아 직접 히트가 항상 우선.
        AttentionTargetState softAimCandidate = FindSoftAimCandidate(f);
        if (softAimCandidate.isValid)
        {
            // 소프트 에임도 흔들림 필터 적용
            softAimCandidate.confidence = Mathf.Clamp01(softAimCandidate.confidence * stabilityScale);
        }
        if (softAimCandidate.confidence > best.confidence)
            best = softAimCandidate;

        // 4) Sustained aim: 타겟이 없을 때 일정 시간 이상 겨누면 트리거 없이도 타겟 인정
        AttentionTargetState sustainedCandidate = GetSustainedAimCandidate(f);
        if (sustainedCandidate.isValid && sustainedCandidate.confidence > best.confidence)
            best = sustainedCandidate;

        return best;
    }

    private AttentionTargetState BuildCandidateFromRayHit(
        GameObject obj,
        Vector3 hitPoint,
        float distance,
        bool isAim,
        bool isGaze,
        VRSignalFrame f,
        bool useLeftHandGrip = false)
    {
        obj = ResolveTargetRoot(obj);

        AttentionTargetType type = TargetSemanticUtility.Classify(obj);

        if (!IsCameraRelevantTarget(type))
        {
            return new AttentionTargetState
            {
                targetObject = null,
                targetType = AttentionTargetType.None,
                confidence = 0f,
                targetPosition = Vector3.zero,
                isValid = false
            };
        }

        // Gaze ray로 감지된 Interactable/Object는 최소 그립 값 필요 (흰 구체 등 의도치 않은 선택 방지)
        if (isGaze && !isAim &&
            (type == AttentionTargetType.Interactable || type == AttentionTargetType.Object) &&
            f.rightHand.gripValue < gazeInteractableGripThreshold)
        {
            return new AttentionTargetState { isValid = false };
        }

        // 1) feature���� 0~1 ������ ����
        float aimFeature = isAim ? 1f : 0f;
        float gazeFeature = isGaze ? 1f : 0f;

        float distFeature = Mathf.Clamp01(1f - (distance / Mathf.Max(gazeRayDistance, 0.01f)));

        float combatFeature = 0f;
        float interactionFeature = 0f;

        if (situationEstimator != null)
        {
            if (type == AttentionTargetType.Enemy)
            {
                combatFeature = situationEstimator.combatIntensity;
            }
            else if (type == AttentionTargetType.Interactable || type == AttentionTargetType.Object)
            {
                interactionFeature = situationEstimator.interactionIntensity;
            }
        }

        float triggerFeature = Mathf.Clamp01(f.rightHand.triggerValue);
        float gripFeature = useLeftHandGrip
            ? Mathf.Clamp01(f.leftHand.gripValue)
            : Mathf.Clamp01(f.rightHand.gripValue);

        // 2) ����ġ ����
        float wAim = 0.30f;
        float wGaze = 0.15f;
        float wDistance = 0.15f;
        float wCombat = 0.20f;
        float wInteraction = 0.10f;
        float wTrigger = 0.05f;
        float wGrip = 0.05f;

        // 3) Ÿ�Ժ��� trigger / grip ����
        float weightedSum = 0f;
        float totalWeight = 0f;

        weightedSum += aimFeature * wAim;
        totalWeight += wAim;

        weightedSum += gazeFeature * wGaze;
        totalWeight += wGaze;

        weightedSum += distFeature * wDistance;
        totalWeight += wDistance;

        weightedSum += combatFeature * wCombat;
        totalWeight += wCombat;

        weightedSum += interactionFeature * wInteraction;
        totalWeight += wInteraction;

        if (type == AttentionTargetType.Enemy)
        {
            weightedSum += triggerFeature * wTrigger;
            totalWeight += wTrigger;
        }

        if (type == AttentionTargetType.Interactable || type == AttentionTargetType.Object)
        {
            weightedSum += gripFeature * wGrip;
            totalWeight += wGrip;
        }

        float score = totalWeight > 0f ? weightedSum / totalWeight : 0f;

        // 4) ���� ���� Ÿ�� bias�� �߰�
        if (type == AttentionTargetType.Enemy)
            score += 0.03f;

        if (isAim && type == AttentionTargetType.Enemy)
            score += 0.03f;

        // 현재 타겟 유지 bias (hysteresis)
        if (_current.isValid && _current.targetObject != null && obj == _current.targetObject)
        {
            score += 0.02f;
        }

        score = Mathf.Clamp01(score);

        return new AttentionTargetState
        {
            targetObject = obj,
            targetType = type,
            confidence = score,
            targetPosition = GetProperTargetPosition(obj),
            isValid = obj != null
        };
    }

    private bool IsCameraRelevantTarget(AttentionTargetType type)
    {
        return type == AttentionTargetType.Enemy ||
               type == AttentionTargetType.Interactable ||
               type == AttentionTargetType.Object;
    }

    // 타겟이 없을 때 같은 적을 sustainedAimTime 이상 겨누면 타겟으로 인정
    private AttentionTargetState GetSustainedAimCandidate(VRSignalFrame f)
    {
        // 이미 타겟이 있으면 타이머 리셋 후 패스
        if (_current.isValid)
        {
            _sustainedAimTarget = null;
            _sustainedAimStartTime = -999f;
            return default;
        }

        // 소프트 에임 콘 안에서 가장 정중앙에 가까운 적 탐색
        Vector3 origin = f.rightHand.position;
        Vector3 aimDir = f.rightHand.forward;

        GameObject aimed = null;
        float bestAngle = softAimConeAngle;

        Collider[] cols = Physics.OverlapSphere(origin, softAimRange, enemyLayerMask);
        foreach (var col in cols)
        {
            GameObject root = ResolveTargetRoot(col.gameObject);
            if (root == null || !root.CompareTag("Enemy")) continue;

            float angle = Vector3.Angle(aimDir, root.transform.position - origin);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                aimed = root;
            }
        }

        if (aimed == null)
        {
            _sustainedAimTarget = null;
            _sustainedAimStartTime = -999f;
            return default;
        }

        if (!HasLineOfSight(origin, aimed))
        {
            _sustainedAimTarget = null;
            _sustainedAimStartTime = -999f;
            return default;
        }

        // 다른 적으로 바뀌면 타이머 재시작
        if (_sustainedAimTarget != aimed)
        {
            _sustainedAimTarget = aimed;
            _sustainedAimStartTime = Time.time;
            return default;
        }

        // 같은 적을 계속 겨누는 중 - 시간 확인
        if (Time.time - _sustainedAimStartTime < sustainedAimTime)
            return default;

        // sustainedAimTime 이상 겨눔 → 타겟 후보 생성
        EnemyBodyReference bodyRef = aimed.GetComponentInParent<EnemyBodyReference>();
        Vector3 targetPos = bodyRef != null && bodyRef.lookTarget != null
            ? bodyRef.lookTarget.position
            : aimed.transform.position;

        return new AttentionTargetState
        {
            targetObject = aimed,
            targetType = AttentionTargetType.Enemy,
            confidence = 0.4f,
            targetPosition = targetPos,
            isValid = true
        };
    }

    private AttentionTargetState FindSoftAimCandidate(VRSignalFrame f)
    {
        Vector3 origin = f.rightHand.position;
        Vector3 aimDir = f.rightHand.forward;

        AttentionTargetState best = new AttentionTargetState { isValid = false };

        Collider[] cols = Physics.OverlapSphere(origin, softAimRange, enemyLayerMask);

        foreach (var col in cols)
        {
            GameObject root = ResolveTargetRoot(col.gameObject);
            if (root == null || !root.CompareTag("Enemy")) continue;

            Vector3 toTarget = root.transform.position - origin;
            float dist = toTarget.magnitude;
            if (dist < 0.01f) continue;

            float angle = Vector3.Angle(aimDir, toTarget);
            if (angle > softAimConeAngle) continue;

            if (!HasLineOfSight(origin, root)) continue;

            // 각도 0° → aimFeature 0.9 (직접 히트 1.0보다 살짝 낮게 상한)
            float aimFeature = (1f - angle / softAimConeAngle) * 0.9f;
            float distFeature = Mathf.Clamp01(1f - dist / Mathf.Max(softAimRange, 0.01f));
            float combatFeature = situationEstimator != null ? situationEstimator.combatIntensity : 0f;
            float triggerFeature = Mathf.Clamp01(f.rightHand.triggerValue);

            float wAim = 0.30f, wDist = 0.15f, wCombat = 0.20f, wTrigger = 0.05f;
            float totalW = wAim + wDist + wCombat + wTrigger;
            float score = (aimFeature * wAim + distFeature * wDist + combatFeature * wCombat + triggerFeature * wTrigger) / totalW;
            score += 0.03f; // enemy bias
            if (aimFeature > 0.5f) score += 0.03f; // aim enemy bias

            // 현재 타겟과 동일하면 유지 선호 (hysteresis)
            if (_current.isValid && _current.targetObject == root)
                score += switchThreshold;

            score = Mathf.Clamp01(score);

            if (score > best.confidence)
            {
                EnemyBodyReference bodyRef = root.GetComponent<EnemyBodyReference>();
                Vector3 targetPos = bodyRef != null && bodyRef.lookTarget != null
                    ? bodyRef.lookTarget.position
                    : root.transform.position;

                best = new AttentionTargetState
                {
                    targetObject = root,
                    targetType = AttentionTargetType.Enemy,
                    confidence = score,
                    targetPosition = targetPos,
                    isValid = true
                };
            }
        }

        return best;
    }

    private AttentionTargetState EstimateNearestEnemy()
    {
        if (signalExtractor == null) return default;

        Vector3 origin = signalExtractor.CurrentFrame.playerRootPosition;
        Collider[] cols = Physics.OverlapSphere(origin, softAimRange, enemyLayerMask);

        GameObject nearest = null;
        float bestDist = float.MaxValue;

        foreach (var col in cols)
        {
            GameObject root = ResolveTargetRoot(col.gameObject);
            if (root == null || !root.CompareTag("Enemy")) continue;

            float dist = Vector3.Distance(origin, root.transform.position);
            if (dist < bestDist)
            {
                bestDist = dist;
                nearest = root;
            }
        }

        if (nearest == null) return default;

        return new AttentionTargetState
        {
            targetObject = nearest,
            targetType = AttentionTargetType.Enemy,
            confidence = 1f,
            targetPosition = GetProperTargetPosition(nearest),
            isValid = true
        };
    }

    private Vector3 GetProperTargetPosition(GameObject obj)
    {
        // 1) EnemyBodyReference.lookTarget (Chest/Spine 등 적절한 높이의 본)
        EnemyBodyReference bodyRef = obj.GetComponentInParent<EnemyBodyReference>();
        if (bodyRef != null && bodyRef.lookTarget != null)
            return bodyRef.lookTarget.position;

        // 2) Humanoid Animator Head → Chest 순 탐색
        Animator anim = obj.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            Transform head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head != null) return head.position;
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (chest != null) return chest.position;
        }

        // 3) 루트 위치 + 1.2m (발 위치 대신 대략 가슴 높이)
        return obj.transform.position + Vector3.up * 1.2f;
    }

    private bool HasLineOfSight(Vector3 origin, GameObject target)
    {
        EnemyBodyReference bodyRef = target.GetComponentInParent<EnemyBodyReference>();
        Vector3 targetPos = bodyRef != null && bodyRef.lookTarget != null
            ? bodyRef.lookTarget.position
            : target.transform.position + Vector3.up * 1.2f;

        Vector3 dir = targetPos - origin;
        float dist = dir.magnitude;
        if (dist < 0.1f) return true;

        if (Physics.Raycast(origin, dir / dist, out RaycastHit hit, dist,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.gameObject == target ||
                   hit.collider.transform.IsChildOf(target.transform);
        }
        return true;
    }

    private GameObject ResolveTargetRoot(GameObject obj)
    {
        if (obj == null) return null;

        // 계층 중 가장 위에 있는 Enemy/Interactable/Object 태그 오브젝트를 루트로 반환.
        // (예: Body 콜라이더가 맞아도 EnemyBodyReference가 있는 Enemy Sample 루트를 반환)
        GameObject topmost = null;
        Transform t = obj.transform;
        while (t != null)
        {
            if (t.CompareTag("Enemy") || t.CompareTag("Interactable") || t.CompareTag("Object"))
                topmost = t.gameObject;
            t = t.parent;
        }

        return topmost != null ? topmost : obj;
    }
}
