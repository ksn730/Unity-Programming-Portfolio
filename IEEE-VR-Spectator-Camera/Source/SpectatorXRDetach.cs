using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR; // XRDevice 사용

[RequireComponent(typeof(Camera))]
public class SpectatorXRDetach : MonoBehaviour
{
    void Awake()
    {
        var cam = GetComponent<Camera>();

        // ① XR 눈(스테레오)로는 렌더하지 않도록
        cam.stereoTargetEye = StereoTargetEyeMask.None;   // 문서상: VR이 켜졌을 때 어떤 눈에 렌더할지 정의. None이면 비활성. 

        // ② XR이 이 카메라의 위치/회전을 자동 제어하지 못하게
        XRDevice.DisableAutoXRCameraTracking(cam, true);

        // (선택) 메인 카메라 태그/오디오 리스너 제거
        if (cam.CompareTag("MainCamera")) cam.tag = "Untagged";
        var al = GetComponent<AudioListener>();
        if (al) Destroy(al);
    }

    void OnEnable() => XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), true);
    void OnDisable() => XRDevice.DisableAutoXRCameraTracking(GetComponent<Camera>(), false);
}
