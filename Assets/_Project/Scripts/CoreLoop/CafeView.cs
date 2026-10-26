using UnityEngine;
using UnityEngine.InputSystem;

// 카페 카메라에 붙인다. 이동 없이 1인칭으로 마우스를 움직여 시야를 돌린다.
// 커서는 숨기고 화면 가운데 점(크로스헤어)으로 기계를 가리킨다
public class CafeView : MonoBehaviour
{
    [Header("입력 (InputSystem_Actions)")]
    [Tooltip("Player/Look (마우스 이동량)")]
    public InputActionReference lookAction;
    [Tooltip("UI/Click")]
    public InputActionReference clickAction;

    [Header("시야 회전")]
    [Tooltip("마우스 1픽셀당 도는 각도")]
    public float sensitivity = 0.08f;
    [Tooltip("시작 방향 기준 좌우로 돌 수 있는 각도")]
    public float yawLimit = 70f;
    [Tooltip("시작 방향 기준 위아래로 돌 수 있는 각도")]
    public float pitchLimit = 30f;
    [Tooltip("가장자리로 갈수록 무거워지는 정도 (0 = 없음, 1 = 끝에서 멈춤)")]
    [Range(0f, 1f)]
    public float resistance = 0.85f;
    [Tooltip("시야가 따라오는 빠르기. 낮을수록 부드럽다")]
    public float smoothing = 14f;

    [Header("기계 가리키기")]
    [Tooltip("가운데 점 아래 기계를 찾는 최대 거리")]
    public float rayDistance = 20f;
    [Tooltip("화면 가운데 점 (Canvas의 Image)")]
    public GameObject crosshair;

    [Tooltip("끄면 회전·하이라이트·클릭을 하지 않고 커서를 보여 준다 (미니게임 중)")]
    public bool viewEnabled = true;

    // 클릭한 기계. 관리 스크립트가 Update에서 읽고 null로 비운다
    [HideInInspector] public Station clickedStation;

    private Camera cam;
    private Station hovered;
    private float startYaw;
    private float startPitch;
    // 목표 각도와 실제 각도 (시작 방향 기준)
    private float targetYaw;
    private float targetPitch;
    private float yaw;
    private float pitch;
    private bool cursorLocked;
    // 마지막으로 일시정지였던 프레임. ESC 메뉴 버튼 클릭이 재개 직후 기계 클릭으로 넘어가지 않게
    private int pausedFrame = -10;

    private void Start()
    {
        lookAction.action.Enable();
        clickAction.action.Enable();
        cam = GetComponent<Camera>();
        Vector3 angles = transform.eulerAngles;
        startYaw = angles.y;
        startPitch = angles.x;
        if (startPitch > 180f) startPitch -= 360f;
        SetCursorLocked(true);
    }

    private void Update()
    {
        // 일시정지(timeScale 0) 중에는 시야·클릭 입력을 버린다. 쌓인 회전이 재개 순간 반영되지 않게
        if (Time.timeScale <= 0f)
        {
            pausedFrame = Time.frameCount;
            return;
        }
        // ESC 메뉴가 닫히며 커서를 잠그므로, 재개 직후 카페·미니게임·영수증에 맞게 다시 둔다
        if (Time.frameCount == pausedFrame + 1) SetCursorLocked(viewEnabled);
        if (viewEnabled != cursorLocked) SetCursorLocked(viewEnabled);
        if (!viewEnabled)
        {
            SetHovered(null);
            return;
        }
        Look();
        Hover();
        if (hovered != null && clickAction.action.WasPressedThisFrame() && Time.frameCount > pausedFrame + 1)
        {
            clickedStation = hovered;
        }
    }

    private void Look()
    {
        Vector2 delta = lookAction.action.ReadValue<Vector2>() * sensitivity;
        targetYaw = Move(targetYaw, delta.x, yawLimit);
        targetPitch = Move(targetPitch, -delta.y, pitchLimit);

        float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        yaw = Mathf.Lerp(yaw, targetYaw, t);
        pitch = Mathf.Lerp(pitch, targetPitch, t);
        transform.rotation = Quaternion.Euler(startPitch + pitch, startYaw + yaw, 0f);
    }

    // 가운데에서 멀어지는 쪽으로 움직일 때만 저항을 준다. 가운데로 돌아올 때는 가볍다
    private float Move(float angle, float amount, float limit)
    {
        bool outward = (angle >= 0f && amount > 0f) || (angle <= 0f && amount < 0f);
        if (outward)
        {
            float edge = Mathf.Clamp01(Mathf.Abs(angle) / limit);
            amount *= 1f - resistance * edge * edge;
        }
        return Mathf.Clamp(angle + amount, -limit, limit);
    }

    // 화면 가운데 아래 클릭 가능한 기계만 하이라이트
    // 클릭할 수 없는 기계는 통과해 뒤의 손님·기계까지 찾고, 벽 같은 기계 아닌 물체에서는 멈춘다
    private void Hover()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);

        float blocked = rayDistance;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.GetComponentInParent<Station>() == null && hits[i].distance < blocked)
            {
                blocked = hits[i].distance;
            }
        }

        Station found = null;
        float nearest = blocked;
        for (int i = 0; i < hits.Length; i++)
        {
            Station station = hits[i].collider.GetComponentInParent<Station>();
            if (station != null && station.clickable && hits[i].distance <= nearest)
            {
                found = station;
                nearest = hits[i].distance;
            }
        }
        SetHovered(found);
    }

    private void SetHovered(Station station)
    {
        if (hovered == station) return;
        if (hovered != null) hovered.SetHighlight(false);
        hovered = station;
        if (hovered != null) hovered.SetHighlight(true);
    }

    private void SetCursorLocked(bool locked)
    {
        cursorLocked = locked;
        Cursor.visible = !locked;
        Cursor.lockState = CursorLockMode.None;
        if (locked) Cursor.lockState = CursorLockMode.Locked;
        if (crosshair != null) crosshair.SetActive(locked);
    }
}
