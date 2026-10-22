using TMPro;
using UnityEngine;

// 손님. 카운터 앞에 켜고 끄기만 한다. 머리 위 말풍선에 메뉴 이름과 인내도 막대를 보여 준다.
// 클릭·하이라이트는 같은 오브젝트의 Station(stationId "customer")이 맡는다
public class Customer : MonoBehaviour
{
    [Header("말풍선")]
    public Transform bubble;
    public TMP_Text menuText;
    [Tooltip("인내도 막대 전체 (수락 전에는 숨김)")]
    public GameObject patienceBar;
    [Tooltip("줄어드는 막대. 왼쪽 기준(pivot x = 0)이어야 한다")]
    public RectTransform patienceFill;
    [Tooltip("말풍선이 바라볼 카메라")]
    public Transform faceCamera;

    public void Show(MenuData menu)
    {
        gameObject.SetActive(true);
        menuText.text = menu.menuName;
        patienceBar.SetActive(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ratio: 남은 인내도 비율 (1 = 가득, 0 = 다 됨)
    public void SetPatience(bool visible, float ratio)
    {
        patienceBar.SetActive(visible);
        patienceFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    private void LateUpdate()
    {
        if (faceCamera != null) bubble.rotation = faceCamera.rotation;
    }
}
