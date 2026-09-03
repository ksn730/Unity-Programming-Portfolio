using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ShotCameraBinding
{
    public CameraShotType shotType;
    public CinemachineVirtualCamera virtualCamera;
}

[DefaultExecutionOrder(-10)]
public class CameraDirector : MonoBehaviour
{
    [Header("References")]
    public ShotEvaluator shotEvaluator;

    [Header("VCam Bindings")]
    public List<ShotCameraBinding> shotCameraBindings = new List<ShotCameraBinding>();

    [Header("Priority Settings")]
    public int activePriority = 20;
    public int inactivePriority = 10;

    [Header("Switch Control")]
    [Range(0f, 5f)] public float switchScoreThreshold = 0.1f;
    [Range(0f, 5f)] public float minHoldTime = 1.0f;

    [Header("Fallback Camera")]
    public CinemachineVirtualCamera fallbackCamera;
    public int fallbackPriority = 30;

    [Header("Blend Lock")]
    [Tooltip("씬에 있는 CinemachineBrain을 연결. 블렌딩 중 새 샷 전환을 차단하는 데 사용.")]
    public CinemachineBrain cinemachineBrain;
    [Tooltip("Cinemachine이 블렌딩 중일 때 새 샷 전환을 차단합니다 (전환 가로채기 방지).")]
    public bool lockSwitchDuringBlend = true;

    [Header("Temporal Stabilization")]
    [Tooltip("Target이 사라진 후 이 시간 동안은 현재 shot 유지. TargetGroupUpdater.deathFreezeTime과 맞출 것.")]
    [Range(0f, 3f)] public float targetLossGraceTime = 0.5f;

    [Tooltip("Target이 다시 생겼을 때 이 시간 이상 유지되어야 action shot으로 전환")]
    [Range(0f, 3f)] public float targetReacquireConfirmTime = 0.2f;

    [Tooltip("탐색 모드가 이 시간(초) 이상 지속됐을 때만 재획득 시 holdTime을 초기화. " +
             "짧은 에임 이탈 후 복귀에는 holdTime 보호를 유지해 샷 전환을 방지.")]
    [Range(0f, 5f)] public float freshAcquisitionMinTime = 2.0f;

    [Header("Debug")]
    public CameraShotType currentShotType = CameraShotType.None;
    public float currentShotScore = float.NegativeInfinity;
    public CameraShotType candidateShotType = CameraShotType.None;
    public float candidateShotScore = float.NegativeInfinity;

    public bool isInFallback = false;
    public bool hasAttentionNow = false;
    public bool isInTargetLossGrace = false;

    private Dictionary<CameraShotType, CinemachineVirtualCamera> _cameraMap;
    private float _lastSwitchTime = -999f;

    private float _lastValidAttentionTime = -999f;
    private float _attentionReacquiredTime = -999f;
    private bool _wasExploring = false;
    private GameObject _prevAttentionObject = null;

    private void Awake()
    {
        BuildCameraMap();
    }

    private void Start()
    {
        SetAllInactive();
    }

    private void Update()
    {
        if (shotEvaluator == null || shotEvaluator.attentionTargetEstimator == null)
            return;

        hasAttentionNow =
            shotEvaluator.attentionTargetEstimator.CurrentAttention.isValid &&
            shotEvaluator.attentionTargetEstimator.CurrentAttention.targetObject != null;

        float now = Time.time;

        // 탐색 모드 → target 획득 전환 시 holdTime 해제
        // 단, 탐색 기간이 freshAcquisitionMinTime 이상일 때만 해제
        // (짧은 에임 이탈 후 복귀는 holdTime 보호 유지)
        if (hasAttentionNow && _wasExploring)
        {
            float explorationDuration = now - _lastValidAttentionTime;
            if (explorationDuration >= freshAcquisitionMinTime)
            {
                _lastSwitchTime = -999f;
                currentShotScore = candidateShotScore - switchScoreThreshold;
            }
        }
        _wasExploring = !hasAttentionNow;

        // -----------------------------
        // 1) attention ��ȿ �ð� ����
        // -----------------------------
        if (hasAttentionNow)
        {
            _lastValidAttentionTime = now;

            GameObject currentTarget =
                shotEvaluator.attentionTargetEstimator.CurrentAttention.targetObject;

            // 타겟 오브젝트가 바뀌면 reacquire 타이머 리셋.
            // Enemy A 사망 후 Enemy B가 즉시 획득될 때 낡은 타이머로
            // reacquireConfirmTime 체크를 우회하는 현상 방지.
            if (currentTarget != _prevAttentionObject)
            {
                _attentionReacquiredTime = now;
                _prevAttentionObject = currentTarget;
            }
            else if (_attentionReacquiredTime < 0f)
            {
                _attentionReacquiredTime = now;
            }
        }
        else
        {
            _attentionReacquiredTime = -999f;
            _prevAttentionObject = null;
        }

        // -----------------------------
        // 2) target 소실 후 grace period 이후 탐색 shot 선택
        // -----------------------------
        if (!hasAttentionNow)
        {
            float lostDuration = now - _lastValidAttentionTime;

            // grace time 이내는 그냥 현재 shot 유지
            if (lostDuration < targetLossGraceTime)
            {
                isInTargetLossGrace = true;
                return;
            }
            isInTargetLossGrace = false;

            // 블렌딩 중이면 탐색 전환도 차단 (블렌드 가로채기 방지)
            if (lockSwitchDuringBlend && cinemachineBrain != null &&
                currentShotType != CameraShotType.None && cinemachineBrain.IsBlending)
                return;

            // 타겟 소실 시 minHoldTime 미적용: 적 사망 즉시 탐색 모드로 전환 허용.
            // minHoldTime은 action shot 간 전환 품질 유지 목적이므로
            // 타겟이 없는 탐색 전환에는 불필요.

            ShotEvaluationResult explorationBest = shotEvaluator.bestShot;
            if (!explorationBest.isValid)
            {
                // WideBack을 강제 활성화 (fallback 카메라 대신)
                ActivateShot(CameraShotType.WideBack, 0f);
                return;
            }

            if (isInFallback)
                DeactivateFallbackCamera();

            // target 잃은 시점부터 target 기반 shot 점수는 탐색 shot과 비교 불가
            // score threshold 없이 바로 전환
            if (currentShotType != explorationBest.candidate.shotType)
                ActivateShot(explorationBest.candidate.shotType, explorationBest.finalScore);
            else
                currentShotScore = explorationBest.finalScore;
            return;
        }

        // -----------------------------
        // 3) target �ٽ� ���� �� confirm time ������ fallback ����
        // -----------------------------
        isInTargetLossGrace = false;

        if (isInFallback)
        {
            if (!shotEvaluator.bestShot.isValid)
                return;

            if (hasAttentionNow)
            {
                float reacquiredDuration = now - _attentionReacquiredTime;
                if (reacquiredDuration < targetReacquireConfirmTime)
                    return;
            }

            DeactivateFallbackCamera();
        }

        // -----------------------------
        // 4) �Ϲ� shot selection
        // -----------------------------
        ShotEvaluationResult best = shotEvaluator.bestShot;

        if (!best.isValid)
            return;

        candidateShotType = best.candidate.shotType;
        candidateShotScore = best.finalScore;

        // Cinemachine 블렌딩 중이면 전환 차단 (전환 가로채기 방지)
        if (lockSwitchDuringBlend && cinemachineBrain != null &&
            currentShotType != CameraShotType.None && cinemachineBrain.IsBlending)
            return;

        // ���� shot�� ������ �ٷ� Ȱ��ȭ
        if (currentShotType == CameraShotType.None)
        {
            ActivateShot(candidateShotType, candidateShotScore);
            return;
        }

        // ���� shot�̸� score�� ����
        if (candidateShotType == currentShotType)
        {
            currentShotScore = candidateShotScore;
            return;
        }

        // hold time
        bool holdSatisfied = (now - _lastSwitchTime) >= minHoldTime;

        // 점수 차이
        bool scoreSatisfied = (candidateShotScore - currentShotScore) >= switchScoreThreshold;

        // 탐색(WideBack) → 액션 샷 전환 시 재획득 확인 시간 적용.
        // freshAcquisitionMinTime으로 holdTime이 초기화된 직후 타겟이 사살되면
        // OTS가 한 프레임 활성화됐다가 grace 동안 유지되는 깜빡임 현상을 방지.
        bool reacquireConfirmed = true;
        if (currentShotType == CameraShotType.WideBack
            && candidateShotType != CameraShotType.WideBack
            && _attentionReacquiredTime > 0f)
        {
            reacquireConfirmed = (now - _attentionReacquiredTime) >= targetReacquireConfirmTime;
        }

        if (holdSatisfied && scoreSatisfied && reacquireConfirmed)
        {
            ActivateShot(candidateShotType, candidateShotScore);
        }
    }

    private void ActivateShot(CameraShotType shotType, float score)
    {
        if (_cameraMap == null || !_cameraMap.ContainsKey(shotType))
        {
            Debug.LogWarning($"[CameraDirector] No camera bound for shot type: {shotType}");
            return;
        }

        Debug.Log($"[CamDirector] {currentShotType}→{shotType} score={score:F2} " +
            $"hasAttention={hasAttentionNow} inGrace={isInTargetLossGrace} t={Time.time:F3}");

        foreach (var pair in _cameraMap)
        {
            if (pair.Value == null) continue;
            pair.Value.Priority = (pair.Key == shotType) ? activePriority : inactivePriority;
        }

        if (fallbackCamera != null)
            fallbackCamera.Priority = inactivePriority;

        currentShotType = shotType;
        currentShotScore = score;
        _lastSwitchTime = Time.time;
        isInFallback = false;

        if (shotEvaluator != null)
        {
            shotEvaluator.currentShotType = shotType;
        }
    }

    private void ActivateFallbackCamera()
    {
        if (fallbackCamera == null) return;

        foreach (var pair in _cameraMap)
        {
            if (pair.Value != null)
            {
                pair.Value.Priority = inactivePriority;
            }
        }

        fallbackCamera.Priority = fallbackPriority;

        currentShotType = CameraShotType.None;
        currentShotScore = 0f;
        isInFallback = true;

        if (shotEvaluator != null)
        {
            shotEvaluator.currentShotType = CameraShotType.None;
        }
    }

    private void DeactivateFallbackCamera()
    {
        if (fallbackCamera != null)
        {
            fallbackCamera.Priority = inactivePriority;
        }

        isInFallback = false;
    }

    private void SetAllInactive()
    {
        if (_cameraMap == null) return;

        foreach (var pair in _cameraMap)
        {
            if (pair.Value != null)
            {
                pair.Value.Priority = inactivePriority;
            }
        }

        if (fallbackCamera != null)
        {
            fallbackCamera.Priority = inactivePriority;
        }
    }

    private void BuildCameraMap()
    {
        _cameraMap = new Dictionary<CameraShotType, CinemachineVirtualCamera>();

        foreach (var binding in shotCameraBindings)
        {
            if (binding == null || binding.virtualCamera == null)
                continue;

            if (_cameraMap.ContainsKey(binding.shotType))
            {
                Debug.LogWarning($"[CameraDirector] Duplicate binding for shot type: {binding.shotType}");
                continue;
            }

            _cameraMap.Add(binding.shotType, binding.virtualCamera);
        }
    }
}