using System.Collections.Generic;
using UnityEngine;

public enum CameraShotType
{
    None,
    OverShoulderLeft,
    OverShoulderRight,
    SideLeft,
    SideRight,
    WideBack
}

[System.Serializable]
public struct CameraCandidate
{
    public CameraShotType shotType;
    public Vector3 position;
    public Vector3 lookAt;
    public float baseWeight;
    public bool isValid;
}

[DefaultExecutionOrder(-30)]
public class CameraCandidateGenerator : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Transform playerHead;
    public AttentionTargetEstimator attentionTargetEstimator;
    public SituationEstimator situationEstimator;

    [Header("Common Settings")]
    public float minTargetDistance = 0.5f;
    public Vector3 worldUp = Vector3.up;

    [Header("Reference Offsets")]
    public Vector3 playerReferenceOffset = new Vector3(0f, 1.4f, 0f);
    public Vector3 targetReferenceOffset = new Vector3(0f, 0.0f, 0f);

    [Header("Over Shoulder")]
    public float otsBackDistance = 3.0f;
    public float otsSideDistance = 1.5f;
    public float otsHeight = 1.6f;

    [Header("Side Shot")]
    public float sideDistance = 4.0f;
    public float sideHeight = 1.8f;

    [Header("Wide Back")]
    public float wideBackDistance = 6.0f;
    public float wideBackHeight = 1.5f;

    [Header("Adaptive Distance")]
    public bool useAdaptiveDistance = true;
    public float adaptiveStartDistance = 4.0f;
    public float otsDistanceScale = 0.40f;
    public float sideDistanceScale = 0.55f;
    public float wideDistanceScale = 0.80f;

    [Header("Adaptive Weight Bias")]
    public bool useDistanceWeightBias = true;
    public float wideBiasScale = 0.25f;
    public float otsPenaltyScale = 0.15f;

    [Header("Exploration Forward Smoothing")]
    [Tooltip("탐색 중 카메라 방향 기준이 바뀌는 데 걸리는 시간 (초)")]
    public float explorationForwardSmoothTime = 0.5f;
    [Tooltip("이 각도 이하의 고개 회전은 탐색 카메라 위치에 반영하지 않음")]
    [Range(0f, 60f)] public float explorationForwardDeadZone = 25f;

    [Header("Target Position Smoothing")]
    [Tooltip("어텐션 타겟이 바뀔 때 카메라 축이 서서히 돌아가는 시간 (초)")]
    public float targetPositionSmoothTime = 0.6f;

    [Header("Target Loss Fade")]
    [Tooltip("타겟이 사라진 후 액션 샷이 서서히 사라지는 시간 (초). 이 구간 동안 탐색 샷과 블렌드됨.")]
    public float targetLossFadeDuration = 1.5f;

    [Header("Debug Logging")]
    [Tooltip("타겟 소실/복귀, WideBack lookAt 급변 시 로그 출력")]
    public bool enableCandidateLog = true;

    [Header("Output")]
    public List<CameraCandidate> currentCandidates = new List<CameraCandidate>();
    public IReadOnlyList<CameraCandidate> CurrentCandidates => currentCandidates;

    private Vector3 _smoothedExploreForward;
    private Vector3 _exploreTargetForward;
    private float _exploreAngleVelocity;
    private bool _exploreForwardInitialized;

    private Vector3 _smoothedTargetPos;
    private Vector3 _targetPosVelocity;
    private bool _targetPosInitialized;
    private Vector3 _playerPosAtTargetLoss;

    private float _targetLostTime = -999f;
    private bool _wasHasTarget = false;
    private Vector3 _prevWideLookAt;
    private bool _prevWideLookAtInitialized = false;

    private void Update()
    {
        GenerateCandidates();
    }

    public void GenerateCandidates()
    {
        currentCandidates.Clear();

        if (playerRoot == null || attentionTargetEstimator == null)
            return;

        AttentionTargetState attention = attentionTargetEstimator.CurrentAttention;
        bool hasTarget = attention.isValid && attention.targetObject != null;

        Vector3 P = playerHead != null ? playerHead.position : playerRoot.position + playerReferenceOffset;
        Vector3 forwardRef = playerHead != null ? playerHead.forward : playerRoot.forward;

        if (hasTarget != _wasHasTarget && enableCandidateLog)
        {
            if (!hasTarget)
                Debug.Log($"[CamGen] 타겟 소실 — smoothedTargetPos={_smoothedTargetPos:F2} playerPos={P:F2} t={Time.time:F3}");
            else
                Debug.Log($"[CamGen] 타겟 획득 — rawTarget={attention.targetPosition:F2} t={Time.time:F3}");
        }
        _wasHasTarget = hasTarget;

        if (hasTarget)
        {
            // 타겟이 있는 동안 매 프레임 갱신 → 소실 시점부터 카운트 시작
            _targetLostTime = Time.time;

            Vector3 rawT = attention.targetPosition + targetReferenceOffset;

            if (!_targetPosInitialized)
            {
                _smoothedTargetPos = rawT;
                _targetPosInitialized = true;
            }
            _smoothedTargetPos = Vector3.SmoothDamp(
                _smoothedTargetPos, rawT, ref _targetPosVelocity, targetPositionSmoothTime);

            _playerPosAtTargetLoss = P;
            GenerateActionCandidates(P, _smoothedTargetPos, forwardRef, 1f);
            return;
        }

        // ── 타겟 없음 ──────────────────────────────────────────
        float timeSinceLost = Time.time - _targetLostTime;
        bool inFadeOut = _targetPosInitialized && timeSinceLost < targetLossFadeDuration;

        // 타겟 소실 즉시 velocity 초기화 → fadeOut 중 새 타겟 획득 시 오버슈트 방지
        _targetPosVelocity = Vector3.zero;

        // 탐색 샷 변수 준비 (아래에서 마지막으로 추가됨 → CameraAnchorUpdater에서 우선 적용)
        float explorationVal = situationEstimator != null ? situationEstimator.explorationIntensity : 0f;
        Vector3 dFlat = GetSmoothedExploreForward(forwardRef);
        // lookAt을 플레이어 가까이 유지해서 탐색 중에도 플레이어가 프레임 안에 있도록 함
        Vector3 lookAheadPoint = P + dFlat * wideBackDistance * 0.15f;

        if (inFadeOut)
        {
            // 마지막 타겟 위치 기반 액션 샷을 서서히 페이드아웃
            // P 대신 소실 시점의 플레이어 위치를 사용 → OTS s벡터가 현재 머리 움직임에 반응하지 않도록
            float fadeFactor = Mathf.SmoothStep(1f, 0f, timeSinceLost / targetLossFadeDuration);
            if (enableCandidateLog && timeSinceLost < Time.deltaTime * 2f)
                Debug.Log($"[CamGen] fadeOut 시작 — fadeDuration={targetLossFadeDuration:F1}s playerAtLoss={_playerPosAtTargetLoss:F2} smoothedTarget={_smoothedTargetPos:F2} t={Time.time:F3}");
            GenerateActionCandidates(_playerPosAtTargetLoss, _smoothedTargetPos, forwardRef, fadeFactor);
        }
        else
        {
            // 타겟 소실 확정 시 속도 초기화 → 다음 타겟 획득 때 오버슈트 방지
            _targetPosVelocity = Vector3.zero;
            _targetPosInitialized = false;
        }

        // 탐색 WideBack을 마지막에 추가 → fadeOut 중 액션 WideBack보다 나중에 처리되어
        // CameraAnchorUpdater가 탐색 방향(전방)으로 앵커를 유지. 죽은 적 위치로 줌인되는 현상 방지.
        Vector3 widePos = P - dFlat * wideBackDistance + worldUp * wideBackHeight;
        if (enableCandidateLog && _prevWideLookAtInitialized)
        {
            float lookAtDelta = Vector3.Angle(_prevWideLookAt - widePos, lookAheadPoint - widePos);
            if (lookAtDelta > 30f)
                Debug.Log($"[CamGen] WideBack lookAt 급변 {lookAtDelta:F1}° — " +
                    $"pos={widePos:F2} lookAt={lookAheadPoint:F2} dFlat={dFlat:F2} inFadeOut={inFadeOut} t={Time.time:F3}");
        }
        _prevWideLookAt = lookAheadPoint;
        _prevWideLookAtInitialized = true;

        AddCandidate(new CameraCandidate
        {
            shotType = CameraShotType.WideBack,
            position = widePos,
            lookAt = lookAheadPoint,
            baseWeight = 0.7f + 0.5f * explorationVal,
            isValid = true
        });
    }

    // 액션 샷 6종 생성. weightMult: 0~1 (페이드아웃 시 감소)
    private void GenerateActionCandidates(Vector3 P, Vector3 T, Vector3 forwardRef, float weightMult)
    {
        float dist = Vector3.Distance(P, T);
        if (dist < minTargetDistance)
        {
            T = P + forwardRef * minTargetDistance;
            dist = minTargetDistance;
        }

        Vector3 d = (T - P).normalized;
        Vector3 s = Vector3.Cross(worldUp, d).normalized;
        Vector3 M = (P + T) * 0.5f;

        float combat      = situationEstimator != null ? situationEstimator.combatIntensity    : 0f;
        float interaction = situationEstimator != null ? situationEstimator.interactionIntensity : 0f;
        float exploration = situationEstimator != null ? situationEstimator.explorationIntensity : 0f;

        float extraDistance = useAdaptiveDistance && dist > adaptiveStartDistance
            ? dist - adaptiveStartDistance : 0f;

        float adaptiveOTSBack       = otsBackDistance      + extraDistance * otsDistanceScale;
        float adaptiveSideDistance  = sideDistance     + extraDistance * sideDistanceScale;
        float adaptiveWideBack      = wideBackDistance + extraDistance * wideDistanceScale;

        float normalizedFar = useDistanceWeightBias
            ? Mathf.Clamp01((dist - adaptiveStartDistance) / 6f) : 0f;

        float otsDistancePenalty = normalizedFar * otsPenaltyScale;
        float wideDistanceBonus  = normalizedFar * wideBiasScale;

        AddCandidate(new CameraCandidate
        {
            shotType   = CameraShotType.OverShoulderLeft,
            position   = P - d * adaptiveOTSBack - s * otsSideDistance + worldUp * otsHeight,
            lookAt     = Vector3.Lerp(P, T, 0.65f),
            baseWeight = (Mathf.Lerp(1.1f, 1.4f, combat) - otsDistancePenalty) * weightMult,
            isValid    = true
        });

        AddCandidate(new CameraCandidate
        {
            shotType   = CameraShotType.OverShoulderRight,
            position   = P - d * adaptiveOTSBack + s * otsSideDistance + worldUp * otsHeight,
            lookAt     = Vector3.Lerp(P, T, 0.65f),
            baseWeight = (Mathf.Lerp(1.1f, 1.4f, combat) - otsDistancePenalty) * weightMult,
            isValid    = true
        });

        AddCandidate(new CameraCandidate
        {
            shotType   = CameraShotType.SideLeft,
            position   = M - s * adaptiveSideDistance + worldUp * sideHeight,
            lookAt     = M,
            baseWeight = (0.9f + 0.3f * combat + 0.4f * interaction) * weightMult,
            isValid    = true
        });

        AddCandidate(new CameraCandidate
        {
            shotType   = CameraShotType.SideRight,
            position   = M + s * adaptiveSideDistance + worldUp * sideHeight,
            lookAt     = M,
            baseWeight = (0.9f + 0.3f * combat + 0.4f * interaction) * weightMult,
            isValid    = true
        });

        AddCandidate(new CameraCandidate
        {
            shotType   = CameraShotType.WideBack,
            position   = P - d * adaptiveWideBack + worldUp * wideBackHeight,
            lookAt     = M,
            baseWeight = (0.7f + 0.5f * exploration + wideDistanceBonus) * weightMult,
            isValid    = true
        });

    }

    private Vector3 GetSmoothedExploreForward(Vector3 rawForward)
    {
        Vector3 flat = new Vector3(rawForward.x, 0f, rawForward.z);
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        flat.Normalize();

        if (!_exploreForwardInitialized)
        {
            _smoothedExploreForward = flat;
            _exploreTargetForward   = flat;
            _exploreForwardInitialized = true;
            return _smoothedExploreForward;
        }

        if (Vector3.Angle(_exploreTargetForward, flat) > explorationForwardDeadZone)
            _exploreTargetForward = flat;

        // 벡터 SmoothDamp 대신 각도 기반 보간:
        // 180° 전환 시 보간 도중 벡터가 (0,0,0)을 지나며 카메라가 순간 붕괴하는 현상 방지
        float currentAngle = Mathf.Atan2(_smoothedExploreForward.x, _smoothedExploreForward.z) * Mathf.Rad2Deg;
        float targetAngle  = Mathf.Atan2(_exploreTargetForward.x,   _exploreTargetForward.z)   * Mathf.Rad2Deg;
        float smoothAngle  = Mathf.SmoothDampAngle(currentAngle, targetAngle, ref _exploreAngleVelocity, explorationForwardSmoothTime);
        _smoothedExploreForward = new Vector3(Mathf.Sin(smoothAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(smoothAngle * Mathf.Deg2Rad));

        return _smoothedExploreForward;
    }

    private void AddCandidate(CameraCandidate candidate)
    {
        currentCandidates.Add(candidate);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (currentCandidates == null) return;
        foreach (var c in currentCandidates)
        {
            if (!c.isValid) continue;
            Gizmos.DrawSphere(c.position, 0.15f);
            Gizmos.DrawLine(c.position, c.lookAt);
        }
    }
#endif
}
