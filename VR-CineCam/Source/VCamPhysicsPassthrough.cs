using Cinemachine;
using UnityEngine;

/// <summary>
/// 씬의 모든 Cinemachine Virtual Camera에서 CinemachineCollider를 비활성화합니다.
/// 카메라 앵커는 수학 계산으로 이동하므로 Cinemachine의 충돌 회피가 필요 없으며,
/// 시야를 막는 오브젝트는 OcclusionTransparencyHandler가 투명화로 처리합니다.
/// </summary>
public class VCamPhysicsPassthrough : MonoBehaviour
{
    private void Start()
    {
        int disabled = 0;
        foreach (var vcam in FindObjectsOfType<CinemachineVirtualCamera>())
        {
            var col = vcam.GetComponent<CinemachineCollider>();
            if (col != null)
            {
                col.enabled = false;
                disabled++;
            }
        }

        if (disabled > 0)
            Debug.Log($"[VCamPhysicsPassthrough] CinemachineCollider {disabled}개 비활성화 완료");
        else
            Debug.Log("[VCamPhysicsPassthrough] CinemachineCollider 없음 — 카메라 앵커 자체에 Rigidbody가 있다면 Kinematic 확인 필요");
    }
}
