using TMPro;
using UnityEngine;

// 우유 스팀 화면. 왼쪽 세로 온도계(목표 띠·위험선), 오른쪽 세로 거품 게이지(목표 띠), 피처 옆 깊이 띠(튐/공기/가열)와 화살표, 튐 경고, 시작 안내. 표시만 한다
public class SteamMilkHud : MonoBehaviour
{
    [Header("온도계 (왼쪽 세로)")]
    [Tooltip("채움. anchorMax.y = 온도 / 눈금 최대")]
    [SerializeField] private RectTransform tempFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 목표 온도 구간")]
    [SerializeField] private RectTransform tempBand;
    [Tooltip("위험선. anchorMin.y/anchorMax.y = 한계 온도")]
    [SerializeField] private RectTransform dangerLine;
    [SerializeField] private TMP_Text tempText;

    [Header("거품 게이지 (오른쪽 세로)")]
    [Tooltip("채움. anchorMax.y = 거품 비율")]
    [SerializeField] private RectTransform foamFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 목표 거품 구간")]
    [SerializeField] private RectTransform foamBand;
    [SerializeField] private TMP_Text foamText;

    [Header("깊이 띠 (피처 옆 세로, 위 = 표면)")]
    [Tooltip("튐 구간 칸. anchorMin.y = 1 - 공기 시작 깊이, anchorMax.y = 1")]
    [SerializeField] private RectTransform splashZone;
    [Tooltip("공기 구간 칸")]
    [SerializeField] private RectTransform airZone;
    [Tooltip("가열 구간 칸. anchorMin.y = 0, anchorMax.y = 1 - 가열 시작 깊이")]
    [SerializeField] private RectTransform heatZone;
    [Tooltip("현재 노즐 위치 화살표. anchorMin.y/anchorMax.y = 1 - 깊이")]
    [SerializeField] private RectTransform depthArrow;

    [Header("튐 경고")]
    [Tooltip("튐 누적. anchorMax.x = 누적 / 한계")]
    [SerializeField] private RectTransform splashFill;
    [Tooltip("표면 위에서 분사 중일 때만 켠다")]
    [SerializeField] private GameObject splashWarning;

    [Header("안내")]
    [SerializeField] private TMP_Text hintText;
    [Tooltip("안내 문구를 보여 주는 시간(초)")]
    [SerializeField] private float hintSeconds = 1.5f;
    [SerializeField] private string hintMessage = "마우스 위아래로 노즐 깊이! 클릭해서 스팀";

    private const float PercentScale = 100f;

    private float gaugeMax = 80f;
    private float hintTimer;

    public void Configure(float gaugeMaxTemp, float targetMinTemp, float targetMaxTemp, float failTemp,
        float foamMinPercent, float foamMaxPercent, float airStartDepth, float heatStartDepth)
    {
        gaugeMax = Mathf.Max(1f, gaugeMaxTemp);
        SetAnchorY(tempBand, targetMinTemp / gaugeMax, targetMaxTemp / gaugeMax);
        float danger = failTemp / gaugeMax;
        SetAnchorY(dangerLine, danger, danger);
        SetAnchorY(foamBand, foamMinPercent / PercentScale, foamMaxPercent / PercentScale);
        SetAnchorY(splashZone, 1f - airStartDepth, 1f);
        SetAnchorY(airZone, 1f - heatStartDepth, 1f - airStartDepth);
        SetAnchorY(heatZone, 0f, 1f - heatStartDepth);
        SetState(0f, 0f, 0f, 0f, false);
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

    // splash01 = 튐 누적 / 한계
    public void SetState(float temp, float foamPercent, float depth01, float splash01, bool splashing)
    {
        tempFill.anchorMax = new Vector2(tempFill.anchorMax.x, Mathf.Clamp01(temp / gaugeMax));
        tempText.SetText("{0}℃", Mathf.RoundToInt(temp));
        foamFill.anchorMax = new Vector2(foamFill.anchorMax.x, Mathf.Clamp01(foamPercent / PercentScale));
        foamText.SetText("거품 {0}%", Mathf.RoundToInt(foamPercent));
        float y = 1f - Mathf.Clamp01(depth01);
        depthArrow.anchorMin = new Vector2(depthArrow.anchorMin.x, y);
        depthArrow.anchorMax = new Vector2(depthArrow.anchorMax.x, y);
        splashFill.anchorMax = new Vector2(Mathf.Clamp01(splash01), splashFill.anchorMax.y);
        splashWarning.SetActive(splashing);
    }

    private void Update()
    {
        if (hintTimer <= 0f) return;
        if (Time.timeScale <= 0f) return;
        hintTimer -= Time.deltaTime;
        if (hintTimer <= 0f) hintText.gameObject.SetActive(false);
    }
}
