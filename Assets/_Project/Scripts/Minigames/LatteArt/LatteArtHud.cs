using TMPro;
using UnityEngine;

// 라떼 아트 화면. 남은 붓는 양 세로 막대 + 채운 점 n/N 글자. 표시만 한다
public class LatteArtHud : MonoBehaviour
{
    [Tooltip("남은 붓는 양. anchorMax.y = 남은 비율")]
    [SerializeField] private RectTransform inkFill;
    [SerializeField] private TMP_Text countText;

    public void SetState(float ink01, int filled, int total)
    {
        inkFill.anchorMax = new Vector2(inkFill.anchorMax.x, Mathf.Clamp01(ink01));
        countText.SetText("{0}/{1}", filled, total);
    }
}
