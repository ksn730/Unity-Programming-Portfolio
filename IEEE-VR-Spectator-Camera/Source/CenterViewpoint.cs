using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CenterViewpoint : MonoBehaviour
{
    OVRCameraRig CameraRig;
    //private Transform CenterEye => CameraRig.centerEyeAnchor;
    public Transform CenterEye;
    public Transform avatar_Tr;
    public Transform headObject_Tr;
    Transform focusingPoints;

    public StablePivot stable;
    public PositionOnlyPivot positionOnlyPivot;

    // --- snap(딱 멈춤) 임계와 락 ---
    const float ANG_EPS = 0.1f;   // 남은 회전각 스냅 임계(도)
    const float RAD_EPS = 0.01f;  // 반경 스냅 임계(미터)
    const float AZ_EPS = 1.0f;   // 방위(등뒤 방향) 스냅 임계(도)
    const float POS_SNAP_EPS = 0.05f; // 5cm 이내면 스냅(원하면 3~7cm로 조정)
    const float LOCK_DEG = 5f;   // 5~8 권장


    bool snapLock = false;       // 스냅 직후 섹터 재명령 일시 차단
    float snapLockRemain = 0f;
    const float SNAP_LOCK_TIME = 0.12f; // 0.1~0.2s 권장
    float commitCooldown = 0f;

    float rotation_speed = 0f;
    public float remaining_angle = 0f;
    float rotate_time = 0.25f;

    float smooth_time = 0.1f;
    Vector3 velocity=Vector3.zero;
    
    Vector3 offset= new Vector3(0f,2.2f,-2.5f);

    int snapConfirm = 0; // 파일 상단 class 내부 필드로 추가
    Vector3 prevPos;
    const int SNAP_REQUIRE_FRAMES = 3; // 3~5 추천

    int prev_point_num =0;
    int temp_point_num = 0;
    float point_changed_timer = 0f;
    bool turning_fisrt = false;
    private void Awake()
    {
        //CenterEye = Camera.main.transform;
        CameraRig ??= CenterEye.root.GetComponentInChildren<OVRCameraRig>();
        focusingPoints = GameObject.Find("FocusingPoints24").transform;
        avatar_Tr = GameObject.Find("Pilot").transform;
    }

    // Start is called before the first frame update
    void Start()
    {

    }



    private void LateUpdate()
    {
        float dt = Time.deltaTime; // FixedUpdate면 fixedDeltaTime 사용 권장 (확실함)
        commitCooldown -= Time.deltaTime;

        //Vector3 pivot = stable.WorldPosition;
        Vector3 pivot = positionOnlyPivot.transform.position;
        Vector3 rootFwdXZ = new Vector3(avatar_Tr.forward.x, 0f, avatar_Tr.forward.z).normalized;


        // 섹터 판정에 쓰는 시선도 수평만:
        var dir_y_zero = new Vector3(CenterEye.forward.x, 0, CenterEye.forward.z).normalized;
        float angle = Vector3.SignedAngle(focusingPoints.forward, dir_y_zero, Vector3.up);

        // snapLock 해제 타이머 (없으면 영구 락 걸림)
        if (snapLock)
        {
            snapLockRemain -= dt;
            if (snapLockRemain <= 0f) snapLock = false;
        }

        int point_num = Mathf.RoundToInt(((angle + 360f) % 360f) / 15f);
        if (point_num > 23) point_num = 0;

        if (point_num != prev_point_num)
        {
            // (A) 큰 점프는 즉시 커밋 (쿨다운 중이면 무시)
            int jump = Mathf.Min(Mathf.Abs(point_num - prev_point_num), 24 - Mathf.Abs(point_num - prev_point_num));
            if (jump >= 6 && commitCooldown <= 0f)
            {
                int diff = PointDifference(point_num, prev_point_num);
                RotateBy(15 * diff);
                prev_point_num = point_num;      // ← 커밋 시점에만 prev 갱신
                turning_fisrt = true;
                point_changed_timer = 0f;
                temp_point_num = point_num;      // 후보 동기화
                commitCooldown = 0.12f;          // 짧은 쿨다운(딸깍 방지)
            }
            else
            {
                // (B) 후보 변경 시 타이머 리셋만 (이 프레임엔 누적 금지)
                if (point_num != temp_point_num)
                {
                    temp_point_num = point_num;
                    point_changed_timer = 0f;
                }
                else
                {
                    // (C) 같은 후보가 유지될 때만 누적
                    point_changed_timer += Time.deltaTime;

                    // 0.8초 유지되면 그때 커밋 (쿨다운 중이면 대기)
                    if (point_changed_timer >= 0.8f && commitCooldown <= 0f)
                    {
                        int diff = PointDifference(point_num, prev_point_num);
                        RotateBy(15 * diff);
                        prev_point_num = point_num;    // ← 커밋 시점에만 prev 갱신
                        turning_fisrt = true;
                        point_changed_timer = 0f;
                        commitCooldown = 0.12f;        // 재발화 잠깐 차단
                    }
                }
            }
        }
        else
        {
            // 동일 섹터면 보류 상태 정리
            temp_point_num = prev_point_num;
            point_changed_timer = 0f;
        }

        // ====== 회전+이동 융합 (보간 없음) ======
        //Vector3 pivot = avatar_Tr.position;

        // 1) 이번 프레임 회전각(overshoot 방지)
        float angleThisFrame = 0f;
        if (Mathf.Abs(remaining_angle) > 0.01f)
        {
            angleThisFrame = rotation_speed * dt;
            if (Mathf.Abs(angleThisFrame) > Mathf.Abs(remaining_angle))
                angleThisFrame = Mathf.Sign(remaining_angle) * Mathf.Abs(remaining_angle);
            remaining_angle -= angleThisFrame;
        }

        // 2) 현재 카메라의 피벗 기준 XZ 벡터를 '각도만큼' 회전
        Vector3 rel = transform.position - pivot;
        float keepY = 2.2f;
        Vector3 relXZ = new Vector3(rel.x, 0f, rel.z);
        Quaternion q = Quaternion.AngleAxis(angleThisFrame, Vector3.up);
        Vector3 rotatedXZ = q * relXZ;

        // 3) 목표(아바타 등 뒤) 방향 계산
        float desiredDist = 2.5f; // 유지 반경
        Vector3 toPoint = focusingPoints.GetChild(prev_point_num).position - pivot;
        toPoint.y = 0f;
        if (toPoint.sqrMagnitude < 1e-6f) toPoint = avatar_Tr.forward; // 안전장치(확실하지 않음)
        Vector3 desiredDirXZ = (-toPoint).normalized; // 등 뒤

        // 4) 반경은 '즉시' 고정, 방향은 이번 프레임 회전 결과를 사용
        Vector3 dirXZ = rotatedXZ.sqrMagnitude > 1e-6f ? rotatedXZ.normalized : desiredDirXZ;

        // 회전+이동 융합 블록에서 finalXZ 계산 직전, 이 두 줄 추가
        float signedAzErr = Vector3.SignedAngle(dirXZ, desiredDirXZ, Vector3.up); // 부호 포함
        float azErr = Mathf.Abs(signedAzErr);  // 기존 azErr로 써도 됨

        if (azErr <= LOCK_DEG)                 // ? 아주 근접하면 방향을 '바로' 등뒤로 고정
            dirXZ = desiredDirXZ;

        Vector3 finalXZ = dirXZ * desiredDist;

        // 5) 최종 위치 한 번만 세팅 (회전+이동 동시 적용)
        transform.position = new Vector3(pivot.x + finalXZ.x, keepY, pivot.z + finalXZ.z);

        // 현재 계산된 최종 후보 위치
        Vector3 candidate = new Vector3(pivot.x + finalXZ.x, keepY, pivot.z + finalXZ.z);
        Vector3 snapGoal = new Vector3(pivot.x + (desiredDirXZ.x * desiredDist), keepY, pivot.z + (desiredDirXZ.z * desiredDist));
        float posJump = Vector3.Distance(candidate, snapGoal);

        // 6) 스냅 존: 충분히 가까우면 '딱' 고정 + 재명령 잠깐 차단
        azErr = Vector3.Angle(dirXZ, desiredDirXZ); // 방향 오차(도)
        float angErr = Mathf.Abs(remaining_angle);

        float camSpeed = (transform.position - prevPos).magnitude / dt;
        prevPos = transform.position;

        // 스냅 블록 직전
        float snapMin = 2f * desiredDist * Mathf.Sin(Mathf.Deg2Rad * (AZ_EPS * 0.5f)) + 0.005f; // +5mm 여유
        float posSnapEps = Mathf.Max(POS_SNAP_EPS /*기존 상수*/, snapMin);
        
        if(angErr <= ANG_EPS && azErr <= AZ_EPS && posJump <= posSnapEps)
        {
            transform.position = snapGoal; // 딱 고정
            remaining_angle = 0f;
            rotation_speed = 0f;
            snapLock = true;
            snapLockRemain = SNAP_LOCK_TIME;
        }
        

        // 7) Yaw만 '즉시' 아바타로 스냅(보간 없음, Pitch/Roll 유지)
        Vector3 toAvatarXZ = new Vector3(
            pivot.x - transform.position.x, 0f,
            pivot.z - transform.position.z

        );
        if (toAvatarXZ.sqrMagnitude > 1e-6f)
        {
            Quaternion currentYaw = Quaternion.LookRotation(
                new Vector3(transform.forward.x, 0f, transform.forward.z).normalized, Vector3.up);
            Quaternion desiredYaw = Quaternion.LookRotation(toAvatarXZ.normalized, Vector3.up);
            // Yaw만 교체(즉시 스냅)
            transform.rotation = desiredYaw * Quaternion.Inverse(currentYaw) * transform.rotation;
        }
    }

    void CamRotateAround(int point_num)
    {
        float angle_this_frame=rotation_speed*Time.deltaTime;
        transform.RotateAround(new Vector3(headObject_Tr.position.x, transform.position.y, headObject_Tr.position.z), Vector3.up, 15 * (point_num - prev_point_num));
        offset=transform.position-headObject_Tr.position;
    }

    void RotateBy(float angle)
    {
        remaining_angle += angle;
        rotation_speed = remaining_angle / rotate_time;
    }

    int PointDifference(int point_num, int prev_point_num,int modulo=24) {
        int diff = (point_num - prev_point_num + modulo) % modulo;

        // 최소 회전 방향으로 조정: -11~+12 범위
        if (diff > modulo / 2)
            diff -= modulo;

        return diff;
    }
}
