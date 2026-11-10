using TMPro;
using UnityEngine;

// 원두·물 넣기 화면. 드리퍼 옆 가루 게이지, 주전자 옆 물 게이지(세로, 목표 띠 + 퍼센트). 표시만 한다
public class DripSetupHud : MonoBehaviour
{
    [Header("가루 게이지 (드리퍼 옆)")]
    [Tooltip("가루 높이. anchorMax.y = 채움 비율")]
    [SerializeField] private RectTransform groundsFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform groundsBand;
    [SerializeField] private TMP_Text groundsPercentText;

    [Header("물 게이지 (주전자 옆)")]
    [Tooltip("물 높이. anchorMax.y = 채움 비율")]
    [SerializeField] private RectTransform waterFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform waterBand;
    [SerializeField] private TMP_Text waterPercentText;

    public void Configure(float groundsMin01, float groundsMax01, float waterMin01, float waterMax01)
    {
        SetBand(groundsBand, groundsMin01, groundsMax01);
        SetBand(waterBand, waterMin01, waterMax01);
        SetFills(0f, 0f);
    }

    public void SetFills(float grounds01, float water01)
    {
        SetFill(groundsFill, groundsPercentText, grounds01);
        SetFill(waterFill, waterPercentText, water01);
    }

    // 앵커의 x는 그대로 두고 y만 구간으로. 오프셋 y는 0
    private void SetBand(RectTransform band, float min01, float max01)
    {
        band.anchorMin = new Vector2(band.anchorMin.x, min01);
        band.anchorMax = new Vector2(band.anchorMax.x, max01);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
    }

    private void SetFill(RectTransform fill, TMP_Text percentText, float v01)
    {
        v01 = Mathf.Clamp01(v01);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v01);
        fill.gameObject.SetActive(v01 > 0f);
        percentText.SetText("{0}%", Mathf.RoundToInt(v01 * 100f));
    }
}
