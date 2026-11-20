using TMPro;
using UnityEngine;
using UImage = UnityEngine.UI.Image;

// 우유 붓기 화면. 왼쪽 컵 단면(우유 높이 + 목표 띠) + 퍼센트 + 아래 "흘림" 막대. 표시만 한다
public class MilkPourHud : MonoBehaviour
{
    [Header("컵 단면")]
    [Tooltip("우유 높이. anchorMax.y = 채움 비율")]
    [SerializeField] private RectTransform fill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform band;
    [SerializeField] private RectTransform bandLabel;
    [SerializeField] private TMP_Text percentText;

    [Header("흘림 막대")]
    [Tooltip("흘린 양. anchorMax.x = 흘린 양 / 흘림 한도")]
    [SerializeField] private RectTransform spillFill;

    // 목표 띠의 최소 두께(0~1)
    private const float MinBandWidth01 = 0.02f;
    // 띠 라벨의 위아래 절반 높이(px)
    private const float BandLabelHalfHeight = 24f;

    // 이미 든 양 띠 색(커피), 라벨 크기
    private static readonly Color BaseColor = new Color(0.32f, 0.18f, 0.09f, 1f);
    private const float BaseLabelHalfHeight = 14f;
    private const float BaseLabelFontSize = 18f;

    private RectTransform baseBar;
    private RectTransform baseLabel;
    private TMP_Text baseLabelText;

    public void Configure(float targetMin01, float targetMax01)
    {
        float width = Mathf.Max(targetMax01 - targetMin01, MinBandWidth01);
        float mid = (targetMin01 + targetMax01) * 0.5f;
        SetAnchorY(band, mid - width * 0.5f, mid + width * 0.5f);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
        SetAnchorY(bandLabel, mid, mid);
        // 라벨은 좌우 오프셋 0 고정(띠와 달리 기존 x 오프셋을 유지하지 않는다)
        bandLabel.offsetMin = new Vector2(0f, -BandLabelHalfHeight);
        bandLabel.offsetMax = new Vector2(0f, BandLabelHalfHeight);
        SetFill(0f);
        SetSpill(0f);
    }

    // 앵커의 x는 그대로 두고 y(최소·최대)만 바꾼다
    private static void SetAnchorY(RectTransform rt, float min, float max)
    {
        rt.anchorMin = new Vector2(rt.anchorMin.x, min);
        rt.anchorMax = new Vector2(rt.anchorMax.x, max);
    }

    public void SetFill(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v01);
        fill.gameObject.SetActive(v01 > 0f);
        percentText.SetText("{0}%", Mathf.RoundToInt(v01 * 100f));
    }

    // 앞 단계에서 이미 들어 있던 양(커피·얼음)을 우유와 구분해 맨 아래에 갈색 띠로 덮어 그린다
    public void SetBase(float base01, string label)
    {
        base01 = Mathf.Clamp01(base01);
        EnsureBaseWidgets();
        bool show = base01 > 0f;
        baseBar.gameObject.SetActive(show);
        baseLabel.gameObject.SetActive(show);
        if (!show) return;

        baseBar.anchorMax = new Vector2(baseBar.anchorMax.x, base01);
        baseLabel.anchorMin = new Vector2(baseLabel.anchorMin.x, base01 * 0.5f);
        baseLabel.anchorMax = new Vector2(baseLabel.anchorMax.x, base01 * 0.5f);
        baseLabelText.SetText(label);
    }

    // 갈색 띠와 라벨을 처음 한 번 코드로 만든다(fill과 같은 부모, fill 바로 위)
    private void EnsureBaseWidgets()
    {
        if (baseBar != null) return;
        Transform parent = fill.parent;

        GameObject barObj = new GameObject("BaseBar", typeof(RectTransform), typeof(UImage));
        baseBar = barObj.GetComponent<RectTransform>();
        baseBar.SetParent(parent, false);
        baseBar.SetSiblingIndex(fill.GetSiblingIndex() + 1);
        baseBar.anchorMin = new Vector2(fill.anchorMin.x, 0f);
        baseBar.anchorMax = new Vector2(fill.anchorMax.x, 0f);
        baseBar.offsetMin = new Vector2(fill.offsetMin.x, 0f);
        baseBar.offsetMax = new Vector2(fill.offsetMax.x, 0f);
        UImage img = barObj.GetComponent<UImage>();
        img.color = BaseColor;
        img.raycastTarget = false;

        GameObject labelObj = new GameObject("BaseLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        baseLabel = labelObj.GetComponent<RectTransform>();
        baseLabel.SetParent(parent, false);
        baseLabel.SetSiblingIndex(baseBar.GetSiblingIndex() + 1);
        baseLabel.anchorMin = new Vector2(fill.anchorMin.x, 0f);
        baseLabel.anchorMax = new Vector2(fill.anchorMax.x, 0f);
        baseLabel.offsetMin = new Vector2(fill.offsetMin.x, -BaseLabelHalfHeight);
        baseLabel.offsetMax = new Vector2(fill.offsetMax.x, BaseLabelHalfHeight);
        baseLabelText = labelObj.GetComponent<TextMeshProUGUI>();
        baseLabelText.font = percentText.font;
        baseLabelText.fontSize = BaseLabelFontSize;
        baseLabelText.alignment = TextAlignmentOptions.Center;
        baseLabelText.color = Color.white;
        baseLabelText.raycastTarget = false;
    }

    public void SetSpill(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        spillFill.anchorMax = new Vector2(v01, spillFill.anchorMax.y);
    }
}
