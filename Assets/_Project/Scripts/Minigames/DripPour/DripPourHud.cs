using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 핸드드립 붓기 화면. 가로 색 띠 + 표시선, 추출 게이지(목표선), 남은 기회 점, "+25" 글자. 표시만 한다
public class DripPourHud : MonoBehaviour
{
    [Header("색 띠")]
    [Tooltip("띠 칸 7개(왼쪽부터). 같은 부모(띠 영역) 아래에 두고 위아래로 늘린다. anchorMin.x/anchorMax.x를 코드가 넣는다")]
    [SerializeField] private Image[] bandSegments;
    [Tooltip("표시선. 띠 영역 안, anchorMin.x = anchorMax.x = 위치")]
    [SerializeField] private Image marker;
    [Tooltip("띠 색: 빨강 / 주황 / 노랑 / 초록")]
    [SerializeField] private Color redColor = new Color(0.78f, 0.27f, 0.22f);
    [SerializeField] private Color orangeColor = new Color(0.91f, 0.55f, 0.24f);
    [SerializeField] private Color yellowColor = new Color(0.96f, 0.82f, 0.36f);
    [SerializeField] private Color greenColor = new Color(0.45f, 0.70f, 0.33f);

    [Header("추출 게이지")]
    [Tooltip("채움. anchorMax.x = 게이지 / 게이지 끝")]
    [SerializeField] private RectTransform gaugeFill;
    [Tooltip("목표선. anchorMin.x = anchorMax.x = 목표 / 게이지 끝")]
    [SerializeField] private RectTransform targetLine;
    [Tooltip("\"85 / 100\" 숫자")]
    [SerializeField] private TMP_Text gaugeText;

    [Header("남은 기회")]
    [Tooltip("기회 점(왼쪽부터). 기회 수보다 많은 점은 숨긴다")]
    [SerializeField] private Image[] pourDots;
    [SerializeField] private Color dotLeftColor = MinigameUiPalette.Gold;
    [SerializeField] private Color dotUsedColor = MinigameUiPalette.InkDeep;

    [Header("+점수 글자")]
    [Tooltip("멈춘 색으로 뜨는 \"+25\"")]
    [SerializeField] private TMP_Text gainText;
    [Tooltip("글자 뒤 어두운 바탕(글자를 자식으로). 띠 영역 안에 두면 표시선 위치로 옮긴다. 비우면 글자만 옮긴다")]
    [SerializeField] private Image gainBadge;
    [Tooltip("떠오르는 시간(초)과 높이(px)")]
    [SerializeField] private float gainSeconds = 0.6f;
    [SerializeField] private float gainRisePixels = 50f;

    [Header("연출")]
    [Tooltip("멈춘 순간 표시선이 커지는 정도")]
    [SerializeField] private float markerPulseScale = 0.6f;

    // 게이지 숫자 형식
    private const string GaugeFormat = "{0} / {1}";
    // 표시선 튐이 가라앉는 속도
    private const float PulseDecaySpeed = 4f;
    // "+25" 글자 형식
    private const string GainFormat = "+{0}";

    private bool saved;
    private Vector3 markerRestScale;
    private RectTransform gainMover;
    private Vector2 gainBasePos;
    private Color badgeColor;
    private float targetValue;
    private float pulse;
    private float gainElapsed;
    private bool gainShowing;
    private Color gainColor;

    // 처음 쓸 때 원래 크기·위치·색을 기억한다 (Configure가 Awake보다 먼저 불릴 수 있다)
    private void SaveRest()
    {
        if (saved) return;
        saved = true;
        markerRestScale = marker.rectTransform.localScale;
        if (gainBadge != null)
        {
            gainMover = gainBadge.rectTransform;
            badgeColor = gainBadge.color;
        }
        else if (gainText != null)
        {
            gainMover = gainText.rectTransform;
        }
        if (gainMover != null) gainBasePos = gainMover.anchoredPosition;
    }

    // 띠 경계(0~1, 칸 수 + 1개)와 칸별 색 종류로 띠를 놓고, 게이지·기회를 비운다
    public void Configure(float[] edges, int[] kinds, int pourCount, float targetGauge, float gaugeMax)
    {
        SaveRest();
        for (int i = 0; i < bandSegments.Length; i++)
        {
            bool used = i < kinds.Length && i + 1 < edges.Length;
            bandSegments[i].gameObject.SetActive(used);
            if (!used) continue;
            RectTransform rt = bandSegments[i].rectTransform;
            SetAnchorX(rt, edges[i], edges[i + 1]);
            rt.offsetMin = new Vector2(0f, rt.offsetMin.y);
            rt.offsetMax = new Vector2(0f, rt.offsetMax.y);
            bandSegments[i].color = KindColor(kinds[i]);
        }

        targetValue = targetGauge;
        float target01 = gaugeMax > 0f ? Mathf.Clamp01(targetGauge / gaugeMax) : 1f;
        SetAnchorX(targetLine, target01, target01);

        for (int i = 0; i < pourDots.Length; i++)
        {
            pourDots[i].gameObject.SetActive(i < pourCount);
        }

        pulse = 0f;
        marker.rectTransform.localScale = markerRestScale;
        HideGain();
    }

    // 앵커의 y는 그대로 두고 x 범위만 바꾼다
    private static void SetAnchorX(RectTransform rt, float minX, float maxX)
    {
        rt.anchorMin = new Vector2(minX, rt.anchorMin.y);
        rt.anchorMax = new Vector2(maxX, rt.anchorMax.y);
    }

    // 표시선 위치(0~1)
    public void SetMarker(float pos01)
    {
        SetAnchorX(marker.rectTransform, pos01, pos01);
        marker.rectTransform.anchoredPosition = new Vector2(0f, marker.rectTransform.anchoredPosition.y);
    }

    public void SetGauge(float gauge, float gaugeMax)
    {
        float v = gaugeMax > 0f ? Mathf.Clamp01(gauge / gaugeMax) : 0f;
        gaugeFill.anchorMax = new Vector2(v, gaugeFill.anchorMax.y);
        gaugeFill.gameObject.SetActive(v > 0f);
        if (gaugeText != null) gaugeText.SetText(GaugeFormat, Mathf.RoundToInt(gauge), Mathf.RoundToInt(targetValue));
    }

    public void SetPoursLeft(int left)
    {
        for (int i = 0; i < pourDots.Length; i++)
        {
            pourDots[i].color = i < left ? dotLeftColor : dotUsedColor;
        }
    }

    // 멈춘 순간: 표시선이 튀고, 그 자리에서 멈춘 색으로 "+25"가 떠오른다
    // (표시선 색은 바꾸지 않는다. 같은 색 띠 위에서 안 보이게 되므로)
    public void ShowGain(int kind, float gain, float pos01)
    {
        SaveRest();
        pulse = 1f;
        if (gainText == null) return;

        SetAnchorX(gainMover, pos01, pos01);
        gainMover.anchoredPosition = gainBasePos;
        gainText.SetText(GainFormat, Mathf.RoundToInt(gain));
        gainColor = KindColor(kind);
        gainText.color = gainColor;
        if (gainBadge != null)
        {
            gainBadge.color = badgeColor;
            gainBadge.gameObject.SetActive(true);
        }
        gainText.gameObject.SetActive(true);
        gainElapsed = 0f;
        gainShowing = true;
    }

    private void HideGain()
    {
        gainShowing = false;
        if (gainBadge != null) gainBadge.gameObject.SetActive(false);
        else if (gainText != null) gainText.gameObject.SetActive(false);
    }

    private Color KindColor(int kind)
    {
        switch (kind)
        {
            case DripPourMicrogame.KindGreen: return greenColor;
            case DripPourMicrogame.KindYellow: return yellowColor;
            case DripPourMicrogame.KindOrange: return orangeColor;
            default: return redColor;
        }
    }

    private void Update()
    {
        if (!saved) return;
        float dt = Time.deltaTime;

        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - dt * PulseDecaySpeed);
            marker.rectTransform.localScale = markerRestScale * (1f + markerPulseScale * pulse);
        }

        if (gainShowing)
        {
            gainElapsed += dt;
            float t = gainSeconds > 0f ? Mathf.Clamp01(gainElapsed / gainSeconds) : 1f;
            gainMover.anchoredPosition = gainBasePos + Vector2.up * (gainRisePixels * t);
            // 끝으로 갈수록 흐려진다
            Color c = gainColor;
            c.a = gainColor.a * (1f - t);
            gainText.color = c;
            if (gainBadge != null)
            {
                Color b = badgeColor;
                b.a = badgeColor.a * (1f - t);
                gainBadge.color = b;
            }
            if (t >= 1f) HideGain();
        }
    }
}
