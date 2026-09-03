using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StablePivot : MonoBehaviour
{
    [Header("Sources")]
    public Transform avatarRoot;     // 위치는 이걸 기준 (루트/힙 등)
    public Transform headOrCenterEye;// 방향은 여기서 Yaw만 사용 (없으면 null 허용)

    [Header("Tuning")]
    [Tooltip("위치 저역통과 시간상수(초). 작을수록 빠름, 클수록 안정")]
    public float posTau = 0.3f;     // 50ms
    [Tooltip("Yaw 저역통과 시간상수(초)")]
    public float yawTau = 0.10f;     // 100ms
    [Tooltip("고속 이동 시 선행 보정 시간(초). 0이면 끔")]
    public float leadTime = 0f;   // 80ms 선행(원치 않으면 0)

    // 내부 상태
    Vector3 _smPos;
    float _smYaw;                  // degree
    Vector3 _prevRawPos;
    bool _init;

    public Vector3 WorldPosition => _smPos;
    public Vector3 ForwardXZ
    {
        get
        {
            float rad = _smYaw * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)); // +Z가 0도 기준
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (avatarRoot == null) return;

        // 1) 원시 위치(루트, 월드)
        Vector3 rawPos = avatarRoot.position;

        // 2) 원시 Yaw (head가 있으면 그 수평방향, 없으면 루트 전방)
        Vector3 fwd = (headOrCenterEye != null)
            ? new Vector3(headOrCenterEye.forward.x, 0f, headOrCenterEye.forward.z)
            : new Vector3(avatarRoot.forward.x, 0f, avatarRoot.forward.z);
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;

        float rawYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg; // 수평 Yaw

        // 초기화
        if (!_init)
        {
            _smPos = rawPos;
            _smYaw = rawYaw;
            _prevRawPos = rawPos;
            _init = true;
        }

        // 3) 속도 추정 (선행 보정용)
        Vector3 vel = (rawPos - _prevRawPos) / Mathf.Max(1e-6f, dt);
        _prevRawPos = rawPos;

        // 4) 선행 보정(옵션): 고속 이동 시 뒤로 밀림 방지
        Vector3 targetPos = (leadTime > 0f) ? rawPos + vel * leadTime : rawPos;

        // 5) 지수 저역통과(프레임 독립)
        float aPos = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, posTau));
        float aYaw = 1f - Mathf.Exp(-dt / Mathf.Max(1e-4f, yawTau));

        _smPos = Vector3.Lerp(_smPos, targetPos, aPos);
        _smYaw = Mathf.LerpAngle(_smYaw, rawYaw, aYaw);

        // (선택) 이 오브젝트 자체를 따라가게 하고 싶으면:
        transform.position = _smPos;
        transform.rotation = Quaternion.Euler(0f, _smYaw, 0f);
    }
}
