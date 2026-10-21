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

    public void Configure(float targetMin01, float targetMax01)
    {
        float width = Mathf.Max(targetMax01 - targetMin01, 0.02f);
        float mid = (targetMin01 + targetMax01) * 0.5f;
        band.anchorMin = new Vector2(band.anchorMin.x, mid - width * 0.5f);
        band.anchorMax = new Vector2(band.anchorMax.x, mid + width * 0.5f);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
        bandLabel.anchorMin = new Vector2(bandLabel.anchorMin.x, mid);
        bandLabel.anchorMax = new Vector2(bandLabel.anchorMax.x, mid);
        bandLabel.offsetMin = new Vector2(0f, -24f);
        bandLabel.offsetMax = new Vector2(0f, 24f);
        SetFill(0f);
        SetSpill(0f);
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
