using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 물리 손 대체 조작. 포인터를 평면에 투영한 점을 Rigidbody가 스프링·감쇠로 따라간다.
    /// 기울이기 버튼(우클릭)을 누르는 동안 지정 축을 기준으로 기울고, 떼면 다시 선다(휠 방식도 선택 가능). 속도로 움직이므로 위에 올린 물체(얼음 등)는 물리적으로 따라오거나 튀어 나간다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MouseSpringFollower : MonoBehaviour
    {
        public enum GrabMode
        {
            /// <summary>항상 따라간다(스쿱).</summary>
            Always,
            /// <summary>잡기 입력을 누르고 있을 때만 따라간다(우유팩).</summary>
            WhileGrabHeld,
            /// <summary>SetGrabbed로 밖에서 잡기를 정한다(컵 끌기).</summary>
            Manual,
        }

        public enum TiltMode
        {
            /// <summary>기울이기 버튼을 누르는 동안 기울고, 떼면 다시 선다.</summary>
            HoldToTilt,
            /// <summary>휠 한 칸마다 일정 각도씩 기울인다.</summary>
            WheelSteps,
        }

        [Header("참조")]
        [SerializeField] private Camera viewCamera;
        [Tooltip("IHandInput을 구현한 컴포넌트. 비우면 마우스")]
        [SerializeField] private MonoBehaviour handInputSource;
        [Tooltip("이동 평면의 기준점. 비우면 시작 위치")]
        [SerializeField] private Transform planeOrigin;

        [Header("이동")]
        [SerializeField] private GrabMode grabMode = GrabMode.Always;
        [SerializeField] private Vector3 planeNormal = Vector3.up;
        [Tooltip("평면 기준점에서 벗어날 수 있는 최대 거리")]
        [SerializeField] private float maxDistanceFromOrigin = 1.5f;
        [Tooltip("스프링 강도. 클수록 빨리 따라온다")]
        [SerializeField] private float springStrength = 150f;
        [Tooltip("감쇠. 작을수록 출렁인다 (임계 감쇠 ≈ 2·√강도)")]
        [SerializeField] private float damping = 16f;
        [SerializeField] private float maxSpeed = 5f;

        [Header("기울이기")]
        [Tooltip("기울이는 로컬 축")]
        [SerializeField] private Vector3 tiltAxisLocal = Vector3.right;
        [SerializeField] private TiltMode tiltMode = TiltMode.HoldToTilt;
        [Tooltip("HoldToTilt: 누르고 있는 동안 초당 기울어지는 각도")]
        [SerializeField] private float tiltSpeed = 90f;
        [Tooltip("HoldToTilt: 떼면 초당 되돌아오는 각도")]
        [SerializeField] private float tiltReturnSpeed = 180f;
        [Tooltip("WheelSteps: 휠 한 칸당 각도")]
        [SerializeField] private float tiltPerNotch = 15f;
        [SerializeField] private float minTilt = 0f;
        [SerializeField] private float maxTilt = 120f;
        [Tooltip("목표 기울기로 회전을 맞추는 속도")]
        [SerializeField] private float rotationGain = 12f;
        [SerializeField] private float maxAngularSpeed = 12f;

        private Rigidbody body;
        private IHandInput hand;
        private Vector3 originPoint;
        private Vector3 holdTarget;
        private Quaternion restRotation;
        private Vector3 velocity;
        private bool initialized;
        private bool authoredGravity;

        public bool InputEnabled { get; set; }
        public bool IsGrabbed { get; private set; }
        /// <summary>입력으로 정한 목표 기울기(도).</summary>
        public float TargetTilt { get; private set; }
        /// <summary>실제 몸체가 세워진 상태에서 기울어진 각도(도). 출렁임 포함.</summary>
        public float ActualTilt => body != null ? Quaternion.Angle(restRotation, body.rotation) : 0f;
        public Vector3 Target => holdTarget;
        public Rigidbody Body { get { EnsureInit(); return body; } }
        public IHandInput Hand { get { EnsureInit(); return hand; } set { hand = value ?? MouseHandInput.Shared; } }
        public Camera ViewCamera { get => viewCamera; set => viewCamera = value; }

        private void Awake() => EnsureInit();

        private void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            body = GetComponent<Rigidbody>();
            authoredGravity = body.useGravity;
            if (hand == null) hand = HandInputResolver.Resolve(handInputSource);
            restRotation = transform.rotation;
            originPoint = planeOrigin != null ? planeOrigin.position : transform.position;
            holdTarget = transform.position;
        }

        /// <summary>
        /// 처음 상태로 되돌린다. 위치·회전·속도·기울기·잡기를 모두 초기화.
        /// </summary>
        public void ResetTo(Vector3 position, Quaternion rotation)
        {
            EnsureInit();
            restRotation = rotation;
            originPoint = planeOrigin != null ? planeOrigin.position : position;
            transform.SetPositionAndRotation(position, rotation);
            body.position = position;
            body.rotation = rotation;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            velocity = Vector3.zero;
            holdTarget = position;
            TargetTilt = 0f;
            IsGrabbed = false;
        }

        public void SetGrabbed(bool grabbed)
        {
            IsGrabbed = grabbed;
            if (!grabbed) holdTarget = body != null ? body.position : transform.position;
        }

        /// <summary>몸체를 멈추고 물리 조작을 끈다(받침 위에 놓은 컵 등).</summary>
        public void Freeze(bool frozen)
        {
            EnsureInit();
            if (frozen)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
                body.useGravity = authoredGravity;
            }
            else
            {
                body.isKinematic = false;
                body.useGravity = false;
            }
            velocity = Vector3.zero;
            holdTarget = body.position;
        }

        private void Update()
        {
            // 일시정지(timeScale 0) 중에 쌓인 입력이 재개 순간 한꺼번에 반영되지 않게
            if (!InputEnabled || Time.timeScale <= 0f) return;

            switch (grabMode)
            {
                case GrabMode.Always: IsGrabbed = true; break;
                case GrabMode.WhileGrabHeld:
                    bool held = hand.GrabHeld;
                    if (IsGrabbed && !held) holdTarget = body.position;
                    IsGrabbed = held;
                    break;
            }

            // 누르고 있는 동안 기울기는 FixedUpdate에서 고정 간격으로 쌓는다(프레임레이트와 무관)
            if (tiltMode == TiltMode.WheelSteps)
            {
                float tilt = hand.TiltDelta;
                if (tilt != 0f) TargetTilt = Mathf.Clamp(TargetTilt + tilt * tiltPerNotch, minTilt, maxTilt);
            }

            if (IsGrabbed && TryProjectPointer(out Vector3 p)) holdTarget = p;
        }

        private bool TryProjectPointer(out Vector3 point)
        {
            point = holdTarget;
            if (viewCamera == null) return false;
            Ray ray = viewCamera.ScreenPointToRay(hand.PointerPosition);
            var plane = new Plane(planeNormal.normalized, originPoint);
            if (!plane.Raycast(ray, out float enter)) return false;
            point = ray.GetPoint(enter);
            Vector3 offset = point - originPoint;
            if (offset.sqrMagnitude > maxDistanceFromOrigin * maxDistanceFromOrigin)
                point = originPoint + offset.normalized * maxDistanceFromOrigin;
            return true;
        }

        private void FixedUpdate()
        {
            if (body == null || body.isKinematic) return;
            float dt = Time.fixedDeltaTime;

            if (InputEnabled && tiltMode == TiltMode.HoldToTilt)
            {
                float rate = hand.TiltHeld ? tiltSpeed : -tiltReturnSpeed;
                TargetTilt = Mathf.Clamp(TargetTilt + rate * dt, minTilt, maxTilt);
            }

            // 위치: 스프링-감쇠 가속도로 속도를 정하고 Rigidbody에 넣는다
            Vector3 accel = (holdTarget - body.position) * springStrength - velocity * damping;
            velocity += accel * dt;
            if (velocity.sqrMagnitude > maxSpeed * maxSpeed) velocity = velocity.normalized * maxSpeed;
            body.linearVelocity = velocity;

            // 회전: 목표 기울기 쪽으로 각속도를 준다
            Quaternion target = restRotation * Quaternion.AngleAxis(TargetTilt, tiltAxisLocal);
            Quaternion delta = target * Quaternion.Inverse(body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (float.IsNaN(axis.x) || Mathf.Abs(angle) < 0.01f)
            {
                body.angularVelocity = Vector3.zero;
            }
            else
            {
                Vector3 w = axis * (angle * Mathf.Deg2Rad * rotationGain);
                if (w.sqrMagnitude > maxAngularSpeed * maxAngularSpeed) w = w.normalized * maxAngularSpeed;
                body.angularVelocity = w;
            }
        }
    }
}
