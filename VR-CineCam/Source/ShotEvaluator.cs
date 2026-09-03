using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct ShotEvaluationResult
{
    public CameraCandidate candidate;

    public float playerVisibility;
    public float targetVisibility;
    public float jointVisibility;

    public float occlusionPenalty;
    public float proximityPenalty;
    public float transitionPenalty;
    public float situationFitness;

    public float finalScore;
    public bool isValid;
}

[DefaultExecutionOrder(-20)]
public class ShotEvaluator : MonoBehaviour
{
    [Header("References")]
    public Transform playerRoot;
    public Transform playerHead;
    public AttentionTargetEstimator attentionTargetEstimator;
    public SituationEstimator situationEstimator;
    public CameraCandidateGenerator candidateGenerator;

    [Header("Visibility Sampling")]
    public LayerMask occlusionMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Player Body Points")]
    [Tooltip("Head/Chest/Hips Transform을 직접 지정. 비어있으면 Animator HumanBodyBones 자동 탐색, 그것도 없으면 Offset 폴백")]
    public Transform[] playerBodyPoints;

    [Header("Player Sample Offsets (Fallback)")]
    [Tooltip("playerBodyPoints가 없을 때 playerRoot 기준 상대 오프셋으로 사용")]
    public Vector3[] playerSampleOffsets = new Vector3[]
    {
        new Vector3(0f, 1.6f, 0f),
        new Vector3(0f, 1.2f, 0f),
        new Vector3(0f, 0.9f, 0f)
    };

    [Header("Target Sample Offsets")]
    public Vector3[] targetSampleOffsets = new Vector3[]
    {
        new Vector3(0f, 0.8f, 0f),
        new Vector3(0f, 1.2f, 0f)
    };

    [Header("Occlusion Transparency")]
    public OcclusionTransparencyHandler occlusionHandler;
    [Range(0f, 1f)] public float transparentOccluderVisibility = 0.55f;

    [Header("Weights")]
    public float playerVisibilityWeight = 1.2f;
    public float targetVisibilityWeight = 1.2f;
    public float jointVisibilityWeight = 1.5f;
    public float occlusionPenaltyWeight = 1.0f;
    public float transitionPenaltyWeight = 0.5f;
    public float situationFitnessWeight = 1.0f;

    [Header("Camera Proximity Penalty")]
    [Tooltip("카메라 위치 주변에서 비타겟 캐릭터를 감지할 레이어 마스크 (Enemy 레이어 선택 권장)")]
    public LayerMask characterProximityMask;
    [Tooltip("이 반경 이내에 비타겟 캐릭터가 있으면 페널티 (미터)")]
    public float cameraProximityRadius = 2.5f;
    [Tooltip("근접 캐릭터 페널티 가중치")]
    public float proximityPenaltyWeight = 1.5f;

    [Header("Transition")]
    public CameraShotType currentShotType = CameraShotType.None;
    [Tooltip("WideBack(탐색)에서 액션 샷으로 전환할 때 전환 페널티를 면제합니다.\n탐색→전투 진입은 빠르게 반응해야 하므로 기본값 true.")]
    public bool exemptWideBackToActionTransition = true;

    [Header("Output")]
    public List<ShotEvaluationResult> currentResults = new List<ShotEvaluationResult>();
    public ShotEvaluationResult bestShot;

    private void Start()
    {
        TryAutoResolveBodyPoints();
    }

    // playerBodyPoints가 비어있으면 Animator HumanBodyBones로 자동 탐색
    private void TryAutoResolveBodyPoints()
    {
        if (playerBodyPoints != null && playerBodyPoints.Length > 0) return;
        if (playerRoot == null) return;

        var animator = playerRoot.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman) return;

        var head  = animator.GetBoneTransform(HumanBodyBones.Head);
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        var hips  = animator.GetBoneTransform(HumanBodyBones.Hips);

        if (head != null || chest != null || hips != null)
        {
            var bones = new System.Collections.Generic.List<Transform>();
            if (head  != null) bones.Add(head);
            if (chest != null) bones.Add(chest);
            if (hips  != null) bones.Add(hips);
            playerBodyPoints = bones.ToArray();
            Debug.Log("[ShotEvaluator] Auto-resolved playerBodyPoints from Animator: " + bones.Count + " bones");
        }
    }

    // OcclusionTransparencyHandler 등 외부에서 실제 샘플 위치를 가져올 때 사용
    public Vector3[] GetPlayerSamplePositions()
    {
        if (playerRoot == null) return new Vector3[0];

        if (playerBodyPoints != null && playerBodyPoints.Length > 0)
        {
            var result = new Vector3[playerBodyPoints.Length];
            for (int i = 0; i < playerBodyPoints.Length; i++)
                result[i] = playerBodyPoints[i] != null
                    ? playerBodyPoints[i].position
                    : playerRoot.position + (i < playerSampleOffsets.Length ? playerSampleOffsets[i] : Vector3.up);
            return result;
        }

        var fallback = new Vector3[playerSampleOffsets.Length];
        for (int i = 0; i < playerSampleOffsets.Length; i++)
            fallback[i] = playerRoot.position + playerSampleOffsets[i];
        return fallback;
    }

    private void Update()
    {
        EvaluateAllShots();
    }

    public void EvaluateAllShots()
    {
        currentResults.Clear();
        bestShot = default;
        bestShot.finalScore = float.NegativeInfinity;

        if (playerRoot == null ||
            attentionTargetEstimator == null ||
            candidateGenerator == null)
            return;

        AttentionTargetState attention = attentionTargetEstimator.CurrentAttention;
        bool hasTarget = attention.isValid && attention.targetObject != null;

        IReadOnlyList<CameraCandidate> candidates = candidateGenerator.CurrentCandidates;
        if (candidates == null || candidates.Count == 0)
            return;

        foreach (var candidate in candidates)
        {
            if (!hasTarget && !IsExplorationShot(candidate.shotType))
                continue;

            ShotEvaluationResult result = EvaluateCandidate(candidate, attention);
            currentResults.Add(result);

            if (result.isValid && result.finalScore > bestShot.finalScore)
            {
                bestShot = result;
            }
        }
    }

    private ShotEvaluationResult EvaluateCandidate(CameraCandidate candidate, AttentionTargetState attention)
    {
        ShotEvaluationResult result = new ShotEvaluationResult
        {
            candidate = candidate,
            isValid = candidate.isValid
        };

        if (!candidate.isValid)
            return result;

        bool hasTarget = attention.targetObject != null;

        // 1. Visibility
        result.playerVisibility = ComputePlayerVisibility(candidate.position);
        result.targetVisibility = hasTarget
            ? ComputeTargetVisibility(candidate.position, attention.targetObject.transform)
            : 0f;
        result.jointVisibility = hasTarget
            ? Mathf.Min(result.playerVisibility, result.targetVisibility)
            : result.playerVisibility;

        // 2. Occlusion penalty
        result.occlusionPenalty = 1f - result.jointVisibility;

        // 2b. Proximity penalty: 비타겟 캐릭터가 카메라 위치 바로 앞에 있는 경우
        result.proximityPenalty = ComputeProximityPenalty(candidate.position, attention.targetObject);

        // 3. Situation fitness
        result.situationFitness = ComputeSituationFitness(candidate.shotType);

        // 4. Transition penalty
        result.transitionPenalty = ComputeTransitionPenalty(candidate.shotType);

        // 5. Final score
        float score = 0f;
        score += playerVisibilityWeight * result.playerVisibility;
        score += targetVisibilityWeight * result.targetVisibility;
        score += jointVisibilityWeight * result.jointVisibility;
        score -= occlusionPenaltyWeight * result.occlusionPenalty;
        score -= proximityPenaltyWeight * result.proximityPenalty;
        score -= transitionPenaltyWeight * result.transitionPenalty;
        score += situationFitnessWeight * result.situationFitness;
        score += candidate.baseWeight;

        result.finalScore = score;
        result.isValid = true;

        return result;
    }

    private float ComputePlayerVisibility(Vector3 cameraPos)
    {
        if (playerRoot == null) return 0f;

        Vector3[] samplePositions = GetPlayerSamplePositions();
        if (samplePositions.Length == 0) return 0f;

        float totalVisibility = 0f;
        for (int i = 0; i < samplePositions.Length; i++)
            totalVisibility += ComputePointVisibility(cameraPos, samplePositions[i], playerRoot.gameObject);

        return totalVisibility / samplePositions.Length;
    }

    private Vector3[] GetTargetSamplePositions(Transform target)
    {
        // 1) EnemyBodyReference 컴포넌트
        EnemyBodyReference bodyRef = target.GetComponent<EnemyBodyReference>();
        if (bodyRef != null && bodyRef.bodyPoints != null && bodyRef.bodyPoints.Length > 0)
        {
            var result = new Vector3[bodyRef.bodyPoints.Length];
            for (int i = 0; i < bodyRef.bodyPoints.Length; i++)
                result[i] = bodyRef.bodyPoints[i] != null
                    ? bodyRef.bodyPoints[i].position
                    : target.position;
            return result;
        }

        // 2) Humanoid Animator에서 직접 탐색 (플레이어와 동일한 방식)
        Animator anim = target.GetComponentInChildren<Animator>();
        if (anim != null && anim.isHuman)
        {
            var bones = new System.Collections.Generic.List<Vector3>();
            var head  = anim.GetBoneTransform(HumanBodyBones.Head);
            var chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            var hips  = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (head  != null) bones.Add(head.position);
            if (chest != null) bones.Add(chest.position);
            if (hips  != null) bones.Add(hips.position);
            if (bones.Count > 0) return bones.ToArray();
        }

        // 3) fallback: 기존 오프셋 방식
        var fallback = new Vector3[targetSampleOffsets.Length];
        for (int i = 0; i < targetSampleOffsets.Length; i++)
            fallback[i] = target.position + targetSampleOffsets[i];
        return fallback;
    }

    private float ComputeTargetVisibility(Vector3 cameraPos, Transform target)
    {
        if (target == null) return 0f;

        Vector3[] samplePositions = GetTargetSamplePositions(target);
        if (samplePositions.Length == 0) return 0f;

        float totalVisibility = 0f;
        foreach (var pos in samplePositions)
            totalVisibility += ComputePointVisibility(cameraPos, pos, target.gameObject);

        return totalVisibility / samplePositions.Length;
    }

    // Returns: 1=clear, transparentOccluderVisibility=transparent blocker, 0=fully blocked
    private float ComputePointVisibility(Vector3 cameraPos, Vector3 targetPoint, GameObject expectedObject)
    {
        Vector3 dir = targetPoint - cameraPos;
        float dist = dir.magnitude;
        if (dist < 1e-4f) return 1f;

        dir /= dist;

        if (Physics.Raycast(cameraPos, dir, out RaycastHit hit, dist, occlusionMask, triggerInteraction))
        {
            if (hit.collider == null) return 0f;

            // �ڱ� �ڽ� �Ǵ� �ڽı��� ���
            if (hit.collider.gameObject == expectedObject ||
                hit.collider.transform.IsChildOf(expectedObject.transform))
                return 1f;

            if (occlusionHandler != null && occlusionHandler.IsCurrentlyTransparent(hit.collider.gameObject))
                return transparentOccluderVisibility;

            return 0f;
        }

        return 1f;
    }

    private float ComputeProximityPenalty(Vector3 cameraPos, GameObject targetObject)
    {
        if (characterProximityMask == 0) return 0f;

        float penalty = 0f;
        Collider[] nearby = Physics.OverlapSphere(cameraPos, cameraProximityRadius, characterProximityMask);
        foreach (var col in nearby)
        {
            if (col == null) continue;
            if (targetObject != null && col.transform.IsChildOf(targetObject.transform)) continue;
            if (playerRoot != null && col.transform.IsChildOf(playerRoot)) continue;

            float dist = Vector3.Distance(cameraPos, col.ClosestPoint(cameraPos));
            penalty += Mathf.Clamp01(1f - dist / cameraProximityRadius);
        }
        return Mathf.Clamp01(penalty);
    }

    private float ComputeSituationFitness(CameraShotType shotType)
    {
        if (situationEstimator == null) return 0f;

        float combat = situationEstimator.combatIntensity;
        float interaction = situationEstimator.interactionIntensity;
        float exploration = situationEstimator.explorationIntensity;

        switch (shotType)
        {
            case CameraShotType.OverShoulderLeft:
            case CameraShotType.OverShoulderRight:
                return 0.7f * combat + 0.3f * interaction;

            case CameraShotType.SideLeft:
            case CameraShotType.SideRight:
                return 0.5f * combat + 0.8f * interaction;

            case CameraShotType.WideBack:
                return 0.3f * combat + 0.7f * exploration;

            default:
                return 0f;
        }
    }

    private bool IsExplorationShot(CameraShotType type) =>
        type == CameraShotType.WideBack;

    private float ComputeTransitionPenalty(CameraShotType nextShotType)
    {
        if (currentShotType == CameraShotType.None)
            return 0f;

        if (currentShotType == nextShotType)
            return 0f;

        // 탐색(WideBack) → 액션 샷 전환: 전투 진입이므로 페널티 면제
        if (exemptWideBackToActionTransition && currentShotType == CameraShotType.WideBack)
            return 0f;

        // 다른 타입으로 바뀌면 기본 penalty
        return 0.4f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (currentResults == null) return;

        foreach (var result in currentResults)
        {
            if (!result.isValid) continue;

            Color c = Color.Lerp(Color.red, Color.green, Mathf.InverseLerp(0f, 5f, result.finalScore));
            Gizmos.color = c;
            Gizmos.DrawSphere(result.candidate.position, 0.12f);
        }
    }
#endif
}