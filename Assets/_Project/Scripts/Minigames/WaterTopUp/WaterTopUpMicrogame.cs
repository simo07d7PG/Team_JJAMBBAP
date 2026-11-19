using UnityEngine;
using UnityEngine.InputSystem;

// "물 타라!" — 마우스 휠로 보온 포트 밸브를 열어 컵에 물을 채우고, 목표 구간에서 거의 멈춘 채 잠깐 머물면 성공.
// 판정은 "이번에 부은 물"(cup.water 증가분) 기준이라 앞 단계 내용물과 무관하다.
public class WaterTopUpMicrogame : Minigame
{
    [Header("참조")]
    [Tooltip("UI/ScrollWheel (Vector2, PassThrough)")]
    [SerializeField] private InputActionReference scrollAction;
    [SerializeField] private CupVisual cupVisual;
    [SerializeField] private WaterTopUpHud hud;
    [Tooltip("물줄기 피벗. localScale.y = 길이, 위쪽이 원점")]
    [SerializeField] private Transform stream;
    [Tooltip("물줄기 시작점(포트 주둥이)")]
    [SerializeField] private Transform spout;
    [Tooltip("물줄기 끝(컵 입구 높이). 비워 두면 주둥이 아래 streamLength만큼")]
    [SerializeField] private Transform streamEnd;
    [Tooltip("밸브 손잡이. 열림에 비례해 0~90도 회전")]
    [SerializeField] private Transform valveHandle;

    [Header("목표")]
    [Tooltip("이번에 부을 물 목표 구간(ml)")]
    [SerializeField] private float targetMinMl = 140f;
    [SerializeField] private float targetMaxMl = 200f;
    [Tooltip("주문 변형(물 적게·많이)마다 구간을 옮기는 양(ml). 넘침 한계도 같이 움직인다")]
    [SerializeField] private float variantShiftMl = 40f;
    [Tooltip("구간 최대보다 이만큼 더 부으면 넘침")]
    [SerializeField] private float overflowMarginMl = 60f;
    [SerializeField] private float cupCapacityMl = 300f;
    [Tooltip("얼음 1개가 차지하는 부피(ml). 액체 높이 계산에 더한다")]
    [SerializeField] private float iceDisplacementMl = 8f;

    [Header("손맛")]
    [Tooltip("휠 한 칸에 열리는 양(0~1)")]
    [SerializeField] private float openPerNotch = 0.18f;
    [Tooltip("초당 저절로 닫히는 양")]
    [SerializeField] private float autoCloseRate = 0.35f;
    [Tooltip("우클릭 중 초당 닫히는 양")]
    [SerializeField] private float brakeCloseRate = 2.5f;
    [Tooltip("밸브가 활짝 열렸을 때 초당 나오는 물(ml)")]
    [SerializeField] private float maxFlowMlPerSec = 80f;
    [Tooltip("이 유량(ml/초) 이하면 멈춘 것으로 본다")]
    [SerializeField] private float stillFlowMlPerSec = 5f;
    [Tooltip("구간 안에서 멈춘 채 머물러야 하는 시간(초)")]
    [SerializeField] private float holdSeconds = 0.6f;
    [SerializeField] private float streamWidth = 0.03f;
    [Tooltip("streamEnd가 없을 때 물줄기 길이")]
    [SerializeField] private float streamLength = 0.3f;

    private const float BigWheelNotch = 120f;
    // 한 프레임에 센 휠 칸 수 상한(고해상도 휠 조각 값 대비)
    private const float MaxNotchesPerFrame = 3f;
    // 밸브 손잡이 최대 회전(도)
    private const float HandleMaxAngle = 90f;
    // 물줄기가 보이는 최소 열림
    private const float MinStreamOpen = 0.01f;
    // 열림이 작을 때 물줄기 두께 배율
    private const float MinStreamWidth01 = 0.4f;
    private const float Epsilon = 0.0001f;

    private Vector3 streamRestScale;
    private Quaternion handleRestRot;
    private float openness;
    private float pouredMl;
    private float initialWater;
    private float holdTimer;
    private float sfxTimer;
    private float curMinMl;
    private float curMaxMl;

    private void Awake()
    {
        streamRestScale = stream.localScale;
        if (valveHandle != null) handleRestRot = valveHandle.localRotation;
    }

    protected override void ResetGame()
    {
        scrollAction.action.Enable();
        openness = 0f;
        pouredMl = 0f;
        holdTimer = 0f;
        initialWater = cup.water;
        curMinMl = Mathf.Max(0f, targetMinMl + variant * variantShiftMl);
        curMaxMl = Mathf.Max(curMinMl + 1f, targetMaxMl + variant * variantShiftMl);
        hud.Configure(curMinMl, curMaxMl, curMaxMl + overflowMarginMl, cupCapacityMl);
        cupVisual.SetIceVisible(cup.ice > 0);
        HideStream();
        ShowHandle();
        UpdateDisplay();
    }

    private void Update()
    {
        if (!isPlaying) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        openness = Mathf.Clamp01(openness + WheelNotches() * openPerNotch);
        float close = RightHeld() ? brakeCloseRate : autoCloseRate;
        openness = Mathf.Max(0f, openness - close * dt);

        float flow = openness * maxFlowMlPerSec;
        pouredMl += flow * dt;
        if (flow > Epsilon)
        {
            sfxTimer -= dt;
            if (sfxTimer <= 0f)
            {
                sfxTimer = 0.35f;
                MinigameSfx.Play("sfx_pour", 0.35f);
            }
        }
        UpdateStream();
        ShowHandle();
        UpdateDisplay();

        if (pouredMl > curMaxMl + overflowMarginMl)
        {
            EndGame(false, "물이 넘쳤어!\n<size=50%>" + Mathf.RoundToInt(curMaxMl + overflowMarginMl) + "ml를 넘었어요</size>");
            return;
        }

        bool inTarget = pouredMl >= curMinMl - Epsilon && pouredMl <= curMaxMl + Epsilon;
        if (inTarget && flow <= stillFlowMlPerSec) holdTimer += dt;
        else holdTimer = 0f;
        hud.SetHold(holdTimer / holdSeconds);
        if (holdTimer >= holdSeconds) EndGame(true, "성공!");
    }

    // 이번 프레임에 위로 굴린 휠 칸 수. 환경마다 ±1 또는 ±120이라 크기를 칸으로 정규화한다. 아래로 굴림은 무시
    private float WheelNotches()
    {
        float y = scrollAction.action.ReadValue<Vector2>().y;
        if (y <= 0f) return 0f;
        float notches = Mathf.Round(y / BigWheelNotch);
        return Mathf.Clamp(notches, 1f, MaxNotchesPerFrame);
    }

    public override void TimeOver()
    {
        string amount = "\n<size=50%>" + Mathf.RoundToInt(pouredMl) + "ml / 목표 " + Mathf.RoundToInt(curMinMl) + "~" + Mathf.RoundToInt(curMaxMl) + "ml</size>";
        if (pouredMl <= Epsilon) EndGame(false, "시간 초과!\n<size=50%>휠을 올려 밸브를 열어요</size>");
        else if (pouredMl < curMinMl) EndGame(false, "물이 모자라!" + amount);
        else if (pouredMl > curMaxMl) EndGame(false, "물이 너무 많아!" + amount);
        else EndGame(false, "시간 초과!\n<size=50%>멈춘 채 " + holdSeconds.ToString("0.0") + "초 유지 필요</size>");
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        HideStream();
        // 표시용으로 올려 둔 값을 되돌리고, 성공했을 때만 부은 양을 컵에 남긴다
        cup.water = initialWater;
        if (success)
        {
            cup.water += pouredMl;
            quality = QualityOf(pouredMl, curMinMl, curMaxMl);
        }
        Finish(success, text);
    }

    // 컵 높이·색·HUD. 화면에는 앞 단계 내용물까지 전부 그린다
    private void UpdateDisplay()
    {
        cup.water = initialWater + pouredMl;
        float fill = Mathf.Clamp01((cup.TotalLiquid() + cup.ice * iceDisplacementMl) / cupCapacityMl);
        cupVisual.SetFill(fill);
        cupVisual.SetColorFrom(cup, 0f, 0f);
        hud.SetState(pouredMl, openness);
    }

    private void UpdateStream()
    {
        if (openness < MinStreamOpen)
        {
            HideStream();
            return;
        }
        float length = streamLength;
        if (streamEnd != null) length = Mathf.Max(0f, spout.position.y - streamEnd.position.y);
        if (length <= 0f)
        {
            HideStream();
            return;
        }
        stream.gameObject.SetActive(true);
        stream.SetPositionAndRotation(spout.position, Quaternion.identity);
        float width = streamWidth * Mathf.Lerp(MinStreamWidth01, 1f, openness);
        stream.localScale = new Vector3(width, length, width);
    }

    private void HideStream()
    {
        stream.gameObject.SetActive(false);
        stream.localScale = streamRestScale;
    }

    private void ShowHandle()
    {
        if (valveHandle == null) return;
        valveHandle.localRotation = handleRestRot * Quaternion.Euler(0f, 0f, openness * HandleMaxAngle);
    }
}
