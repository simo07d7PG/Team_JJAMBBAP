using UnityEngine;

// "커피 받아라!" — 마우스 좌우로 포타필터를 그라인더 토출구(좌우로 흔들림) 아래에 맞추고, 좌클릭을 누르는 동안 가루를 받는다.
// 줄기 밑에서 받은 만큼만 담기고 나머지는 흘린다. 좌클릭을 뗀 채 80~95%에 잠깐 머물면 성공, 100%를 넘으면 즉시 실패.
// 컵은 쓰지 않는다(포타필터는 컵이 아님). 주문 변형도 없다.
public class PortafilterFillMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private PortafilterFillHud hud;
    [Tooltip("그라인더 토출구. 로컬 X로 좌우 흔들린다")]
    [SerializeField] private Transform spout;
    [Tooltip("포타필터(바스켓 + 손잡이). 로컬 X로 마우스를 따라간다. 가운데일 때 토출구 기준점 아래")]
    [SerializeField] private Transform basket;
    [Tooltip("가루 줄기 피벗. 위쪽이 원점, localScale.y = 길이. 자식 메시는 로컬 Y -1~0을 덮는 단위 길이")]
    [SerializeField] private Transform stream;
    [Tooltip("바스켓 안 가루 피벗. 원점이 바스켓 바닥, localScale.y = 높이. 자식 메시는 로컬 Y 0~1을 덮는 단위 높이")]
    [SerializeField] private Transform powderPivot;
    [Tooltip("가루 색을 바꿀 렌더러(선택). 95% 이상이면 경고색")]
    [SerializeField] private Renderer powderRenderer;
    [Tooltip("흘린 가루가 떨어지는 책상 높이(월드 Y만 쓴다)")]
    [SerializeField] private Transform tableLevel;
    [Tooltip("흘린 가루 조각들(미리 놓고 꺼 둔다). 흘린 양에 따라 하나씩 켜진다")]
    [SerializeField] private Transform[] spillPieces;
    [Tooltip("좌클릭 중 미세하게 떨리는 그라인더 몸체(선택)")]
    [SerializeField] private Transform shakeTarget;

    [Header("목표")]
    [Tooltip("바스켓 용량(g). 가득 = 100%")]
    [SerializeField] private float capacityGrams = 20f;
    [Tooltip("성공 구간 (1 = 100%)")]
    [SerializeField] private float targetMin = 0.8f;
    [SerializeField] private float targetMax = 0.95f;
    [Tooltip("이 값을 넘으면 즉시 실패")]
    [SerializeField] private float overflowLimit = 1f;
    [Tooltip("좌클릭을 뗀 채 구간 안에 머물러야 하는 시간(초)")]
    [SerializeField] private float holdSeconds = 0.4f;

    [Header("토출")]
    [Tooltip("좌클릭 중 초당 나오는 가루(g)")]
    [SerializeField] private float flowGramsPerSec = 8f;
    [Tooltip("좌클릭을 떼도 이만큼 더 흘러나온다(초)")]
    [SerializeField] private float tailSeconds = 0.15f;
    [Tooltip("줄기와 바스켓 중심의 X 거리가 이 값 이내면 받는다(m)")]
    [SerializeField] private float catchHalfWidth = 0.09f;

    [Header("흔들림·이동")]
    [Tooltip("토출구 흔들림 진폭(m)")]
    [SerializeField] private float swayAmplitude = 0.22f;
    [Tooltip("토출구 흔들림 주기(초)")]
    [SerializeField] private float swayPeriod = 1.6f;
    [Tooltip("마우스가 화면 가장자리(가운데에서 폭의 절반)일 때 바스켓 이동 거리(m). 진폭보다 커야 한다")]
    [SerializeField] private float basketRange = 0.4f;
    [Tooltip("바스켓이 마우스를 따라가는 속도. 클수록 빨리 붙는다(1/초)")]
    [SerializeField] private float followSharpness = 25f;

    [Header("등급")]
    [Tooltip("등급에서 채움 근접도가 차지하는 비율 (나머지는 흘림 점수)")]
    [SerializeField] private float fillQualityWeight = 0.7f;
    [Tooltip("흘림 비율에 곱하는 감점 배율")]
    [SerializeField] private float spillPenalty = 1.5f;

    [Header("연출")]
    [SerializeField] private float streamWidth = 0.03f;
    [Tooltip("가루 100%일 때 높이(m)")]
    [SerializeField] private float powderMaxHeight = 0.05f;
    [Tooltip("좌클릭 중 몸체 떨림 크기(m)")]
    [SerializeField] private float shakeAmount = 0.01f;
    [Tooltip("몸체 떨림 속도(초당 회전수)")]
    [SerializeField] private float shakeHz = 25f;
    [Tooltip("흘린 가루 한 조각이 나타나는 흘림 양(g)")]
    [SerializeField] private float spillGramsPerPiece = 0.8f;
    [Tooltip("흘린 가루 조각의 X 흩어짐(m)")]
    [SerializeField] private float spillScatter = 0.03f;
    [Tooltip("가루 색: 평소 / 95% 이상 경고")]
    [SerializeField] private Color powderNormalColor = new Color(0.36f, 0.22f, 0.12f);
    [SerializeField] private Color powderWarnColor = new Color(0.85f, 0.35f, 0.15f);

    // 일시정지 판정: timeScale이 이 값 이하면 입력을 받지 않는다
    private const float PausedTimeScale = 0f;
    private const float ScreenCenterRatio = 0.5f;
    private const float MinStreamLength = 0.001f;
    private const float Epsilon = 0.0001f;

    // 결과 문구
    private const string TextSuccess = "딱 맞아!";
    private const string TextOverflow = "넘쳤어!";
    private const string TextShort = "덜 담겼어!";
    private const string TextTooMuch = "너무 많아!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private float fillGrams;
    private float spilledGrams;
    private float tailLeft;
    private float holdTimer;
    private float swayTime;
    private float swayPhase;
    private float basketX;
    private int shownSpillPieces;

    private MaterialPropertyBlock block;
    private Vector3 streamRestScale;
    private Vector3 powderRestScale;
    private float spoutRestX;
    private float basketRestX;
    private Vector3 shakeRestLocal;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        streamRestScale = stream.localScale;
        powderRestScale = powderPivot.localScale;
        spoutRestX = spout.localPosition.x;
        basketRestX = basket.localPosition.x;
        if (shakeTarget != null) shakeRestLocal = shakeTarget.localPosition;
    }

    protected override void ResetGame()
    {
        fillGrams = 0f;
        spilledGrams = 0f;
        tailLeft = 0f;
        holdTimer = 0f;
        swayTime = 0f;
        // 판마다 흔들림 시작 위치가 다르다
        swayPhase = Random.value * Mathf.PI * 2f;
        basketX = PointerTargetX();
        shownSpillPieces = 0;
        for (int i = 0; i < spillPieces.Length; i++)
        {
            spillPieces[i].gameObject.SetActive(false);
        }
        hud.Configure(targetMin, targetMax, overflowLimit, capacityGrams);
        MoveSpout();
        MoveBasket(0f, true);
        HideStream();
        ShowShake(false);
        UpdateDisplay();
    }

    private void Update()
    {
        if (!isPlaying) return;

        // 일시정지 중에는 입력도 시간도 흘리지 않는다
        if (Time.timeScale <= PausedTimeScale) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        swayTime += dt;
        MoveSpout();
        MoveBasket(dt, false);

        bool held = ClickHeld();
        float flowDt = held ? dt : Mathf.Min(dt, tailLeft);
        tailLeft = held ? tailSeconds : Mathf.Max(0f, tailLeft - dt);
        bool flowing = held || flowDt > 0f;

        bool caught = Mathf.Abs(basket.position.x - spout.position.x) <= catchHalfWidth;
        if (flowDt > 0f)
        {
            float grams = flowGramsPerSec * flowDt;
            if (caught) fillGrams += grams;
            else AddSpill(grams);
        }

        UpdateStream(flowing, caught);
        ShowShake(flowing);
        UpdateDisplay();

        float ratio = fillGrams / capacityGrams;
        if (ratio > overflowLimit)
        {
            EndGame(false, TextOverflow);
            return;
        }

        // 모터가 완전히 멈춘 뒤(클릭 뗌 + 관성 끝) 구간 안에 머물러야 한다
        bool settled = !held && tailLeft <= 0f;
        bool inTarget = ratio >= targetMin - Epsilon && ratio <= targetMax + Epsilon;
        // 구간 안에 멈춰 선 순간 한 번 틱
        if (settled && inTarget && holdTimer <= 0f) MinigameSfx.Play("sfx_tick");
        if (settled && inTarget) holdTimer += dt;
        else holdTimer = 0f;
        hud.SetHold(holdTimer / holdSeconds);
        if (holdTimer >= holdSeconds)
        {
            quality = QualityOfRun(ratio);
            EndGame(true, TextSuccess);
        }
    }

    public override void TimeOver()
    {
        float ratio = fillGrams / capacityGrams;
        if (ratio < targetMin) EndGame(false, TextShort);
        else if (ratio > targetMax) EndGame(false, TextTooMuch);
        else EndGame(false, TextTimeOver);
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        HideStream();
        ShowShake(false);
        Finish(success, text);
    }

    // 채움 한가운데 근접도 + 흘림 점수
    private float QualityOfRun(float ratio)
    {
        float fillScore = QualityOf(ratio, targetMin, targetMax);
        float total = fillGrams + spilledGrams;
        float spillRatio = total > 0f ? spilledGrams / total : 0f;
        float spillScore = Mathf.Clamp01(1f - spillRatio * spillPenalty);
        return Mathf.Clamp01(fillQualityWeight * fillScore + (1f - fillQualityWeight) * spillScore);
    }

    // 토출구: 사인으로 좌우 흔들림
    private void MoveSpout()
    {
        float angle = swayTime / Mathf.Max(0.01f, swayPeriod) * Mathf.PI * 2f + swayPhase;
        Vector3 p = spout.localPosition;
        p.x = spoutRestX + Mathf.Sin(angle) * swayAmplitude;
        spout.localPosition = p;
    }

    // 화면 X(가운데 기준 -1~1)를 바스켓 로컬 X 목표로 바꾼다
    private float PointerTargetX()
    {
        float edge = (PointerPosition().x / Mathf.Max(1f, Screen.width) - ScreenCenterRatio) / ScreenCenterRatio;
        return basketRestX + Mathf.Clamp(edge, -1f, 1f) * basketRange;
    }

    // 지수 보간으로 마우스를 따라간다(프레임레이트 무관). snap이면 바로 붙는다
    private void MoveBasket(float dt, bool snap)
    {
        float target = PointerTargetX();
        float t = snap ? 1f : 1f - Mathf.Exp(-followSharpness * dt);
        basketX = Mathf.Lerp(basketX, target, t);
        Vector3 p = basket.localPosition;
        p.x = basketX;
        basket.localPosition = p;
    }

    private void AddSpill(float grams)
    {
        spilledGrams += grams;
        int want = Mathf.Min(spillPieces.Length, Mathf.FloorToInt(spilledGrams / spillGramsPerPiece));
        while (shownSpillPieces < want)
        {
            Transform piece = spillPieces[shownSpillPieces];
            Vector3 pos = piece.position;
            pos.x = spout.position.x + (Random.value - 0.5f) * 2f * spillScatter;
            if (tableLevel != null) pos.y = tableLevel.position.y;
            piece.position = pos;
            piece.gameObject.SetActive(true);
            shownSpillPieces++;
        }
    }

    // 줄기: 토출구에서 바스켓 가루 윗면(받을 때) 또는 책상(놓칠 때)까지
    private void UpdateStream(bool flowing, bool caught)
    {
        if (!flowing)
        {
            HideStream();
            return;
        }
        float endY = tableLevel != null ? tableLevel.position.y : spout.position.y;
        if (caught) endY = powderPivot.position.y + powderPivot.localScale.y;
        float length = spout.position.y - endY;
        if (length <= MinStreamLength)
        {
            HideStream();
            return;
        }
        stream.gameObject.SetActive(true);
        stream.SetPositionAndRotation(spout.position, Quaternion.identity);
        stream.localScale = new Vector3(streamWidth, length, streamWidth);
    }

    private void HideStream()
    {
        stream.gameObject.SetActive(false);
        stream.localScale = streamRestScale;
    }

    private void ShowShake(bool on)
    {
        if (shakeTarget == null) return;
        float offset = on ? Mathf.Sin(swayTime * shakeHz * Mathf.PI * 2f) * shakeAmount : 0f;
        shakeTarget.localPosition = shakeRestLocal + new Vector3(0f, offset, 0f);
    }

    // 가루 높이·색·HUD
    private void UpdateDisplay()
    {
        float ratio = fillGrams / capacityGrams;
        float height = Mathf.Clamp(ratio, 0f, overflowLimit) * powderMaxHeight;
        powderPivot.localScale = new Vector3(powderRestScale.x, height, powderRestScale.z);
        powderPivot.gameObject.SetActive(height > 0f);
        if (powderRenderer != null)
        {
            Color color = ratio >= targetMax ? powderWarnColor : powderNormalColor;
            powderRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            powderRenderer.SetPropertyBlock(block);
        }
        hud.SetState(fillGrams, capacityGrams, ratio >= targetMax, ratio >= targetMin && ratio <= targetMax);
    }
}
