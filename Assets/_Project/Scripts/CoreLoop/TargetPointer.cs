using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 가운데 점을 둘러싼 고리와 화살표. 다음에 클릭할 대상(기계·손님) 쪽을 가리킨다
// 대상이 가운데 점에 걸리면 화살표를 숨기고 고리가 금색으로 바뀐다
public class TargetPointer : MonoBehaviour
{
    public CafeView view;
    public Camera cam;
    public Image ring;
    [Tooltip("화살표를 돌리는 중심 (고리 가운데). 화살표는 이 아래 위쪽(+y)에 둔다")]
    public RectTransform arrowPivot;
    public Image arrow;
    [Tooltip("다음 할 일 글자 (고리 아래)")]
    public TMP_Text label;

    public Color ringColor = new Color(1f, 1f, 1f, 0.55f);
    public Color onTargetColor = new Color(1f, 0.82f, 0.35f, 1f);
    [Tooltip("화살표가 고리 밖으로 들락거리는 폭(px)")]
    public float bob = 5f;

    // 화살표를 숨길 최소 거리의 제곱 (정면 판정)
    private const float MinArrowDistanceSqr = 0.0004f;
    // 대상이 걸렸을 때 고리 맥동 크기·속도
    private const float PulseAmount = 0.08f;
    private const float PulseSpeed = 10f;
    // 화살표 들락거림 속도
    private const float BobSpeed = 8f;
    // 화살표 이미지가 위쪽을 향하므로 방향 각도에서 빼는 값
    private const float ArrowBaseAngle = -90f;

    private Station target;
    private RectTransform arrowRect;
    private float arrowRestY;

    private void Awake()
    {
        arrowRect = arrow.rectTransform;
        arrowRestY = arrowRect.anchoredPosition.y;
        SetTarget(null, "");
    }

    // 대상이 없으면(null) 고리·화살표·글자를 모두 숨긴다
    public void SetTarget(Station station, string text)
    {
        target = station;
        label.text = text;
    }

    private void LateUpdate()
    {
        bool show = ShouldShow();
        SetVisible(show);
        if (!show) return;

        if (view.HoveredStation() == target)
        {
            ShowOnTarget();
            return;
        }
        ring.color = ringColor;
        ring.rectTransform.localScale = Vector3.one;
        UpdateArrow();
    }

    // ESC 일시정지(timeScale 0) 중에도 숨긴다
    private bool ShouldShow()
    {
        return target != null && view.viewEnabled && !view.IsWalking() && target.gameObject.activeInHierarchy && Time.timeScale > 0f;
    }

    private void SetVisible(bool show)
    {
        ring.gameObject.SetActive(show);
        arrowPivot.gameObject.SetActive(show);
        label.gameObject.SetActive(show);
    }

    private void HideArrow()
    {
        arrow.gameObject.SetActive(false);
    }

    // 대상이 가운데 점에 걸린 상태: 화살표를 숨기고 금색 고리가 맥동한다
    private void ShowOnTarget()
    {
        HideArrow();
        ring.color = onTargetColor;
        float pulse = 1f + PulseAmount * Mathf.Sin(Time.time * PulseSpeed);
        ring.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
    }

    // 대상 방향으로 화살표를 돌리고 위아래로 들락거리게 한다
    private void UpdateArrow()
    {
        // 화면 가운데에서 대상까지의 방향 (뒤쪽이면 반대로 뒤집는다)
        Vector3 viewportPos = cam.WorldToViewportPoint(view.LookPoint(target));
        Vector2 dir = new Vector2((viewportPos.x - 0.5f) * cam.aspect, viewportPos.y - 0.5f);
        if (viewportPos.z < 0f) dir = -dir;
        // 거의 정면이면 방향이 의미 없으니 화살표를 숨긴다 (걷기가 끝나는 프레임 등)
        if (dir.sqrMagnitude < MinArrowDistanceSqr)
        {
            HideArrow();
            return;
        }

        arrow.gameObject.SetActive(true);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + ArrowBaseAngle;
        arrowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        Vector2 arrowPos = arrowRect.anchoredPosition;
        arrowPos.y = arrowRestY + bob * Mathf.Sin(Time.time * BobSpeed);
        arrowRect.anchoredPosition = arrowPos;
    }
}
