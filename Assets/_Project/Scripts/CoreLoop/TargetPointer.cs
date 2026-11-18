using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 다음에 할 대상(기계·손님) 머리 위에 아래를 향한 화살표를 띄운다
// 대상이 화면 밖이면 화면 가장자리에 붙어 바깥쪽을 가리킨다
public class TargetPointer : MonoBehaviour
{
    public CafeView view;
    public Camera cam;
    [Tooltip("가운데 고리: 더 이상 쓰지 않아 항상 숨긴다")]
    public Image ring;
    [Tooltip("화살표 위치·회전 기준 (화면 위치로 옮긴다)")]
    public RectTransform arrowPivot;
    public Image arrow;
    [Tooltip("다음 할 일 글자 (화살표 위)")]
    public TMP_Text label;

    public Color arrowColor = new Color(1f, 0.82f, 0.35f, 1f);
    [Tooltip("화살표가 위아래로 튀는 폭(px)")]
    public float bob = 10f;

    // 화살표 크기 배율, 대상 위에 떨어뜨릴 여백(m), 말풍선 위 여백(m)
    private const float ArrowScale = 1.6f;
    private const float HoverScale = 0.8f;
    private const float TopMargin = 0.12f;
    private const float BubbleMargin = 0.25f;
    private const float BobSpeed = 6f;
    private const float EdgeMargin = 60f;
    private const float LabelGap = 6f;
    private const int LabelSize = 26;
    // 화살표 이미지가 위쪽을 향하므로 방향 각도에서 빼는 값
    private const float ArrowBaseAngle = -90f;

    private Station target;
    private RectTransform arrowRect;
    private RectTransform rootRect;
    private Canvas canvas;
    private Transform bubble;
    private Vector3 topPoint;

    private void Awake()
    {
        arrowRect = arrow.rectTransform;
        rootRect = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        arrowPivot.anchorMin = arrowPivot.anchorMax = new Vector2(0.5f, 0.5f);
        arrowPivot.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.localScale = Vector3.one * ArrowScale;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
        arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.anchoredPosition = Vector2.zero;
        arrow.color = arrowColor;
        if (arrow.GetComponent<Outline>() == null)
        {
            Outline o = arrow.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.12f, 0.07f, 0.03f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
        }
        label.fontSize = LabelSize;
        label.raycastTarget = false;
        arrow.raycastTarget = false;
        SetTarget(null, "");
    }

    // 대상이 없으면(null) 화살표·글자를 모두 숨긴다
    public void SetTarget(Station station, string text)
    {
        if (station != target)
        {
            target = station;
            bubble = null;
            if (station != null)
            {
                if (station.stationId == "customer")
                {
                    Customer c = station.GetComponentInChildren<Customer>(true);
                    if (c == null) c = station.GetComponentInParent<Customer>();
                    if (c != null) bubble = c.bubble;
                }
                if (bubble == null) topPoint = ComputeTopPoint(station);
            }
        }
        label.text = text;
    }

    // 대상 Renderer들 bounds 윗면 중앙 + 위로 TopMargin
    private Vector3 ComputeTopPoint(Station station)
    {
        Bounds b = new Bounds();
        bool has = false;
        Renderer[] rs = station.targets;
        if (rs == null || rs.Length == 0) rs = station.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rs.Length; i++)
        {
            Renderer r = rs[i];
            if (r == null || !r.gameObject.activeInHierarchy) continue;
            if (!has) b = r.bounds;
            else b.Encapsulate(r.bounds);
            has = true;
        }
        if (!has) return station.transform.position + Vector3.up * TopMargin;
        return new Vector3(b.center.x, b.max.y + TopMargin, b.center.z);
    }

    private void LateUpdate()
    {
        // 가운데 고리는 항상 숨긴다
        if (ring != null && ring.gameObject.activeSelf) ring.gameObject.SetActive(false);

        bool show = ShouldShow();
        SetVisible(show);
        if (!show) return;
        UpdateArrow();
    }

    // ESC 일시정지(timeScale 0) 중에도 숨긴다
    private bool ShouldShow()
    {
        return target != null && view.viewEnabled && !view.IsWalking() && target.gameObject.activeInHierarchy && Time.timeScale > 0f;
    }

    private void SetVisible(bool show)
    {
        arrowPivot.gameObject.SetActive(show);
        label.gameObject.SetActive(show);
        if (!show) return;
        arrow.gameObject.SetActive(true);
    }

    private Camera UiCamera()
    {
        if (canvas == null) return null;
        Canvas root = canvas.rootCanvas;
        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    private Vector2 ToLocal(RectTransform space, Vector2 screen)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(space, screen, UiCamera(), out local);
        return local;
    }

    private void UpdateArrow()
    {
        Vector3 world = bubble != null ? bubble.position + Vector3.up * BubbleMargin : topPoint;
        Vector3 sp = cam.WorldToScreenPoint(world);
        float w = Screen.width, h = Screen.height;
        bool behind = sp.z < 0f;
        bool inside = !behind && sp.x >= EdgeMargin && sp.x <= w - EdgeMargin && sp.y >= EdgeMargin && sp.y <= h - EdgeMargin;

        bool hovered = view.HoveredStation() == target;
        float scale = ArrowScale * (hovered ? HoverScale : 1f);
        arrowRect.localScale = Vector3.one * scale;
        float bobY = bob * Mathf.Sin(Time.time * BobSpeed);

        RectTransform parent = (RectTransform)arrowPivot.parent;
        Vector2 half = arrowRect.rect.size * scale * 0.5f;
        Vector2 screenPos;
        if (inside)
        {
            // 대상 위: 화살표 끝이 대상 쪽(아래)을 향한다
            screenPos = new Vector2(sp.x, sp.y);
            arrowPivot.localRotation = Quaternion.identity;
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
            arrowPivot.anchoredPosition = ToLocal(parent, screenPos) + new Vector2(0f, half.y + bobY);
            arrowRect.anchoredPosition = Vector2.zero;
            PlaceLabel(arrowPivot.anchoredPosition + new Vector2(0f, half.y + LabelGap), parent);
            return;
        }

        // 화면 밖/뒤: 가장자리에 붙이고 바깥쪽을 가리킨다
        Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
        Vector2 dir = new Vector2(sp.x, sp.y) - center;
        if (behind) dir = -dir;
        if (dir.sqrMagnitude < 1f) dir = Vector2.down;
        dir.Normalize();
        float sx = dir.x != 0f ? (w * 0.5f - EdgeMargin) / Mathf.Abs(dir.x) : float.MaxValue;
        float sy = dir.y != 0f ? (h * 0.5f - EdgeMargin) / Mathf.Abs(dir.y) : float.MaxValue;
        screenPos = center + dir * Mathf.Min(sx, sy);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + ArrowBaseAngle;
        arrowRect.localRotation = Quaternion.identity;
        arrowPivot.localRotation = Quaternion.Euler(0f, 0f, angle);
        // 튐은 바깥 방향으로
        arrowPivot.anchoredPosition = ToLocal(parent, screenPos) + (Vector2)dir * bobY;
        arrowRect.anchoredPosition = Vector2.zero;
        Vector2 labelOffset = -dir * (Mathf.Max(half.x, half.y) + LabelGap + label.rectTransform.rect.height * 0.5f);
        PlaceLabel(arrowPivot.anchoredPosition + labelOffset, parent);
    }

    // 글자를 화살표 쪽에 붙인다 (anchoredPosition은 arrowPivot의 부모 좌표 기준)
    private void PlaceLabel(Vector2 posInArrowParent, RectTransform arrowParent)
    {
        label.rectTransform.position = arrowParent.TransformPoint(posInArrowParent);
    }
}
