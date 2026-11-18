using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    // 손님 루트 원래 상태 (처음 Show 때 저장, Hide 때 복원)
    private Vector3 rootPos;
    private Vector3 rootScale;
    private Quaternion rootRot;
    private bool rootSaved;
    // 움직임 상태
    private float phase;
    private float popTime = PopRootSeconds;
    private float nodTime;
    private bool nervous;
    // 반응 상태 (Update로 몸을 움직이므로 StopAllCoroutines와 무관)
    private float reactTime = ReactSeconds;
    private bool reactHappy;
    private TMP_Text reactText;

    // 반응: 글자 떠오름·몸 움직임 시간(초), 떠오르는 높이(로컬), 점프 높이(m), 흔들림 폭(m)
    private const float ReactSeconds = 0.8f;
    private const float ReactRise = 0.25f;
    private const float ReactJump = 0.06f;
    private const float ReactShake = 0.02f;

    // 인내도 막대 색·깜빡임
    private static readonly Color PatienceGreen = new Color(0.45f, 0.85f, 0.4f);
    private static readonly Color PatienceYellow = new Color(0.98f, 0.82f, 0.3f);
    private static readonly Color PatienceRed = new Color(0.92f, 0.3f, 0.28f);
    private const float PatienceBlinkRatio = 0.2f;
    private const float PatienceBlinkPeriod = 0.25f;
    private Image patienceImage;
    private float patienceRatio = 1f;

    // 루트 등장 연출 시간(초), 아래에서 올라오는 거리(m)
    private const float PopRootSeconds = 0.35f;
    private const float PopRise = 0.02f;
    // 대기 흔들림: 진폭(m)·주기(초)·기울임(도). 초조할 때는 더 크고 빠르다
    private const float IdleAmp = 0.012f;
    private const float IdlePeriod = 2f;
    private const float IdleTilt = 1.2f;
    private const float NervousAmp = 0.018f;
    private const float NervousPeriod = 0.9f;
    private const float NervousTilt = 2.2f;
    private const float NervousRatio = 0.25f;
    // 떠날 때 끄덕임 깊이(m)
    private const float NodDepth = 0.008f;

    // 말풍선 등장 연출: 걸리는 시간(초), 1배를 넘치는 정도
    private const float PopSeconds = 0.2f;
    private const float PopOvershoot = 0.15f;
    // 떠날 때 말풍선이 줄어드는 시간(초)
    private const float ShrinkSeconds = 0.25f;

    // request: 주문 변형 글자 ("진하게, 우유 적게"). 비어 있으면 메뉴 이름만
    public void Show(MenuData menu, string request, bool greet = true)
    {
        StopAllCoroutines();
        leaving = false;
        gameObject.SetActive(true);
        SaveBubbleScale();
        SaveRoot();
        popTime = 0f;
        nodTime = 0f;
        nervous = false;
        ClearReact();
        if (request != "") menuText.text = menu.menuName + "\n<size=70%>" + request + "</size>";
        else menuText.text = menu.menuName;
        patienceBar.SetActive(false);
        if (greet) MinigameSfx.Play("sfx_greet", 0.5f);
        StartCoroutine(PopBubble());
    }

    // 루트 원래 위치·크기·회전을 처음 한 번만 저장한다
    private void SaveRoot()
    {
        if (rootSaved) return;
        rootPos = transform.localPosition;
        rootScale = transform.localScale;
        rootRot = transform.localRotation;
        rootSaved = true;
    }

    private void RestoreRoot()
    {
        if (!rootSaved) return;
        transform.localPosition = rootPos;
        transform.localScale = rootScale;
        transform.localRotation = rootRot;
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
        ClearReact();
        if (scaleSaved) bubble.localScale = bubbleScale;
        RestoreRoot();
        popTime = PopRootSeconds;
        gameObject.SetActive(false);
    }

    // 한마디 남기고 사라진다
    public void Leave(string message)
    {
        if (!gameObject.activeSelf) return;
        StopAllCoroutines();
        leaving = true;
        nodTime = 0f;
        menuText.text = message;
        MinigameSfx.Play("sfx_bye", 0.5f);
        StartCoroutine(LeaveRoutine());
    }

    // 반응: happy면 ♥ 글자가 뜨며 점프, 아니면 !? 글자가 뜨며 좌우로 흔들린다 (Leave 뒤에 불러도 된다)
    public void React(bool happy)
    {
        if (!gameObject.activeSelf || bubble == null || menuText == null) return;
        ClearReact();
        reactHappy = happy;
        reactTime = 0f;
        GameObject go = new GameObject("ReactText", typeof(RectTransform));
        go.transform.SetParent(bubble, false);
        reactText = go.AddComponent<TextMeshProUGUI>();
        reactText.font = menuText.font;
        reactText.fontSharedMaterial = menuText.fontSharedMaterial;
        reactText.alignment = TextAlignmentOptions.Center;
        reactText.fontSize = menuText.fontSize * 1.2f;
        reactText.raycastTarget = false;
        reactText.enableWordWrapping = false;
        reactText.text = happy ? "♥" : "!?";
        reactText.color = happy ? new Color(1f, 0.4f, 0.55f) : new Color(1f, 0.85f, 0.2f);
        RectTransform rect = reactText.rectTransform;
        RectTransform menuRect = menuText.rectTransform;
        rect.sizeDelta = new Vector2(200f, 100f);
        // 말풍선 위쪽 모서리 근처에서 시작
        rect.anchoredPosition = new Vector2(0f, menuRect.rect.height * 0.5f + 30f);
    }

    private void ClearReact()
    {
        reactTime = ReactSeconds;
        if (reactText != null) Destroy(reactText.gameObject);
        reactText = null;
    }

    // ratio: 남은 인내도 비율 (1 = 가득, 0 = 다 됨)
    public void SetPatience(bool visible, float ratio)
    {
        nervous = visible && ratio <= NervousRatio;
        patienceBar.SetActive(visible && !leaving);
        patienceFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);
        patienceRatio = Mathf.Clamp01(ratio);
        if (patienceImage == null) patienceImage = patienceFill.GetComponent<Image>();
        ApplyPatienceColor();
    }

    // 인내도 막대 색: 초록 → 노랑 → 빨강, 0.2 이하에서는 알파 깜빡임
    private void ApplyPatienceColor()
    {
        if (patienceImage == null) return;
        Color c = patienceRatio > 0.5f
            ? Color.Lerp(PatienceYellow, PatienceGreen, (patienceRatio - 0.5f) / 0.5f)
            : Color.Lerp(PatienceRed, PatienceYellow, patienceRatio / 0.5f);
        if (patienceRatio <= PatienceBlinkRatio && !leaving)
            c.a = Mathf.Repeat(Time.unscaledTime, PatienceBlinkPeriod) < PatienceBlinkPeriod * 0.5f ? 1f : 0.35f;
        patienceImage.color = c;
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

    // 대기 흔들림·등장·끄덕임. 코루틴이 아니라 Update라 StopAllCoroutines와 무관하다
    private void Update()
    {
        if (patienceBar != null && patienceBar.activeSelf && patienceRatio <= PatienceBlinkRatio) ApplyPatienceColor();
        if (!rootSaved) return;
        float dt = Time.deltaTime;
        bool hurry = nervous && !leaving;
        float amp = hurry ? NervousAmp : IdleAmp;
        float tilt = hurry ? NervousTilt : IdleTilt;
        float period = hurry ? NervousPeriod : IdlePeriod;
        phase += dt * Mathf.PI * 2f / period;
        float y = Mathf.Sin(phase) * amp;
        float z = Mathf.Sin(phase * 0.5f) * tilt;
        Vector3 mul = Vector3.one;

        if (popTime < PopRootSeconds)
        {
            popTime += dt;
            float p = Mathf.Clamp01(popTime / PopRootSeconds);
            // 크기 0.85 → 1.08 → 1
            float s = p < 0.6f ? Mathf.Lerp(0.85f, 1.08f, p / 0.6f) : Mathf.Lerp(1.08f, 1f, (p - 0.6f) / 0.4f);
            float xz = 1f + (1f - s) * 0.5f;
            mul = new Vector3(xz, s, xz);
            y -= PopRise * (1f - p);
        }

        if (leaving)
        {
            nodTime += dt;
            y -= Mathf.Sin(Mathf.Clamp01(nodTime / leaveSeconds) * Mathf.PI) * NodDepth;
        }

        float x = 0f;
        if (reactTime < ReactSeconds)
        {
            reactTime += dt;
            float p = Mathf.Clamp01(reactTime / ReactSeconds);
            if (reactHappy) y += Mathf.Sin(Mathf.Min(p * 2f, 1f) * Mathf.PI) * ReactJump;
            else x = Mathf.Sin(p * Mathf.PI * 6f) * ReactShake * (1f - p);
            if (reactText != null)
            {
                RectTransform rr = reactText.rectTransform;
                Vector2 pos = rr.anchoredPosition;
                rr.anchoredPosition = new Vector2(pos.x, pos.y + ReactRise * 100f * dt / ReactSeconds * 4f);
                Color c = reactText.color;
                c.a = 1f - Mathf.Clamp01((p - 0.5f) / 0.5f);
                reactText.color = c;
            }
            if (p >= 1f) ClearReact();
        }

        transform.localPosition = rootPos + new Vector3(x, y, 0f);
        transform.localRotation = rootRot * Quaternion.Euler(0f, 0f, z);
        transform.localScale = Vector3.Scale(rootScale, mul);
    }

    private void LateUpdate()
    {
        if (faceCamera != null) bubble.rotation = faceCamera.rotation;
    }
}
