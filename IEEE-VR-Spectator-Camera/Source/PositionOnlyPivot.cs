using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionOnlyPivot : MonoBehaviour
{
    public Transform source;   // headObject_Tr
    public float tau = 0.08f;  // 위치만 저역통과(0이면 끔)
    public bool lockY = true;
    public float fixedY = 2.2f;

    Vector3 sm;
    bool inited;

    void LateUpdate()
    {
        if (!source) return;

        // 1) 회전에 영향받지 않는 로컬 위치 구성: x=z=0만듦
        Vector3 lp = source.localPosition; // 부모 좌표계 기준
        lp.x = 0f;
        lp.z = 0f;

        // 2) 부모 공간 -> 월드 공간으로 복원 (회전에 따른 측면 드리프트 제거)
        Transform parent = source.parent;
        Vector3 p = parent ? parent.TransformPoint(lp) : source.position; // parent 없으면 폴백(확실하지 않음)

        // 3) 저역통과
        if (!inited) { sm = p; inited = true; }
        float a = (tau <= 0f) ? 1f : (1f - Mathf.Exp(-Time.deltaTime / tau));
        sm = Vector3.Lerp(sm, p, a);

        // 4) Y 고정 옵션
        if (lockY) sm.y = fixedY;

        // 5) 최종 pivot 갱신 (회전은 강제로 제거)
        transform.position = sm;
        transform.rotation = Quaternion.identity;
    }
}
