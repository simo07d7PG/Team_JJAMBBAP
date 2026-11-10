using TMPro;
using UnityEngine;

// 물 타기 화면. 세로 눈금 막대(0~컵 용량, 목표 띠·넘침 선) + 현재 ml + 밸브 열림 게이지 + 유지 진행 막대. 표시만 한다
public class WaterTopUpHud : MonoBehaviour
{
    [Header("눈금 막대")]
    [Tooltip("부은 양. anchorMax.y = 부은 양 / 컵 용량")]
    [SerializeField] private RectTransform fill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform band;
    [Tooltip("넘침 선. anchorMin.y/anchorMax.y = 넘침 한계")]
    [SerializeField] private RectTransform overflowLine;
    [SerializeField] private TMP_Text mlText;

    [Header("게이지")]
    [Tooltip("밸브 열림. anchorMax.x = 열림")]
    [SerializeField] private RectTransform valveFill;
    [Tooltip("유지 진행. anchorMax.x = 진행")]
    [SerializeField] private RectTransform holdFill;

    private float capacityMl = 300f;

    public void Configure(float targetMinMl, float targetMaxMl, float overflowMl, float cupCapacityMl)
    {
        capacityMl = Mathf.Max(1f, cupCapacityMl);
        SetAnchorY(band, targetMinMl / capacityMl, targetMaxMl / capacityMl);
        float line = overflowMl / capacityMl;
        SetAnchorY(overflowLine, line, line);
        SetState(0f, 0f);
        SetHold(0f);
    }

    // 앵커의 x는 그대로 두고 y(최소·최대)만 바꾼다
    private static void SetAnchorY(RectTransform rt, float min, float max)
    {
        rt.anchorMin = new Vector2(rt.anchorMin.x, Mathf.Clamp01(min));
        rt.anchorMax = new Vector2(rt.anchorMax.x, Mathf.Clamp01(max));
    }

    public void SetState(float pouredMl, float openness01)
    {
        float v = Mathf.Clamp01(pouredMl / capacityMl);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v);
        fill.gameObject.SetActive(v > 0f);
        mlText.SetText("{0}ml", Mathf.RoundToInt(pouredMl));
        valveFill.anchorMax = new Vector2(Mathf.Clamp01(openness01), valveFill.anchorMax.y);
    }

    public void SetHold(float v01)
    {
        holdFill.anchorMax = new Vector2(Mathf.Clamp01(v01), holdFill.anchorMax.y);
    }
}
