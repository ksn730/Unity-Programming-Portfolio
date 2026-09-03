using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class TargetGroupUpdater : MonoBehaviour
{
    [Header("References")]
    public CinemachineTargetGroup targetGroup;
    public Transform playerRoot;
    public Transform playerHead;
    public AttentionTargetEstimator attentionEstimator;

    [Header("Nearby Settings")]
    public float nearbyRadius = 5f;
    public int maxNearbyTargets = 2;

    [Header("Weights")]
    public float playerWeight = 2f;
    public float targetWeight = 2f;
    public float nearbyWeight = 0.3f;

    [Header("Exploration Look-Ahead")]
    public float lookAheadDistance = 8f;
    public float lookAheadWeight = 2f;

    [Header("Temporal Stabilization")]
    [Tooltip("살아있는 타겟이 attention에서 잠시 벗어났을 때 TargetGroup에 유지하는 시간 (초)")]
    public float targetGroupGraceTime = 4.0f;
    [Tooltip("적 사망 직후 frozenCentroid를 playerHead 방향으로 슬라이드하는 시간 (초). CameraDirector.targetLossGraceTime과 맞추면 전환 직전까지 자연스럽게 시선이 이동.")]
    public float deathFreezeTime = 0.5f;
    [Tooltip("deathFreezeTime 이후 frozenCentroid weight를 0으로 페이드아웃하는 시간 (초). 2번째 줌아웃 방지.")]
    public float deathFadeOutDuration = 0.4f;
    [Tooltip("새 타겟 획득 시 weight를 0→targetWeight로 올리는 시간 (초). 타겟 획득 순간 줌아웃 방지.")]
    public float targetIntroTime = 0.25f;

    [Header("Look-ahead Smoothing")]
    [Tooltip("룩어헤드 방향 전환에 걸리는 시간 (초). 스냅턴 충격 완화.")]
    public float lookAheadSmoothTime = 0.35f;
    [Tooltip("이 각도 이하의 고개 회전은 카메라가 무시. VR 소폭 회전 잡음 제거.")]
    [Range(0f, 60f)] public float lookAheadDeadZoneAngle = 25f;

    private Transform _lookAheadTransform;
    private Transform _frozenCentroidTransform;

    private Vector3 _cachedCentroid;
    private float   _cachedGroupRadius = 1f;
    private bool    _hasCachedCentroid;
    private bool    _centroidFrozen;

    private GameObject          _lastValidTargetObject;
    private AttentionTargetType _lastValidTargetType = AttentionTargetType.None;
    private Vector3             _lastValidTargetPosition;
    private Vector3             _lastLookTargetPosition;
    private float               _lastValidTargetTime = -999f;

    private bool _frozenEndCaptured;

    // 새 타겟 획득 시 weight 서서히 증가 → TargetGroup 바운딩 스피어 점진적 확장
    private GameObject _currentIntroObject;
    private float      _introWeight;

    private Vector3 _smoothedLookAheadDir;
    private Vector3 _lookAheadTargetDir;
    private float   _lookAheadAngleVelocity;
    private bool    _lookAheadDirInitialized;

    private void Awake()
    {
        var go = new GameObject("_LookAheadPoint");
        go.transform.SetParent(transform);
        _lookAheadTransform = go.transform;

        var fc = new GameObject("_FrozenCentroid");
        fc.transform.SetParent(transform);
        _frozenCentroidTransform = fc.transform;
    }

    private void LateUpdate()
    {
        if (targetGroup == null || playerRoot == null || attentionEstimator == null)
            return;
        UpdateTargetGroup();
    }

    void UpdateTargetGroup()
    {
        var attention = attentionEstimator.CurrentAttention;
        bool hasCurrentAttention = attention.isValid && attention.targetObject != null
            && IsStillValidTarget(attention.targetObject, attention.targetType);

        if (hasCurrentAttention)
        {
            _lastValidTargetObject   = attention.targetObject;
            _lastValidTargetType     = attention.targetType;
            _lastValidTargetPosition = attention.targetPosition;
            _lastValidTargetTime     = Time.time;

            // lookTarget 본의 실제 위치를 저장 → 사망 전환 시 frozenCentroid 위치 일치용
            Transform lt = ResolveLookTargetTransform(attention.targetObject, attention.targetType);
            _lastLookTargetPosition = lt != null ? lt.position : attention.targetPosition;
        }

        // stable target 결정
        GameObject stableTargetObject = null;
        AttentionTargetType stableTargetType = AttentionTargetType.None;
        Vector3 stableTargetPosition = Vector3.zero;
        bool enemyDied = false;

        if (hasCurrentAttention)
        {
            stableTargetObject   = attention.targetObject;
            stableTargetType     = attention.targetType;
            stableTargetPosition = attention.targetPosition;
        }
        else
        {
            float elapsed = Time.time - _lastValidTargetTime;
            if (elapsed <= targetGroupGraceTime && _lastValidTargetObject != null)
            {
                if (IsStillValidTarget(_lastValidTargetObject, _lastValidTargetType))
                {
                    stableTargetObject   = _lastValidTargetObject;
                    stableTargetType     = _lastValidTargetType;
                    stableTargetPosition = _lastValidTargetPosition;
                }
                else
                {
                    enemyDied = true;
                }
            }
        }

        var targets = new List<CinemachineTargetGroup.Target>();

        if (enemyDied)
        {
            _centroidFrozen = false;
            float timeSinceDeath = Time.time - _lastValidTargetTime;

            if (!_frozenEndCaptured)
            {
                // lookTarget 본 위치로 고정 → 이전 프레임과 바운딩 스피어 크기 유지 → 1번째 FOV 점프 방지
                _frozenCentroidTransform.position = _lastLookTargetPosition;
                _frozenEndCaptured = true;
            }

            targets.Add(new CinemachineTargetGroup.Target
            {
                target = playerHead,
                weight = playerWeight,
                radius = 1f
            });

            // deathFreezeTime: 풀 weight 유지. 이후 deathFadeOutDuration 동안 서서히 0으로 페이드아웃
            // → 갑자기 frozenCentroid 제거 시 발생하는 2번째 FOV 점프 방지
            float frozenWeight;
            if (timeSinceDeath < deathFreezeTime)
            {
                frozenWeight = targetWeight;
            }
            else
            {
                float t = (timeSinceDeath - deathFreezeTime) / Mathf.Max(deathFadeOutDuration, 0.01f);
                frozenWeight = Mathf.Lerp(targetWeight, 0f, Mathf.Clamp01(t));
            }

            if (frozenWeight > 0.05f)
            {
                targets.Add(new CinemachineTargetGroup.Target
                {
                    target = _frozenCentroidTransform,
                    weight = frozenWeight,
                    radius = 1f
                });
            }
        }
        else if (stableTargetObject != null)
        {
            _centroidFrozen    = false;
            _frozenEndCaptured = false;

            // 새 타겟 획득 시 targetIntroTime 동안 weight 0→targetWeight 증가
            // → 바운딩 스피어 점진적 확장 → GroupComposer FOV 점프 방지
            if (_currentIntroObject != stableTargetObject)
            {
                _currentIntroObject = stableTargetObject;
                _introWeight = 0f;
            }
            _introWeight = Mathf.Min(_introWeight + Time.deltaTime / Mathf.Max(targetIntroTime, 0.01f), 1f);
            float effectiveTargetWeight = targetWeight * _introWeight;

            targets.Add(new CinemachineTargetGroup.Target
            {
                target = playerHead,
                weight = playerWeight,
                radius = 1f
            });

            Transform mainTarget;
            if (hasCurrentAttention)
            {
                // 어텐션 활성: live 뼈 사용
                mainTarget = ResolveLookTargetTransform(stableTargetObject, stableTargetType);
            }
            else
            {
                // grace period: ragdoll 추적 방지 + 이전 프레임 lookTarget 위치로 고정 → FOV 점프 방지
                _frozenCentroidTransform.position = _lastLookTargetPosition;
                mainTarget = _frozenCentroidTransform;
            }

            targets.Add(new CinemachineTargetGroup.Target
            {
                target = mainTarget != null ? mainTarget : stableTargetObject.transform,
                weight = effectiveTargetWeight,
                radius = 1f
            });

            // 주변 타겟: 어텐션 활성일 때만 탐색 (ragdoll 주변을 탐색하지 않음)
            if (hasCurrentAttention)
            {
                Collider[] hits = Physics.OverlapSphere(stableTargetPosition, nearbyRadius);
                int count = 0;
                foreach (var hit in hits)
                {
                    if (count >= maxNearbyTargets) break;
                    GameObject obj = hit.gameObject;
                    if (obj == stableTargetObject) continue;
                    if (obj == playerRoot.gameObject) continue;
                    if (!obj.CompareTag("Enemy") && !obj.CompareTag("Object")) continue;
                    Transform nearbyTransform = ResolveLookTargetTransform(
                        obj,
                        obj.CompareTag("Enemy") ? AttentionTargetType.Enemy : AttentionTargetType.Object
                    );
                    if (nearbyTransform == null) continue;
                    targets.Add(new CinemachineTargetGroup.Target
                    {
                        target = nearbyTransform,
                        weight = nearbyWeight * _introWeight,
                        radius = 1f
                    });
                    count++;
                }
            }

            _cachedCentroid    = ComputeWeightedCentroid(targets);
            _cachedGroupRadius = ComputeGroupRadius(targets, _cachedCentroid);
            _hasCachedCentroid = true;
        }
        else
        {
            // 탐색 중: 플레이어 + 앞 지점
            _centroidFrozen    = false;
            _frozenEndCaptured = false;
            _hasCachedCentroid = false;

            targets.Add(new CinemachineTargetGroup.Target
            {
                target = playerHead,
                weight = playerWeight,
                radius = 1f
            });

            if (playerHead != null && _lookAheadTransform != null)
            {
                Vector3 forwardFlat = new Vector3(playerHead.forward.x, 0f, playerHead.forward.z);
                if (forwardFlat.sqrMagnitude < 0.001f) forwardFlat = Vector3.forward;
                forwardFlat.Normalize();

                if (!_lookAheadDirInitialized)
                {
                    _smoothedLookAheadDir    = forwardFlat;
                    _lookAheadTargetDir      = forwardFlat;
                    _lookAheadDirInitialized = true;
                }

                float angleDiff = Vector3.Angle(_lookAheadTargetDir, forwardFlat);
                if (angleDiff > lookAheadDeadZoneAngle)
                    _lookAheadTargetDir = forwardFlat;

                // 각도 기반 보간: 180° 전환 시 Vector3.SmoothDamp가 (0,0,0)을 지나며
                // lookAheadTransform이 playerHead 위치로 붕괴 → anchor 중간 지점과 겹쳐 카메라가
                // 정 아래를 향하는 현상 방지 (CameraCandidateGenerator와 동일한 패턴)
                float currentAngle = Mathf.Atan2(_smoothedLookAheadDir.x, _smoothedLookAheadDir.z) * Mathf.Rad2Deg;
                float targetAngle  = Mathf.Atan2(_lookAheadTargetDir.x,   _lookAheadTargetDir.z)   * Mathf.Rad2Deg;
                float smoothAngle  = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref _lookAheadAngleVelocity, lookAheadSmoothTime);
                _smoothedLookAheadDir = new Vector3(Mathf.Sin(smoothAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(smoothAngle * Mathf.Deg2Rad));

                _lookAheadTransform.position =
                    playerHead.position + _smoothedLookAheadDir * lookAheadDistance;

                targets.Add(new CinemachineTargetGroup.Target
                {
                    target = _lookAheadTransform,
                    weight = lookAheadWeight,
                    radius = 0.5f
                });
            }
        }

        targetGroup.m_Targets = targets.ToArray();
    }

    private Vector3 ComputeWeightedCentroid(List<CinemachineTargetGroup.Target> targets)
    {
        float totalWeight = 0f;
        Vector3 sum = Vector3.zero;
        foreach (var t in targets)
        {
            if (t.target == null) continue;
            sum += t.target.position * t.weight;
            totalWeight += t.weight;
        }
        return totalWeight > 0f ? sum / totalWeight : Vector3.zero;
    }

    private float ComputeGroupRadius(List<CinemachineTargetGroup.Target> targets, Vector3 centroid)
    {
        float maxR = 0f;
        foreach (var t in targets)
        {
            if (t.target == null) continue;
            float d = Vector3.Distance(t.target.position, centroid) + t.radius;
            if (d > maxR) maxR = d;
        }
        return Mathf.Max(maxR, 0.5f);
    }

    private bool IsStillValidTarget(GameObject obj, AttentionTargetType type)
    {
        if (obj == null) return false;
        if (type == AttentionTargetType.Enemy)       return obj.CompareTag("Enemy");
        if (type == AttentionTargetType.Interactable) return obj.CompareTag("Interactable");
        return obj.CompareTag("Object");
    }

    private Transform ResolveLookTargetTransform(GameObject obj, AttentionTargetType type)
    {
        if (obj == null) return null;

        if (type == AttentionTargetType.Enemy)
        {
            EnemyBodyReference bodyRef = obj.GetComponentInParent<EnemyBodyReference>();
            if (bodyRef != null && bodyRef.lookTarget != null)
                return bodyRef.lookTarget;
            return obj.transform;
        }

        return obj.transform;
    }
}
