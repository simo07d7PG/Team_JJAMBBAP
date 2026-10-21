using TMPro;
using UnityEngine;

// 에스프레소 샷 화면. 오른쪽 세로 "샷 잔" 게이지(목표 띠 + "여기서 떼!") + 숫자 ml. 표시만 한다
public class EspressoShotHud : MonoBehaviour
{
    [Header("게이지")]
    [Tooltip("채움. anchorMax.y = 값")]
    [SerializeField] private RectTransform fill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform band;
    [Tooltip("\"여기서 떼!\" 글씨. 띠 가운데 높이로 옮긴다")]
    [SerializeField] private RectTransform bandLabel;
    [SerializeField] private TMP_Text capacityText;
    [SerializeField] private TMP_Text valueText;

    // 목표·용량을 맞추고 게이지를 비운다
    public void Configure(float targetMinMl, float targetMaxMl, float capacityMl)
    {
        float min01 = Mathf.Clamp01(targetMinMl / capacityMl);
        float max01 = Mathf.Clamp01(targetMaxMl / capacityMl);
        // 폭이 아주 좁은 구간도 보이게 최소 두께를 준다
        float width = Mathf.Max(max01 - min01, 0.02f);
        float mid = (min01 + max01) * 0.5f;
        band.anchorMin = new Vector2(band.anchorMin.x, mid - width * 0.5f);
        band.anchorMax = new Vector2(band.anchorMax.x, mid + width * 0.5f);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
        bandLabel.anchorMin = new Vector2(bandLabel.anchorMin.x, mid);
        bandLabel.anchorMax = new Vector2(bandLabel.anchorMax.x, mid);
        bandLabel.offsetMin = new Vector2(0f, -24f);
        bandLabel.offsetMax = new Vector2(0f, 24f);
        capacityText.SetText("{0} ml", Mathf.RoundToInt(capacityMl));
        SetAmount(0f, capacityMl);
    }

    public void SetAmount(float ml, float capacityMl)
    {
        float v = Mathf.Clamp01(ml / capacityMl);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        fill.gameObject.SetActive(v > 0f);
        valueText.SetText("{0} ml", Mathf.RoundToInt(ml));
    }
}
