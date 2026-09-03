using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SignalExtractor;

[DefaultExecutionOrder(-50)]
public class SituationEstimator : MonoBehaviour
{
    public SignalExtractor signalExtractor;
    public AttentionTargetEstimator attentionTargetEstimator;

    [Header("Trigger Decay")]
    [Tooltip("트리거를 뗀 후 combatIntensity가 0으로 내려가는 데 걸리는 시간 (초)")]
    [Range(0f, 2f)] public float triggerDecayTime = 0.4f;

    [Header("Combat Intensity Weights")]
    [Tooltip("트리거 입력이 combatIntensity에 기여하는 최대 가중치")]
    [Range(0f, 1f)] public float combatTriggerWeight = 0.30f;
    [Tooltip("레이캐스트가 Enemy에 직접 맞을 때 기여하는 최대 가중치")]
    [Range(0f, 1f)] public float combatHitEnemyWeight = 0.35f;
    [Tooltip("AttentionTargetEstimator가 Enemy를 타겟으로 확정했을 때 기여하는 가중치 (소프트에임 포함)")]
    [Range(0f, 1f)] public float combatAttentionEnemyWeight = 0.20f;
    [Tooltip("컨트롤러 각속도 기여 가중치 (내부에서 *0.1 스케일 적용)")]
    [Range(0f, 1f)] public float combatControllerAngularWeight = 0.10f;
    [Tooltip("HMD 각속도 기여 가중치 (내부에서 *0.1 스케일 적용)")]
    [Range(0f, 1f)] public float combatHmdAngularWeight = 0.05f;

    [Range(0f, 1f)] public float combatIntensity;
    [Range(0f, 1f)] public float interactionIntensity;
    [Range(0f, 1f)] public float explorationIntensity;

    public GameObject attentionTarget;
    [Range(0f, 1f)] public float attentionConfidence;

    private float _decayedTrigger;

    private void Update()
    {
        if (signalExtractor == null) return;

        VRSignalFrame f = signalExtractor.CurrentFrame;

        // 트리거 decay: 누를 때 즉시 반응, 뗄 때 서서히 감소
        float rawTrigger = f.rightHand.triggerValue;
        if (rawTrigger > _decayedTrigger)
            _decayedTrigger = rawTrigger;
        else
            _decayedTrigger = Mathf.MoveTowards(_decayedTrigger, rawTrigger,
                Time.deltaTime * (triggerDecayTime > 0f ? 1f / triggerDecayTime : float.MaxValue));

        // ������ ray ���� ����
        bool hasHit = f.rightHand.rayHit.hasHit;
        GameObject hitObj = f.rightHand.rayHit.hitObject;

        bool hitEnemy = hasHit && hitObj != null && hitObj.CompareTag("Enemy");
        bool hitInteractable = hasHit && hitObj != null && hitObj.CompareTag("Interactable");
        bool hitEnvironment = hasHit && hitObj != null && hitObj.CompareTag("Environment");
        bool effectiveHit = hasHit && !hitEnvironment;

        // 왼손 ray: NPC 상호작용은 왼손으로 조준
        GameObject leftHitObj = f.leftHand.rayHit.hitObject;
        bool leftHitNpc = f.leftHand.rayHit.hasHit && leftHitObj != null
                          && leftHitObj.CompareTag("Interactable");

        // attention target
        attentionTarget = hasHit ? hitObj : null;
        attentionConfidence = hasHit ? Mathf.Clamp01(
            0.6f * f.rightHand.triggerValue +
            0.4f * Mathf.Clamp01(1.0f / Mathf.Max(f.rightHand.rayHit.distance, 1f))
        ) : 0f;

        // combat
        bool attentionIsEnemy = attentionTargetEstimator != null &&
            attentionTargetEstimator.CurrentAttention.isValid &&
            attentionTargetEstimator.CurrentAttention.targetType == AttentionTargetType.Enemy;

        combatIntensity = Mathf.Clamp01(
            combatTriggerWeight           * _decayedTrigger +
            combatHitEnemyWeight          * (hitEnemy ? 1f : 0f) +
            combatAttentionEnemyWeight    * (attentionIsEnemy ? 1f : 0f) +
            combatControllerAngularWeight * Mathf.Clamp01(f.rightHand.localAngularVelocity.magnitude * 0.1f) +
            combatHmdAngularWeight        * Mathf.Clamp01(f.hmdAngularVelocity.magnitude * 0.1f)
        );

        // interaction: 왼손으로 NPC를 가리키는 것 자체가 주 신호 (카메라 선제 전환)
        //             grip은 보조 강화 신호
        interactionIntensity = Mathf.Clamp01(
            0.45f * (leftHitNpc ? 1f : 0f) +
            0.25f * f.leftHand.gripValue +
            0.15f * f.rightHand.gripValue +
            0.10f * (hitInteractable ? 1f : 0f) +
            0.05f * (f.playerVelocity.magnitude < 0.3f ? 1f : 0f)
        );

        // exploration
        explorationIntensity = Mathf.Clamp01(
            0.45f * Mathf.Clamp01(f.leftHand.thumbstick.magnitude) +
            0.35f * Mathf.Clamp01(f.hmdAngularVelocity.magnitude * 0.08f) +
            0.20f * (!effectiveHit ? 1f : 0f)
        );
    }
}
