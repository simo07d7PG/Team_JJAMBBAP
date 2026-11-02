using System.Collections;
using TMPro;
using UnityEngine;

// 손님. 카운터 앞에 켜고 끄기만 한다. 머리 위 말풍선에 메뉴 이름과 인내도 막대를 보여 준다.
// 클릭·하이라이트는 같은 오브젝트의 Station(stationId "customer")이 맡는다.
// 등장할 때 말풍선이 튀어나오고, 떠날 때 한마디 남긴 뒤 말풍선이 줄어든다.
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
    [Tooltip("떠날 때 한마디를 보여 주는 시간(초)")]
    public float leaveSeconds = 1f;

    // 말풍선 원래 크기 (처음 Show 때 저장)
    private Vector3 bubbleScale;
    private bool scaleSaved;
    private bool leaving;

    // 말풍선 등장 연출: 걸리는 시간(초), 1배를 넘치는 정도
    private const float PopSeconds = 0.2f;
    private const float PopOvershoot = 0.15f;
    // 떠날 때 말풍선이 줄어드는 시간(초)
    private const float ShrinkSeconds = 0.25f;

    // request: 주문 변형 글자 ("진하게, 우유 적게"). 비어 있으면 메뉴 이름만
    public void Show(MenuData menu, string request)
    {
        StopAllCoroutines();
        leaving = false;
        gameObject.SetActive(true);
        SaveBubbleScale();
        if (request != "") menuText.text = menu.menuName + "\n<size=70%>" + request + "</size>";
        else menuText.text = menu.menuName;
        patienceBar.SetActive(false);
        StartCoroutine(PopBubble());
    }

    // 말풍선 원래 크기를 처음 한 번만 저장한다
    private void SaveBubbleScale()
    {
        if (scaleSaved) return;
        bubbleScale = bubble.localScale;
        scaleSaved = true;
    }

    public void Hide()
    {
        if (scaleSaved) bubble.localScale = bubbleScale;
        gameObject.SetActive(false);
    }

    // 한마디 남기고 사라진다
    public void Leave(string message)
    {
        if (!gameObject.activeSelf) return;
        StopAllCoroutines();
        leaving = true;
        menuText.text = message;
        StartCoroutine(LeaveRoutine());
    }

    // ratio: 남은 인내도 비율 (1 = 가득, 0 = 다 됨)
    public void SetPatience(bool visible, float ratio)
    {
        patienceBar.SetActive(visible && !leaving);
        patienceFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
    }

    private IEnumerator PopBubble()
    {
        float t = 0f;
        while (t < PopSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / PopSeconds);
            float s = p + Mathf.Sin(p * Mathf.PI) * PopOvershoot;
            bubble.localScale = bubbleScale * s;
            yield return null;
        }
        bubble.localScale = bubbleScale;
    }

    private IEnumerator LeaveRoutine()
    {
        yield return new WaitForSeconds(leaveSeconds);
        float t = 0f;
        while (t < ShrinkSeconds)
        {
            t += Time.deltaTime;
            bubble.localScale = bubbleScale * (1f - Mathf.Clamp01(t / ShrinkSeconds));
            yield return null;
        }
        Hide();
    }

    private void LateUpdate()
    {
        if (faceCamera != null) bubble.rotation = faceCamera.rotation;
    }
}
