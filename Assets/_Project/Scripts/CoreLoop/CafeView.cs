using UnityEngine;
using UnityEngine.InputSystem;

// 카페 카메라에 붙인다. 이동 없이 마우스를 화면 좌우 가장자리에 두면 그쪽으로 화면을 돌린다
public class CafeView : MonoBehaviour
{
    [Header("입력 (InputSystem_Actions > UI)")]
    public InputActionReference pointAction;

    [Header("화면 회전")]
    [Tooltip("화면 폭에서 가장자리로 보는 비율 (0.08 = 8%)")]
    [Range(0.01f, 0.4f)]
    public float edgeRatio = 0.08f;
    [Tooltip("회전 속도(도/초)")]
    public float turnSpeed = 60f;
    [Tooltip("시작 방향 기준 왼쪽으로 돌 수 있는 각도")]
    public float leftLimit = 60f;
    [Tooltip("시작 방향 기준 오른쪽으로 돌 수 있는 각도")]
    public float rightLimit = 60f;

    [Tooltip("끄면 회전하지 않는다 (미니게임 중)")]
    public bool viewEnabled = true;

    private float startYaw;
    private float yawOffset;

    private void Start()
    {
        pointAction.action.Enable();
        startYaw = transform.eulerAngles.y;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void Update()
    {
        if (!viewEnabled) return;
        Turn();
    }

    private void Turn()
    {
        Vector2 pointer = pointAction.action.ReadValue<Vector2>();
        float edge = Screen.width * edgeRatio;
        float dir = 0f;
        if (pointer.x <= edge) dir = -1f;
        if (pointer.x >= Screen.width - edge) dir = 1f;
        // 화면 밖에 있으면 돌리지 않는다
        if (pointer.x < 0f || pointer.x > Screen.width) dir = 0f;
        if (dir == 0f) return;

        yawOffset += dir * turnSpeed * Time.deltaTime;
        yawOffset = Mathf.Clamp(yawOffset, -leftLimit, rightLimit);
        Vector3 angles = transform.eulerAngles;
        angles.y = startYaw + yawOffset;
        transform.eulerAngles = angles;
    }
}
