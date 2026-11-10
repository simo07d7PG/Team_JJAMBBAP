using UnityEngine;
using UnityEngine.InputSystem;

// "물 부어라!" — 색 띠 위를 왕복하는 표시를 Space로 멈추면, 멈춘 자리 색만큼 추출 게이지가 오른다.
// 기회 5번 안에 게이지 100을 넘기면 성공. 멈출 때마다 표시가 빨라진다
public class DripPourMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private DripPourHud hud;
    [Tooltip("주전자 피벗. 붓는 동안 이 축으로 기운다")]
    [SerializeField] private Transform kettle;
    [Tooltip("물줄기. 붓는 동안만 켠다 (크기는 프리팹에서 맞춘다)")]
    [SerializeField] private Transform stream;
    [Tooltip("서버 안 커피 피벗(원점이 바닥). localScale.y = 게이지 비율")]
    [SerializeField] private Transform serverCoffee;
    [Tooltip("멈추기 입력 (InputSystem_Actions > UI/Space)")]
    [SerializeField] private InputActionReference spaceAction;

    [Header("색 띠 폭 (전체 1 기준, 가운데 대칭. 빨강은 양끝 나머지)")]
    [Tooltip("가운데 초록 폭")]
    [SerializeField] private float greenWidth = 0.12f;
    [Tooltip("초록 양옆 노랑 한쪽 폭")]
    [SerializeField] private float yellowWidth = 0.10f;
    [Tooltip("노랑 양옆 주황 한쪽 폭")]
    [SerializeField] private float orangeWidth = 0.14f;

    [Header("색별 게이지 상승량")]
    [SerializeField] private float greenGain = 25f;
    [SerializeField] private float yellowGain = 20f;
    [SerializeField] private float orangeGain = 15f;
    [SerializeField] private float redGain = 10f;

    [Header("수치")]
    [Tooltip("표시가 끝에서 끝까지 가는 시간(초), 첫 왕복 기준")]
    [SerializeField] private float sweepSeconds = 0.9f;
    [Tooltip("멈출 때마다 빨라지는 비율 (0.08 = 8%)")]
    [SerializeField] private float speedUpRatio = 0.08f;
    [Tooltip("멈춘 뒤 물 붓는 시간(초). 이 동안 Space는 무시")]
    [SerializeField] private float pourSeconds = 0.5f;
    [Tooltip("멈출 수 있는 기회 수")]
    [SerializeField] private int pourCount = 5;
    [Tooltip("이 게이지 이상이면 성공")]
    [SerializeField] private float targetGauge = 100f;
    [Tooltip("목표를 이만큼 넘기면 등급 1 (완벽)")]
    [SerializeField] private float perfectMargin = 25f;

    [Header("연출")]
    [Tooltip("붓는 동안 주전자가 기우는 각도와 축(주전자 로컬)")]
    [SerializeField] private float kettleTiltDegrees = 35f;
    [SerializeField] private Vector3 kettleTiltAxis = Vector3.forward;
    [Tooltip("붓기 시작·끝에서 주전자가 기울고 돌아오는 시간 비율(0~0.5)")]
    [SerializeField] private float kettleTiltEaseRatio = 0.25f;

    // 색 종류. 게이지 상승량과 HUD 색을 고를 때 쓴다
    public const int KindRed = 0;
    public const int KindOrange = 1;
    public const int KindYellow = 2;
    public const int KindGreen = 3;

    // 띠 순서(왼쪽부터): 빨 주 노 초 노 주 빨
    private const int ZoneCount = 7;
    private static readonly int[] ZoneKinds = { KindRed, KindOrange, KindYellow, KindGreen, KindYellow, KindOrange, KindRed };

    // 서버 커피가 0일 때 숨기는 기준
    private const float EmptyEpsilon = 0.0001f;
    // 0으로 나누지 않게 하는 하한: 기울기 시간 비율, 끝→끝 시간(초)
    private const float MinEaseRatio = 0.01f;
    private const float MinSweepSeconds = 0.05f;

    private const string TextSuccess = "성공!";
    private const string TextNotEnough = "덜 내렸어!";
    private const string TextTimeOver = "시간 초과!";

    // 띠 경계(0~1), ZoneCount + 1개. 판정과 HUD가 같은 값을 쓴다
    private float[] zoneEdges = new float[ZoneCount + 1];

    private Quaternion kettleRestRot;
    private Vector3 coffeeRestScale;
    private float gaugeMax;

    private float phase;
    private float markerPos;
    private float curSweepSeconds;
    private int poursUsed;
    private float gauge;

    // 붓는 중 상태
    private bool pouring;
    private float pourTimer;
    private float pourStartGauge;
    private float pourGain;

    private void Awake()
    {
        kettleRestRot = kettle.localRotation;
        coffeeRestScale = serverCoffee.localScale;
    }

    protected override void ResetGame()
    {
        spaceAction.action.Enable();
        BuildZones();

        // 게이지 막대 끝 = 모두 초록일 때 값 (목표보다 작으면 목표)
        gaugeMax = Mathf.Max(targetGauge, pourCount * greenGain);

        phase = 0f;
        markerPos = 0f;
        curSweepSeconds = sweepSeconds;
        poursUsed = 0;
        gauge = 0f;
        pouring = false;
        pourTimer = 0f;

        SetPourVisual(false, 0f);
        UpdateCoffee();
        hud.Configure(zoneEdges, ZoneKinds, pourCount, targetGauge, gaugeMax);
        hud.SetMarker(markerPos);
        hud.SetGauge(gauge, gaugeMax);
        hud.SetPoursLeft(pourCount);
    }

    // 폭 필드에서 띠 경계를 만든다. 폭 합이 1을 넘으면 빨강을 0으로 두고 비율을 맞춘다
    private void BuildZones()
    {
        float g = Mathf.Max(0f, greenWidth);
        float y = Mathf.Max(0f, yellowWidth);
        float o = Mathf.Max(0f, orangeWidth);
        float r = Mathf.Max(0f, (1f - g - 2f * y - 2f * o) * 0.5f);
        float[] widths = { r, o, y, g, y, o, r };
        float total = g + 2f * y + 2f * o + 2f * r;

        zoneEdges[0] = 0f;
        for (int i = 0; i < ZoneCount; i++)
        {
            zoneEdges[i + 1] = zoneEdges[i] + widths[i] / total;
        }
        zoneEdges[ZoneCount] = 1f;
    }

    private void Update()
    {
        if (!isPlaying)
        {
            if (pouring) StopPour();
            return;
        }

        float dt = Time.deltaTime;
        if (pouring)
        {
            UpdatePour(dt);
            return;
        }

        MoveMarker(dt);
        // 일시정지(timeScale 0) 중에는 멈추지 않는다
        if (Time.timeScale > 0f && spaceAction.action.WasPressedThisFrame()) StartPour();
    }

    // 끝→끝 시간 기준으로 왕복. 끝에 닿으면 반사된다 (PingPong)
    private void MoveMarker(float dt)
    {
        phase += dt / Mathf.Max(MinSweepSeconds, curSweepSeconds);
        markerPos = Mathf.PingPong(phase, 1f);
        hud.SetMarker(markerPos);
    }

    private void StartPour()
    {
        int zone = ZoneAt(markerPos);
        int kind = ZoneKinds[zone];
        poursUsed++;
        pouring = true;
        pourTimer = 0f;
        pourStartGauge = gauge;
        pourGain = GainOf(kind);

        hud.ShowGain(kind, pourGain, markerPos);
        hud.SetPoursLeft(pourCount - poursUsed);
        SetPourVisual(true, 0f);
    }

    // 붓는 동안 게이지가 고르게 오르고, 끝나면 판정 후 표시가 다시 움직인다
    private void UpdatePour(float dt)
    {
        pourTimer += dt;
        float t = pourSeconds > 0f ? Mathf.Clamp01(pourTimer / pourSeconds) : 1f;
        gauge = pourStartGauge + pourGain * t;
        SetPourVisual(true, t);
        UpdateCoffee();
        hud.SetGauge(gauge, gaugeMax);
        if (t < 1f) return;

        StopPour();
        if (gauge >= targetGauge)
        {
            EndGame(true, TextSuccess);
            return;
        }
        if (poursUsed >= pourCount)
        {
            EndGame(false, TextNotEnough);
            return;
        }
        // 멈출 때마다 빨라진다
        curSweepSeconds = curSweepSeconds / (1f + speedUpRatio);
    }

    private void StopPour()
    {
        pouring = false;
        gauge = pourStartGauge + pourGain;
        SetPourVisual(false, 0f);
        UpdateCoffee();
        hud.SetGauge(gauge, gaugeMax);
    }

    // 붓는 도중 시간이 끝나면 그 붓기는 다 부은 것으로 친다(이미 누른 결과)
    public override void TimeOver()
    {
        if (!isPlaying) return;
        if (pouring) StopPour();
        if (gauge >= targetGauge) EndGame(true, TextSuccess);
        else if (poursUsed >= pourCount) EndGame(false, TextNotEnough);
        else EndGame(false, TextTimeOver);
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        if (success) quality = Mathf.Clamp01((gauge - targetGauge) / perfectMargin);
        Finish(success, text);
    }

    // 위치(0~1)가 속한 띠 번호
    private int ZoneAt(float pos)
    {
        for (int i = 0; i < ZoneCount; i++)
        {
            if (pos < zoneEdges[i + 1]) return i;
        }
        return ZoneCount - 1;
    }

    private float GainOf(int kind)
    {
        switch (kind)
        {
            case KindGreen: return greenGain;
            case KindYellow: return yellowGain;
            case KindOrange: return orangeGain;
            default: return redGain;
        }
    }

    // 주전자 기울기와 물줄기. t = 붓기 진행(0~1)
    private void SetPourVisual(bool on, float t)
    {
        float tilt = 0f;
        if (on)
        {
            // 앞 ease 구간에 기울고, 뒤 ease 구간에 돌아온다
            float ease = Mathf.Max(MinEaseRatio, kettleTiltEaseRatio);
            tilt = Mathf.Clamp01(Mathf.Min(t, 1f - t) / ease);
        }
        kettle.localRotation = kettleRestRot * Quaternion.AngleAxis(kettleTiltDegrees * tilt, kettleTiltAxis);
        stream.gameObject.SetActive(on);
    }

    // 서버 안 커피 높이 = 게이지 / 게이지 막대 끝
    private void UpdateCoffee()
    {
        float ratio = Mathf.Clamp01(gauge / gaugeMax);
        serverCoffee.localScale = new Vector3(coffeeRestScale.x, coffeeRestScale.y * ratio, coffeeRestScale.z);
        serverCoffee.gameObject.SetActive(ratio > EmptyEpsilon);
    }
}
