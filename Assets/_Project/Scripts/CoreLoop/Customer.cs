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

    [Header("걷기")]
    [Tooltip("등장 시작점: 카운터 자리 기준 로컬 오프셋")]
    [SerializeField] private Vector3 enterOffset = new Vector3(-2.2f, 0f, -0.6f);
    [Tooltip("퇴장 도착점: 카운터 자리 기준 로컬 오프셋")]
    [SerializeField] private Vector3 exitOffset = new Vector3(2.2f, 0f, -0.6f);
    [SerializeField] private float arriveSeconds = 0.9f;
    [SerializeField] private float exitSeconds = 0.8f;

    // 걷기 상태: 0 없음, 1 들어옴, 2 나감
    private int walkMode;
    private float walkT;
    private Vector3 walkOffset;
    private Vector3 walkFrom;
    private float walkPhase;
    private const float WalkBob = 0.04f;
    private const float WalkTilt = 5f;
    private const float WalkStepsPerSecond = 2.2f;

    // 글자 찍힘 (초당 글자 수)
    private const float TypeSpeed = 30f;
    private bool typing;
    private float typeCount;
    private int typeTotal;

    public bool IsArriving { get { return walkMode == 1; } }

    // 성격: 0 보통, 1 급한 손님, 2 느긋한 손님 (Show 전에 SetPersonality로 정한다)
    private int personality;
    public int Personality { get { return personality; } }
    public void SetPersonality(int value) { personality = Mathf.Clamp(value, 0, 2); }

    // 시선·몸짓 (Update에서 한 곳으로 합성)
    private float gazeYaw;
    private float stompTimer;
    private float stompT = 1f;
    private float nodStepTime = 1f;
    private bool patienceVisible;
    private const float GazeSpeed = 120f;
    private const float GazeMax = 50f;
    private const float StompInterval = 1.6f;
    private const float StompSeconds = 0.3f;
    private const float StompHeight = 0.03f;
    private const float ArmsTilt = 3f;
    private const float StepNodSeconds = 0.4f;
    private const float StepNodAngle = 8f;

    // 말풍선 원래 크기 (처음 Show 때 저장)
    private Vector3 bubbleScale;
    private bool scaleSaved;
    private bool leaving;

    // 손님 루트 원래 상태 (처음 Show 때 저장, Hide 때 복원)
    private Vector3 rootPos;
    private Vector3 rootScale;
    private Vector3 rootBaseScale;
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
        stepReacting = false;
        stepRoutine = null;
        leaving = false;
        gameObject.SetActive(true);
        SaveBubbleScale();
        SetupMenuText();
        SaveRoot();
        ApplyScaleVariation();
        ApplyTint();
        gazeYaw = 0f;
        stompTimer = 0f;
        stompT = 1f;
        nodStepTime = 1f;
        patienceVisible = false;
        popTime = 0f;
        nodTime = 0f;
        nervous = false;
        walkMode = 0;
        walkOffset = Vector3.zero;
        ClearReact();
        menuText.text = CustomerLines.Order(menu.menuName, request, personality);
        patienceBar.SetActive(false);
        if (greet)
        {
            // 옆에서 걸어 들어온다. 말풍선은 도착 후에 등장
            walkMode = 1;
            walkT = 0f;
            walkOffset = enterOffset;
            popTime = PopRootSeconds;
            bubble.localScale = Vector3.zero;
            StopTyping();
            menuText.maxVisibleCharacters = 0;
            MinigameSfx.Play("sfx_greet", 0.5f);
        }
        else
        {
            StopTyping();
            StartCoroutine(PopBubble());
        }
    }

    // 말풍선 글자 설정: 한 곳에서 처리 (주문·한마디·단계 반응·성격 줄 모두 적용)
    // 원래 글자 크기의 60%를 최대로 자동 축소하고, 좌우·위 여백을 8%쯤 둔다(아래는 인내도 막대 영역이라 비워 둠)
    private bool menuTextSetup;
    private void SetupMenuText()
    {
        if (menuTextSetup || menuText == null) return;
        menuTextSetup = true;
        float baseSize = menuText.fontSize;
        menuText.enableAutoSizing = true;
        menuText.fontSizeMax = baseSize * 0.6f;
        menuText.fontSizeMin = baseSize * 0.3f;
        menuText.fontSize = menuText.fontSizeMax;
        menuText.enableWordWrapping = true;
        menuText.overflowMode = TextOverflowModes.Truncate;
        menuText.alignment = TextAlignmentOptions.Center;
        // x=좌, y=아래, z=우, w=위 (rect 크기 기준 8%)
        Rect r = menuText.rectTransform.rect;
        float mx = r.width * 0.08f;
        float my = r.height * 0.08f;
        menuText.margin = new Vector4(mx, my, mx, my);
        // 표정·반응 글자는 원래 크기 기준이라 따로 쓰지 않도록 보관
        reactBaseSize = baseSize;
    }
    private float reactBaseSize = 44f;

    // 글자 찍힘 시작 (현재 menuText 기준)
    private void StartTyping()
    {
        typeTotal = menuText.GetParsedText().Length;
        typeCount = 0f;
        typing = typeTotal > 0;
        menuText.maxVisibleCharacters = typing ? 0 : int.MaxValue;
    }

    private void StopTyping()
    {
        typing = false;
        menuText.maxVisibleCharacters = int.MaxValue;
    }

    // 루트 원래 위치·크기·회전을 처음 한 번만 저장한다
    private void SaveRoot()
    {
        if (rootSaved) return;
        rootPos = transform.localPosition;
        rootScale = transform.localScale;
        rootBaseScale = rootScale;
        rootRot = transform.localRotation;
        rootSaved = true;
    }

    // 손님마다 크기를 0.96~1.04배로 아주 조금 다르게 한다 (원래 scale 기준)
    private void ApplyScaleVariation()
    {
        if (!rootSaved) return;
        rootScale = rootBaseScale * Random.Range(0.96f, 1.04f);
        transform.localScale = rootScale;
    }

    private void RestoreRoot()
    {
        if (!rootSaved) return;
        transform.localPosition = rootPos;
        transform.localScale = rootScale;
        transform.localRotation = rootRot;
    }

    // 말풍선 원래 크기를 처음 한 번만 저장한다
    // 옷 색 6가지를 돌려 가며 원래 색에 섞는다 (직전과 같은 색은 피함)
    private static readonly Color[] TintPalette =
    {
        new Color(0.85f, 0.45f, 0.40f), new Color(0.40f, 0.65f, 0.85f), new Color(0.55f, 0.78f, 0.45f),
        new Color(0.92f, 0.75f, 0.35f), new Color(0.70f, 0.50f, 0.80f), new Color(0.45f, 0.75f, 0.72f),
    };
    private int tintIndex = -1;

    // 성격별 색 제한: 팔레트 번호 (급한 손님 = 붉은 계열, 느긋한 손님 = 푸른/초록 계열)
    private static readonly int[] HurryTints = { 0, 3 };
    private static readonly int[] RelaxTints = { 1, 2, 5 };

    private void ApplyTint()
    {
        Station station = GetComponent<Station>();
        if (station == null) return;
        int[] allowed = personality == 1 ? HurryTints : (personality == 2 ? RelaxTints : null);
        int next = allowed != null ? allowed[Random.Range(0, allowed.Length)] : Random.Range(0, TintPalette.Length);
        if (next == tintIndex) next = allowed != null ? allowed[(System.Array.IndexOf(allowed, next) + 1) % allowed.Length] : (next + 1) % TintPalette.Length;
        tintIndex = next;
        station.TintBase(TintPalette[next], Random.Range(0.3f, 0.45f));
    }

    private void SaveBubbleScale()
    {
        if (scaleSaved) return;
        bubbleScale = bubble.localScale;
        scaleSaved = true;
    }

    public void Hide()
    {
        ClearReact();
        walkMode = 0;
        walkOffset = Vector3.zero;
        StopTyping();
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
        stepReacting = false;
        stepRoutine = null;
        leaving = true;
        nodTime = 0f;
        // 걸어 들어오는 중이면 현재 위치에서 멈추고 말풍선을 보여 준다
        walkMode = 0;
        if (scaleSaved) bubble.localScale = bubbleScale;
        menuText.text = message;
        StartTyping();
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
        reactText.fontSize = reactBaseSize * 0.8f;
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

    // 미니게임 한 단계 직후 반응: 말풍선 문구를 잠깐 바꿨다가 원래 주문 문구로 돌아온다
    private const float StepReactionSeconds = 1.2f;
    private bool stepReacting;
    private string stepSavedText;
    private Coroutine stepRoutine;

    public void StepReaction(bool success, bool last = false)
    {
        if (!gameObject.activeSelf || leaving || walkMode == 1 || menuText == null) return;
        if (stepReacting) { menuText.text = stepSavedText; }
        else stepSavedText = menuText.text;
        if (success) nodStepTime = 0f;
        stepReacting = true;
        menuText.text = CustomerLines.StepReaction(success, last);
        StartTyping();
        if (stepRoutine != null) StopCoroutine(stepRoutine);
        stepRoutine = StartCoroutine(StepReactionRoutine());
    }

    private IEnumerator StepReactionRoutine()
    {
        yield return new WaitForSeconds(StepReactionSeconds);
        stepReacting = false;
        stepRoutine = null;
        menuText.text = stepSavedText;
        StartTyping();
    }

    // ratio: 남은 인내도 비율 (1 = 가득, 0 = 다 됨)
    // 말풍선에는 막대·표정을 띄우지 않는다(화면 상단 막대가 대신 보여 줌). 몸짓 판정용으로 비율만 저장
    public void SetPatience(bool visible, float ratio)
    {
        nervous = visible && ratio <= NervousRatio;
        patienceVisible = visible;
        patienceRatio = Mathf.Clamp01(ratio);
        if (patienceBar != null && patienceBar.activeSelf) patienceBar.SetActive(false);
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
        bubble.localScale = Vector3.zero;
        // 반대쪽으로 걸어 나간다
        walkFrom = walkOffset;
        walkT = 0f;
        walkMode = 2;
        while (walkMode == 2) yield return null;
        Hide();
    }

    // 대기 흔들림·등장·끄덕임. 코루틴이 아니라 Update라 StopAllCoroutines와 무관하다
    private void Update()
    {
        if (!rootSaved) return;
        float dt = Time.deltaTime;

        if (typing)
        {
            typeCount += TypeSpeed * dt;
            if (typeCount >= typeTotal) StopTyping();
            else menuText.maxVisibleCharacters = (int)typeCount;
        }

        // 걷기: 위치 보간 + 걸음 보빙·기울임 + 진행 방향 바라보기
        float walkY = 0f, walkZ = 0f;
        Quaternion walkRot = Quaternion.identity;
        if (walkMode != 0)
        {
            Vector3 from = walkMode == 1 ? enterOffset : walkFrom;
            Vector3 to = walkMode == 1 ? Vector3.zero : exitOffset;
            float dur = walkMode == 1 ? arriveSeconds : exitSeconds;
            walkT += dt;
            float p = Mathf.Clamp01(walkT / Mathf.Max(0.01f, dur));
            float e = walkMode == 1 ? 1f - (1f - p) * (1f - p) : p * p * (3f - 2f * p);
            walkOffset = Vector3.Lerp(from, to, e);
            walkPhase += dt * Mathf.PI * 2f * WalkStepsPerSecond;
            walkY = Mathf.Abs(Mathf.Sin(walkPhase)) * WalkBob;
            walkZ = Mathf.Sin(walkPhase) * WalkTilt;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                // 도착이 가까워지면 원래 회전으로 돌아온다
                float w = walkMode == 1 ? 1f - Mathf.Clamp01((p - 0.8f) / 0.2f) : 1f;
                walkRot = Quaternion.Euler(0f, yaw * w, 0f);
            }
            if (p >= 1f)
            {
                if (walkMode == 1)
                {
                    walkMode = 0;
                    walkOffset = Vector3.zero;
                    popTime = 0f;
                    StartCoroutine(PopBubble());
                    StartTyping();
                }
                else walkMode = 0;
            }
        }
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

        // 몸짓: 발 구르기(0.3 이하), 좌우 기울이기(0.6 이하), 단계 성공 끄덕임
        bool waiting = patienceVisible && !leaving && walkMode == 0;
        if (waiting && patienceRatio <= 0.3f)
        {
            stompTimer += dt;
            if (stompTimer >= StompInterval) { stompTimer = 0f; stompT = 0f; }
        }
        else stompTimer = 0f;
        if (stompT < 1f)
        {
            stompT += dt / StompSeconds;
            y += Mathf.Abs(Mathf.Sin(Mathf.Clamp01(stompT) * Mathf.PI * 2f)) * StompHeight;
        }
        if (waiting && patienceRatio <= 0.6f) z += Mathf.Sin(Time.time * Mathf.PI * 2f / 4f) * ArmsTilt;
        float nodX = 0f;
        if (nodStepTime < 1f)
        {
            nodStepTime += dt / StepNodSeconds;
            nodX = Mathf.Sin(Mathf.Clamp01(nodStepTime) * Mathf.PI) * StepNodAngle;
        }

        // 시선: 걷는 중이 아니면 카메라 쪽으로 Y축만 천천히 돌린다
        Transform cam = faceCamera != null ? faceCamera : (Camera.main != null ? Camera.main.transform : null);
        float gazeTarget = 0f;
        if (cam != null && walkMode == 0 && !leaving)
        {
            Vector3 toCam = cam.position - transform.position;
            toCam.y = 0f;
            Vector3 axis = -cam.forward;
            axis.y = 0f;
            if (toCam.sqrMagnitude > 0.0001f && axis.sqrMagnitude > 0.0001f)
                gazeTarget = Mathf.Clamp(Vector3.SignedAngle(axis, toCam, Vector3.up), -GazeMax, GazeMax);
        }
        gazeYaw = Mathf.MoveTowards(gazeYaw, gazeTarget, GazeSpeed * dt);

        y += walkY;
        z += walkZ;
        transform.localPosition = rootPos + walkOffset + new Vector3(x, y, 0f);
        transform.localRotation = walkRot * Quaternion.Euler(0f, gazeYaw, 0f) * rootRot * Quaternion.Euler(nodX, 0f, z);
        transform.localScale = Vector3.Scale(rootScale, mul);
    }

    private void LateUpdate()
    {
        if (faceCamera != null) bubble.rotation = faceCamera.rotation;
    }
}
