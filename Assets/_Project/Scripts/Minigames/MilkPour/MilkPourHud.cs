using TMPro;
using UnityEngine;

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

    public void SetSpill(float v01)
    {
        v01 = Mathf.Clamp01(v01);
        spillFill.anchorMax = new Vector2(v01, spillFill.anchorMax.y);
    }
}
