using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SignalExtractor : MonoBehaviour
{
    [System.Serializable]
    public struct RayHitInfo
    {
        public bool hasHit;
        public GameObject hitObject;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
        public float distance;
        public string tag;
        public int layer;
    }

    [System.Serializable]
    public struct HandInputState
    {
        public float triggerValue;
        public float gripValue;
        public Vector2 thumbstick;

        public bool primaryButton;
        public bool secondaryButton;

        public Vector3 position;
        public Vector3 forward;
        public Vector3 localVelocity;
        public Vector3 localAngularVelocity;

        public RayHitInfo rayHit;
    }

    [System.Serializable]
    public struct VRSignalFrame
    {
        public float time;

        public Vector3 playerRootPosition;
        public Vector3 playerVelocity;

        public Vector3 hmdPosition;
        public Vector3 hmdForward;
        public Vector3 hmdVelocity;
        public Vector3 hmdAngularVelocity;

        public HandInputState leftHand;
        public HandInputState rightHand;
    }


    [Header("Rig References")]
    public Transform playerRoot;
    public Transform centerEyeAnchor;
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;

    [Header("Raycast")]
    public float rayDistance = 30f;
    public LayerMask raycastMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Debug")]
    public bool drawDebugRay = true;

    public VRSignalFrame CurrentFrame { get; private set; }

    private Vector3 _prevPlayerPos;
    private Vector3 _prevHmdPos;
    private Vector3 _prevHmdForward;

    private void Start()
    {
        if (playerRoot != null) _prevPlayerPos = playerRoot.position;
        if (centerEyeAnchor != null)
        {
            _prevHmdPos = centerEyeAnchor.position;
            _prevHmdForward = centerEyeAnchor.forward;
        }
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-5f);

        VRSignalFrame frame = new VRSignalFrame
        {
            time = Time.time,
            playerRootPosition = playerRoot != null ? playerRoot.position : Vector3.zero,
            hmdPosition = centerEyeAnchor != null ? centerEyeAnchor.position : Vector3.zero,
            hmdForward = centerEyeAnchor != null ? centerEyeAnchor.forward : Vector3.forward
        };

        // Player root velocity
        if (playerRoot != null)
        {
            frame.playerVelocity = (playerRoot.position - _prevPlayerPos) / dt;
            _prevPlayerPos = playerRoot.position;
        }

        // HMD velocity / angular velocity
        if (centerEyeAnchor != null)
        {
            frame.hmdVelocity = (centerEyeAnchor.position - _prevHmdPos) / dt;
            frame.hmdAngularVelocity = EstimateAngularVelocity(_prevHmdForward, centerEyeAnchor.forward, dt);

            _prevHmdPos = centerEyeAnchor.position;
            _prevHmdForward = centerEyeAnchor.forward;
        }

        // Left / Right hand
        frame.leftHand = ExtractHandState(
            OVRInput.Controller.LTouch,
            leftControllerAnchor,
            isLeft: true);

        frame.rightHand = ExtractHandState(
            OVRInput.Controller.RTouch,
            rightControllerAnchor,
            isLeft: false);

        CurrentFrame = frame;
    }

    private HandInputState ExtractHandState(
        OVRInput.Controller controller,
        Transform anchor,
        bool isLeft)
    {
        HandInputState state = new HandInputState();

        if (anchor != null)
        {
            state.position = anchor.position;
            state.forward = anchor.forward;

            state.rayHit = PerformRaycast(anchor.position, anchor.forward);

            if (drawDebugRay)
            {
                Color rayColor = state.rayHit.hasHit ? Color.green : Color.red;
                Debug.DrawRay(anchor.position, anchor.forward * rayDistance, rayColor);
            }
        }

        // Analog
        state.triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        state.gripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
        state.thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller);

        // Buttons
        state.primaryButton = OVRInput.Get(isLeft ? OVRInput.Button.Three : OVRInput.Button.One, controller);
        state.secondaryButton = OVRInput.Get(isLeft ? OVRInput.Button.Four : OVRInput.Button.Two, controller);

        // Local velocity / angular velocity
        state.localVelocity = OVRInput.GetLocalControllerVelocity(controller);
        state.localAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(controller);

        return state;
    }

    private RayHitInfo PerformRaycast(Vector3 origin, Vector3 direction)
    {
        RayHitInfo info = new RayHitInfo
        {
            hasHit = false,
            hitObject = null,
            hitPoint = Vector3.zero,
            hitNormal = Vector3.zero,
            distance = rayDistance,
            tag = string.Empty,
            layer = -1
        };

        if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance, raycastMask, triggerInteraction))
        {
            info.hasHit = true;
            info.hitObject = hit.collider.gameObject;
            info.hitPoint = hit.point;
            info.hitNormal = hit.normal;
            info.distance = hit.distance;
            info.tag = hit.collider.tag;
            info.layer = hit.collider.gameObject.layer;
        }

        return info;
    }

    private Vector3 EstimateAngularVelocity(Vector3 prevForward, Vector3 currentForward, float dt)
    {
        if (prevForward.sqrMagnitude < 1e-6f || currentForward.sqrMagnitude < 1e-6f)
            return Vector3.zero;

        Quaternion delta = Quaternion.FromToRotation(prevForward, currentForward);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);

        if (float.IsNaN(axis.x) || axis == Vector3.zero)
            return Vector3.zero;

        if (angleDeg > 180f) angleDeg -= 360f;

        float angleRad = angleDeg * Mathf.Deg2Rad;
        return axis.normalized * (angleRad / dt);
    }
}
