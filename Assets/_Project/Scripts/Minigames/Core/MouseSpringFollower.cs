using UnityEngine;

// 포인터를 평면에 투영한 점을 Rigidbody가 스프링·감쇠로 따라간다.
// 기울이기 버튼을 누르는 동안 지정 축으로 기울고, 떼면 다시 선다. 속도로 움직이므로 위에 올린 물체(얼음 등)도 물리로 따라온다.
// 포인터·잡기·기울이기 값은 이 물체를 쓰는 미니게임이 매 프레임 넣어 준다.
[RequireComponent(typeof(Rigidbody))]
public class MouseSpringFollower : MonoBehaviour
{
    public enum GrabMode
    {
        Always,         // 항상 따라간다(스쿱)
        WhileGrabHeld,  // 잡기 버튼을 누르고 있을 때만 따라간다(우유팩)
    }

    [Header("참조")]
    public Camera viewCamera;
    [Tooltip("이동 평면의 기준점. 비우면 시작 위치")]
    [SerializeField] private Transform planeOrigin;

    [Header("이동")]
    [SerializeField] private GrabMode grabMode = GrabMode.Always;
    [SerializeField] private Vector3 planeNormal = Vector3.up;
    [Tooltip("평면 기준점에서 벗어날 수 있는 최대 거리")]
    [SerializeField] private float maxDistanceFromOrigin = 1.5f;
    [Tooltip("켜면 기준점의 월드 Z에 고정한다")]
    [SerializeField] private bool lockWorldZ;
    [Tooltip("켜면 기준점 대비 월드 X 이동 범위를 제한한다")]
    [SerializeField] private bool limitWorldX;
    [Tooltip("기준점 대비 월드 X 최솟값(보통 음수)")]
    [SerializeField] private float minXFromOrigin = -1f;
    [Tooltip("기준점 대비 월드 X 최댓값")]
    [SerializeField] private float maxXFromOrigin = 1f;
    [Tooltip("스프링 강도. 클수록 빨리 따라온다")]
    [SerializeField] private float springStrength = 150f;
    [Tooltip("감쇠. 작을수록 출렁인다 (임계 감쇠 = 2 x 루트 강도)")]
    [SerializeField] private float damping = 16f;
    [SerializeField] private float maxSpeed = 5f;

    [Header("기울이기")]
    [Tooltip("기울이는 로컬 축")]
    [SerializeField] private Vector3 tiltAxisLocal = Vector3.right;
    [Tooltip("누르고 있는 동안 초당 기울어지는 각도")]
    [SerializeField] private float tiltSpeed = 90f;
    [Tooltip("떼면 초당 되돌아오는 각도")]
    [SerializeField] private float tiltReturnSpeed = 180f;
    [SerializeField] private float minTilt = 0f;
    [SerializeField] private float maxTilt = 120f;
    [Tooltip("목표 기울기로 회전을 맞추는 속도")]
    [SerializeField] private float rotationGain = 12f;
    [SerializeField] private float maxAngularSpeed = 12f;

    // 미니게임이 매 프레임 넣어 주는 입력
    [HideInInspector] public bool inputEnabled;
    [HideInInspector] public Vector2 pointerPosition;
    [HideInInspector] public bool grabHeld;
    [HideInInspector] public bool tiltHeld;

    [HideInInspector] public bool isGrabbed;
    [HideInInspector] public float targetTilt;

    private Rigidbody body;
    private Vector3 originPoint;
    private Vector3 holdTarget;
    private Quaternion restRotation;
    private Vector3 velocity;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        restRotation = transform.rotation;
        originPoint = transform.position;
        if (planeOrigin != null) originPoint = planeOrigin.position;
        holdTarget = transform.position;
        if (lockWorldZ) body.constraints = body.constraints | RigidbodyConstraints.FreezePositionZ;
    }

    public Rigidbody GetBody()
    {
        return body;
    }

    // 세워진 상태에서 실제로 기울어진 각도(도). 출렁임 포함
    public float ActualTilt()
    {
        return Quaternion.Angle(restRotation, body.rotation);
    }

    // 처음 상태로 되돌린다. 위치·회전·속도·기울기·잡기를 모두 초기화
    public void ResetTo(Vector3 position, Quaternion rotation)
    {
        restRotation = rotation;
        originPoint = position;
        if (planeOrigin != null) originPoint = planeOrigin.position;
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
        targetTilt = 0f;
        isGrabbed = false;
        inputEnabled = false;
        grabHeld = false;
        tiltHeld = false;
    }

    private void Update()
    {
        // 일시정지(timeScale 0) 중에 쌓인 입력이 재개 순간 한꺼번에 반영되지 않게
        if (!inputEnabled || Time.timeScale <= 0f) return;

        if (grabMode == GrabMode.Always)
        {
            isGrabbed = true;
        }
        else
        {
            if (isGrabbed && !grabHeld) holdTarget = body.position;
            isGrabbed = grabHeld;
        }

        if (isGrabbed) ProjectPointer();
    }

    private void ProjectPointer()
    {
        Ray ray = viewCamera.ScreenPointToRay(pointerPosition);
        Plane plane = new Plane(planeNormal.normalized, originPoint);
        float enter;
        if (!plane.Raycast(ray, out enter)) return;

        Vector3 point = ray.GetPoint(enter);
        if (lockWorldZ) point.z = originPoint.z;
        if (limitWorldX) point.x = Mathf.Clamp(point.x, originPoint.x + minXFromOrigin, originPoint.x + maxXFromOrigin);
        Vector3 offset = point - originPoint;
        if (offset.sqrMagnitude > maxDistanceFromOrigin * maxDistanceFromOrigin)
            point = originPoint + offset.normalized * maxDistanceFromOrigin;
        holdTarget = point;
    }

    private void FixedUpdate()
    {
        if (body.isKinematic) return;
        float dt = Time.fixedDeltaTime;

        // 기울기는 고정 간격으로 쌓는다(프레임레이트와 무관)
        if (inputEnabled)
        {
            float rate = -tiltReturnSpeed;
            if (tiltHeld) rate = tiltSpeed;
            targetTilt = Mathf.Clamp(targetTilt + rate * dt, minTilt, maxTilt);
        }

        // 위치: 스프링-감쇠 가속도로 속도를 정하고 Rigidbody에 넣는다
        Vector3 accel = (holdTarget - body.position) * springStrength - velocity * damping;
        velocity += accel * dt;
        if (lockWorldZ) velocity.z = 0f;
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed) velocity = velocity.normalized * maxSpeed;
        body.linearVelocity = velocity;

        // 회전: 목표 기울기 쪽으로 각속도를 준다
        Quaternion target = restRotation * Quaternion.AngleAxis(targetTilt, tiltAxisLocal);
        Quaternion delta = target * Quaternion.Inverse(body.rotation);
        float angle;
        Vector3 axis;
        delta.ToAngleAxis(out angle, out axis);
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
