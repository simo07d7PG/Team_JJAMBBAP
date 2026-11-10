using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 포타필터 담기 화면. 오른쪽 세로 게이지(목표 띠 + 경고 띠 + 100% 선) + 현재 g + 유지 진행 막대 + 시작 안내 문구. 표시만 한다
public class PortafilterFillHud : MonoBehaviour
{
    [Header("게이지 (오른쪽 세로)")]
    [Tooltip("채움. anchorMax.y = 담긴 양 / 게이지 꼭대기")]
    [SerializeField] private RectTransform fill;
    [Tooltip("채움 색을 바꿀 그래픽")]
    [SerializeField] private Graphic fillGraphic;
    [Tooltip("목표 띠(초록). anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform band;
    [Tooltip("경고 띠(주황). 목표 최대 ~ 넘침 한계")]
    [SerializeField] private RectTransform warnBand;
    [Tooltip("넘침 선. anchorMin.y/anchorMax.y = 한계")]
    [SerializeField] private RectTransform overflowLine;
    [SerializeField] private TMP_Text gramText;
    [Tooltip("게이지 꼭대기(1 = 100%). 한계선이 보이도록 1보다 조금 크게")]
    [SerializeField] private float gaugeTop = 1.1f;
    [Tooltip("채움 색: 평소 / 목표 안 / 경고(95% 이상)")]
    [SerializeField] private Color normalFillColor = new Color(0.77f, 0.55f, 0.31f);
    [SerializeField] private Color goodFillColor = new Color(0.3f, 0.75f, 0.3f);
    [SerializeField] private Color warnFillColor = new Color(0.95f, 0.55f, 0.2f);

    [Header("유지 진행")]
    [Tooltip("채움. anchorMax.x = 진행")]
    [SerializeField] private RectTransform holdFill;

    [Header("안내")]
    [SerializeField] private TMP_Text hintText;
    [Tooltip("안내 문구를 보여 주는 시간(초)")]
    [SerializeField] private float hintSeconds = 1.5f;
    [SerializeField] private string hintMessage = "마우스로 따라가며 누르고 있어!";

    private float hintTimer;

    // 판 시작: 띠·한계선을 맞추고 안내를 켠다. 비율은 1 = 100%
    public void Configure(float targetMin, float targetMax, float overflowLimit, float capacityGrams)
    {
        gaugeTop = Mathf.Max(overflowLimit, gaugeTop);
        SetAnchorY(band, targetMin / gaugeTop, targetMax / gaugeTop);
        SetAnchorY(warnBand, targetMax / gaugeTop, overflowLimit / gaugeTop);
        float line = overflowLimit / gaugeTop;
        SetAnchorY(overflowLine, line, line);
        SetState(0f, capacityGrams, false, false);
        SetHold(0f);
        hintText.SetText(hintMessage);
        hintText.gameObject.SetActive(true);
        hintTimer = hintSeconds;
    }

    // 앵커의 x는 그대로 두고 y(최소·최대)만 바꾼다
    private static void SetAnchorY(RectTransform rt, float min, float max)
    {
        rt.anchorMin = new Vector2(rt.anchorMin.x, Mathf.Clamp01(min));
        rt.anchorMax = new Vector2(rt.anchorMax.x, Mathf.Clamp01(max));
    }

    public void SetState(float grams, float capacityGrams, bool warn, bool inTarget)
    {
        float v = Mathf.Clamp01(grams / Mathf.Max(0.01f, capacityGrams) / gaugeTop);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        fill.gameObject.SetActive(v > 0f);
        gramText.SetText("{0:0}g / {1:0}g", grams, capacityGrams);
        Color color = normalFillColor;
        if (warn) color = warnFillColor;
        else if (inTarget) color = goodFillColor;
        fillGraphic.color = color;
    }

    public void SetHold(float v01)
    {
        holdFill.anchorMax = new Vector2(Mathf.Clamp01(v01), holdFill.anchorMax.y);
    }

    private void Update()
    {
        if (hintTimer <= 0f) return;
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f) hintText.gameObject.SetActive(false);
    }
}
