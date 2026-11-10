using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 탬핑 화면. 왼쪽 세로 압력 게이지(목표 띠), 가운데 아래 가로 수평계, 오른쪽 세로 다지기 진행 막대, 시작 안내 문구. 표시만 한다
public class TampHud : MonoBehaviour
{
    [Header("압력 게이지 (왼쪽 세로)")]
    [Tooltip("채움. anchorMax.y = 압력 비율")]
    [SerializeField] private RectTransform pressureFill;
    [Tooltip("채움 색을 바꿀 그래픽")]
    [SerializeField] private Graphic pressureFillGraphic;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 목표 압력 비율")]
    [SerializeField] private RectTransform pressureBand;
    [Tooltip("100% 한계선. anchorMin.y/anchorMax.y = 한계 압력 비율")]
    [SerializeField] private RectTransform limitLine;
    [SerializeField] private TMP_Text pressureText;
    [Tooltip("게이지 꼭대기 압력(%). 한계선이 보이도록 100보다 조금 크게")]
    [SerializeField] private float gaugeMaxPercent = 110f;
    [Tooltip("채움 색: 평소 / 다지는 중 / 띠 위(너무 셈)")]
    [SerializeField] private Color normalFillColor = new Color(0.77f, 0.55f, 0.31f);
    [SerializeField] private Color goodFillColor = new Color(0.3f, 0.75f, 0.3f);
    [SerializeField] private Color highFillColor = new Color(0.98f, 0.84f, 0.44f);

    [Header("수평계 (가운데 아래 가로)")]
    [Tooltip("거품. anchorMin.x/anchorMax.x = 기울기 위치")]
    [SerializeField] private RectTransform levelBubble;
    [Tooltip("가운데 허용 띠. anchorMin.x/anchorMax.x = 허용 범위")]
    [SerializeField] private RectTransform levelBand;
    [SerializeField] private Graphic levelBubbleGraphic;
    [Tooltip("거품 색: 수평 / 기울어짐")]
    [SerializeField] private Color levelOkColor = new Color(0.3f, 0.75f, 0.3f);
    [SerializeField] private Color levelBadColor = new Color(0.57f, 0.15f, 0.17f);

    [Header("진행 (오른쪽 세로)")]
    [Tooltip("채움. anchorMax.y = 누적 / 필요 시간")]
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private TMP_Text progressText;
    [Tooltip("진행 글자 형식. {0} = 누적, {1} = 필요 시간")]
    [SerializeField] private string progressFormat = "{0:0.0} / {1:0.0}초";

    [Header("안내")]
    [SerializeField] private TMP_Text hintText;
    [Tooltip("안내 문구를 보여 주는 시간(초)")]
    [SerializeField] private float hintSeconds = 1.5f;
    [SerializeField] private string hintMessage = "클릭으로 누르고, 마우스로 수평!";

    private const float PercentScale = 100f;
    private const float HalfRange = 0.5f;

    private float holdNeeded = 1f;
    private float tiltRange = 1f;
    private float bandMaxPercent = 80f;
    private float hintTimer;

    // 판 시작: 띠·한계선·허용 범위를 맞추고 안내를 켠다. 압력은 1 = 100%, 기울기는 도
    public void Configure(float bandMin, float bandMax, float limit, float tiltRangeDegrees, float tolerance, float holdNeededSeconds)
    {
        holdNeeded = Mathf.Max(0.01f, holdNeededSeconds);
        tiltRange = Mathf.Max(0.01f, tiltRangeDegrees);
        bandMaxPercent = bandMax * PercentScale;

        float limit01 = PressureTo01(limit * PercentScale);
        pressureBand.anchorMin = new Vector2(pressureBand.anchorMin.x, PressureTo01(bandMin * PercentScale));
        pressureBand.anchorMax = new Vector2(pressureBand.anchorMax.x, PressureTo01(bandMax * PercentScale));
        limitLine.anchorMin = new Vector2(limitLine.anchorMin.x, limit01);
        limitLine.anchorMax = new Vector2(limitLine.anchorMax.x, limit01);

        levelBand.anchorMin = new Vector2(TiltTo01(-tolerance), levelBand.anchorMin.y);
        levelBand.anchorMax = new Vector2(TiltTo01(tolerance), levelBand.anchorMax.y);

        hintText.text = hintMessage;
        hintText.gameObject.SetActive(true);
        hintTimer = hintSeconds;

        SetPressure(0f, false);
        SetTilt(0f, true);
        SetProgress(0f);
    }

    // percent = 0~100 (넘으면 한계)
    public void SetPressure(float percent, bool tamping)
    {
        pressureFill.anchorMax = new Vector2(pressureFill.anchorMax.x, PressureTo01(percent));
        pressureText.SetText("{0}%", Mathf.RoundToInt(percent));
        if (pressureFillGraphic == null) return;
        if (tamping) pressureFillGraphic.color = goodFillColor;
        else if (percent > bandMaxPercent) pressureFillGraphic.color = highFillColor;
        else pressureFillGraphic.color = normalFillColor;
    }

    // degrees가 범위를 넘으면 거품은 끝에 붙는다
    public void SetTilt(float degrees, bool level)
    {
        float x = TiltTo01(degrees);
        levelBubble.anchorMin = new Vector2(x, levelBubble.anchorMin.y);
        levelBubble.anchorMax = new Vector2(x, levelBubble.anchorMax.y);
        if (levelBubbleGraphic != null) levelBubbleGraphic.color = level ? levelOkColor : levelBadColor;
    }

    public void SetProgress(float holdSeconds)
    {
        float v = Mathf.Clamp01(holdSeconds / holdNeeded);
        progressFill.anchorMax = new Vector2(progressFill.anchorMax.x, v);
        progressText.SetText(progressFormat, holdSeconds, holdNeeded);
    }

    private float PressureTo01(float percent)
    {
        return Mathf.Clamp01(percent / Mathf.Max(1f, gaugeMaxPercent));
    }

    private float TiltTo01(float degrees)
    {
        return Mathf.Clamp01(degrees / tiltRange * HalfRange + HalfRange);
    }

    // 안내 문구는 처음 잠깐만. 일시정지(timeScale 0) 중에는 시간도 멈춘다
    private void Update()
    {
        if (hintTimer <= 0f) return;
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f) hintText.gameObject.SetActive(false);
    }
}
