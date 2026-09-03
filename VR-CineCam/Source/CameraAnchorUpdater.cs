using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShotAnchorBinding
{
    public CameraShotType shotType;
    public Transform anchor;
}

public class CameraAnchorUpdater : MonoBehaviour
{
    [Header("References")]
    public CameraCandidateGenerator candidateGenerator;

    [Header("Anchor Bindings")]
    public List<ShotAnchorBinding> shotAnchorBindings = new List<ShotAnchorBinding>();

    [Header("Settings")]
    public bool updateRotation = true;
    public Vector3 upVector = Vector3.up;

    [Header("Damping")]
    [Tooltip("활성 샷의 위치 댐핑. 낮을수록 타겟 변경 시 카메라가 천천히 이동 (요동 방지).")]
    public float activePositionDamping = 2.5f;
    [Tooltip("비활성 샷의 위치 댐핑. 높을수록 활성화될 때 이미 올바른 위치에 있음.")]
    public float inactivePositionDamping = 10f;
    [Tooltip("활성 샷의 회전 댐핑.")]
    public float activeRotationDamping = 8f;
    [Tooltip("비활성 샷의 회전 댐핑. 높을수록 활성화될 때 이미 올바른 방향을 보고 있음.")]
    public float inactiveRotationDamping = 25f;

    [Header("Jump Damping")]
    [Tooltip("활성 앵커가 이 거리(m) 이상 이동해야 할 때 느린 댐핑 적용. 타겟 전환 시 순간 충격 완화.")]
    public float largeJumpThreshold = 1.5f;
    [Tooltip("대형 이동 시 적용할 느린 댐핑. 낮을수록 더 천천히 이동.")]
    public float postJumpDamping = 1.0f;

    [Header("References")]
    public CameraDirector cameraDirector;

    [Header("Debug Logging")]
    [Tooltip("이 각도(도) 이상 회전이 한 프레임에 발생하면 글리치 로그 출력")]
    public float glitchRotThreshold = 20f;
    [Tooltip("로그 출력 활성화 여부 — 글리치 재현 시에만 켬")]
    public bool enableGlitchLog = true;

    private Dictionary<CameraShotType, Transform> _anchorMap;
    private Dictionary<CameraShotType, Quaternion> _prevAnchorRot = new Dictionary<CameraShotType, Quaternion>();

    private void Awake()
    {
        BuildAnchorMap();
    }

    private void LateUpdate()
    {
        if (candidateGenerator == null) return;

        IReadOnlyList<CameraCandidate> candidates = candidateGenerator.CurrentCandidates;
        if (candidates == null || candidates.Count == 0) return;

        // 같은 shotType의 candidate가 여러 개일 경우(fadeOut 중 액션+탐색 WideBack 공존)
        // 마지막 항목만 적용한다. 두 번 Slerp되면 매 프레임 진동(glitch)이 발생하기 때문.
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].isValid) continue;
            // 같은 shotType이 뒤에 또 있으면 이번 것은 건너뜀
            bool hasLater = false;
            for (int j = i + 1; j < candidates.Count; j++)
            {
                if (candidates[j].isValid && candidates[j].shotType == candidates[i].shotType)
                {
                    hasLater = true;
                    break;
                }
            }
            if (!hasLater)
                ApplyCandidateToAnchor(candidates[i]);
        }
    }

    private void ApplyCandidateToAnchor(CameraCandidate candidate)
    {
        if (!candidate.isValid) return;
        if (_anchorMap == null) return;
        if (!_anchorMap.ContainsKey(candidate.shotType)) return;

        Transform anchor = _anchorMap[candidate.shotType];
        if (anchor == null) return;

        bool isActiveShot = cameraDirector != null && candidate.shotType == cameraDirector.currentShotType;
        bool targetLost   = cameraDirector != null && !cameraDirector.hasAttentionNow;
        bool inGrace      = cameraDirector != null && cameraDirector.isInTargetLossGrace;

        // 타겟이 사라지면 WideBack 외 모든 앵커를 고정:
        // inactive 앵커가 inFadeOut 중 죽은 적 기준 위치로 이동하면
        // OTS→WideBack 블렌드 중 카메라가 허공으로 당겨지는 "움찔" 현상 발생.
        if (targetLost && candidate.shotType != CameraShotType.WideBack)
            return;

        // grace period 동안 WideBack도 고정: 킬 직후 action→탐색 위치로 앵커가
        // 빠르게 이동하면서 출력 카메라가 순간 0.19m/frame 급증하는 현상 방지.
        if (inGrace && candidate.shotType == CameraShotType.WideBack)
            return;

        float dist = Vector3.Distance(anchor.position, candidate.position);
        float damping;
        if (targetLost && candidate.shotType == CameraShotType.WideBack)
            // grace 종료 후 탐색 위치로 서서히 이동 (action→탐색 거리가 수m에 달하므로 느린 댐핑 필요)
            damping = postJumpDamping;
        else if (isActiveShot)
            damping = dist > largeJumpThreshold ? postJumpDamping : activePositionDamping;
        else
            damping = inactivePositionDamping;
        anchor.position = Vector3.Lerp(anchor.position, candidate.position, Time.deltaTime * damping);

        if (updateRotation)
        {
            // 회전 기준점을 anchor.position과 candidate.position 사이에서 dist에 따라 블렌드:
            // - 멀리 있을 때(dist 크면): candidate.position 기준 → forward가 항상 올바른 방향
            //   (anchor가 lookAt을 통과할 때 forward 벡터가 180° 뒤집히는 현상 방지)
            // - 가까이 있을 때(dist 작으면): anchor.position 기준 → 실제 뷰포인트에서 정확한 방향
            float blendT = Mathf.Clamp01(dist / (largeJumpThreshold * 2f));
            Vector3 rotRef = Vector3.Lerp(anchor.position, candidate.position, blendT);
            Vector3 forward = candidate.lookAt - rotRef;
            if (forward.sqrMagnitude > 1e-6f)
            {
                Quaternion prevRot = anchor.rotation;
                Quaternion targetRot = Quaternion.LookRotation(forward.normalized, upVector);
                float rotDamping = isActiveShot ? activeRotationDamping : inactiveRotationDamping;
                anchor.rotation = Quaternion.Slerp(anchor.rotation, targetRot, Time.deltaTime * rotDamping);

                if (enableGlitchLog)
                {
                    float rotDelta = Quaternion.Angle(prevRot, anchor.rotation);
                    // 활성 샷은 낮은 threshold(10°), 비활성 샷은 높은 threshold 적용
                    float effectiveThreshold = isActiveShot ? Mathf.Min(glitchRotThreshold, 10f) : glitchRotThreshold;
                    if (rotDelta > effectiveThreshold)
                        Debug.Log($"[CamGlitch] {candidate.shotType} rotΔ={rotDelta:F1}° " +
                            $"dist={dist:F2}m blendT={blendT:F2} rotDamp={rotDamping:F1} " +
                            $"fwd={forward.normalized:F2} anchorPos={anchor.position:F2} " +
                            $"rotRef={rotRef:F2} lookAt={candidate.lookAt:F2} isActive={isActiveShot} " +
                            $"targetLost={targetLost} inGrace={inGrace} t={Time.time:F3}");
                }
            }
        }
    }

    private void BuildAnchorMap()
    {
        _anchorMap = new Dictionary<CameraShotType, Transform>();

        foreach (var binding in shotAnchorBindings)
        {
            if (binding == null || binding.anchor == null)
                continue;

            if (_anchorMap.ContainsKey(binding.shotType))
            {
                Debug.LogWarning($"[CameraAnchorUpdater] Duplicate anchor binding for shot type: {binding.shotType}");
                continue;
            }

            _anchorMap.Add(binding.shotType, binding.anchor);
        }
    }
}