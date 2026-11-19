using UnityEngine;

// "꾹 눌러라!" — 좌클릭을 누르는 동안 압력이 오르고 떼면 빠진다. 마우스 좌우로 저절로 쏠리는 탬퍼 기울기를 바로잡는다.
// 압력이 목표 띠 안이고 기울기가 허용 안인 시간을 누적으로 채우면 성공. 압력이 100%를 넘으면 즉시 실패.
public class TampMicrogame : Minigame
{
    [Header("참조")]
    [Tooltip("탬퍼 피벗(구 + 막대 + 원판 묶음). 압력만큼 내려가고 기울기만큼 Z로 회전한다")]
    [SerializeField] private Transform tamperPivot;
    [Tooltip("탬퍼 원판 렌더러. 압력 띠 안이면 초록, 기울기가 허용 밖이면 붉게")]
    [SerializeField] private Renderer tamperDisk;
    [Tooltip("바스켓 안 커피 가루(낮은 원기둥). 눌릴수록 낮아진다")]
    [SerializeField] private Transform powder;
    [Tooltip("100% 초과 때 튀는 가루 알갱이들(기본 꺼 둔다)")]
    [SerializeField] private Transform[] burstPieces;
    [SerializeField] private TampHud hud;

    [Header("압력")]
    [Tooltip("누르는 동안 초당 오르는 압력 (1 = 100%)")]
    [SerializeField] private float pressureRise = 0.45f;
    [Tooltip("떼면 초당 빠지는 압력")]
    [SerializeField] private float pressureFall = 0.7f;
    [Tooltip("목표 띠 (1 = 100%)")]
    [SerializeField] private float pressureMin = 0.6f;
    [SerializeField] private float pressureMax = 0.8f;
    [Tooltip("이 값을 넘으면 즉시 실패")]
    [SerializeField] private float pressureLimit = 1f;

    [Header("기울기")]
    [Tooltip("저절로 쏠리는 최대 각도(도). 노이즈를 늘려 쓰고 이 값에서 자른다")]
    [SerializeField] private float driftMax = 9f;
    [Tooltip("쏠림 노이즈 속도 (클수록 빨리 바뀐다)")]
    [SerializeField] private float driftSpeed = 0.9f;
    [Tooltip("마우스가 화면 가장자리(가운데에서 폭의 절반)에 있을 때 보정 각도(도). 해상도와 무관")]
    [SerializeField] private float tiltAtEdge = 11.5f;
    [Tooltip("이 각도 이내면 수평으로 본다(도)")]
    [SerializeField] private float levelTolerance = 3f;

    [Header("판정")]
    [Tooltip("띠 안 + 수평인 시간을 이만큼 채우면 성공(초, 연속 아니어도 됨)")]
    [SerializeField] private float holdNeeded = 1.4f;
    [Tooltip("조건에서 벗어나도 이 시간까지는 \"다지는 중\" 표시를 유지한다(초). 누적은 쌓이지 않는다")]
    [SerializeField] private float graceSeconds = 0.15f;
    [Tooltip("시간 초과 때 누적이 이 비율 이상이면 \"덜 다졌어!\"")]
    [SerializeField] private float almostRatio = 0.5f;
    [Tooltip("등급에서 압력 근접도가 차지하는 비율 (나머지는 수평 정도)")]
    [SerializeField] private float pressureQualityWeight = 0.5f;

    [Header("연출")]
    [Tooltip("탬퍼가 내려가는 최대 깊이(m, 압력 100%)")]
    [SerializeField] private float tamperDrop = 0.025f;
    [Tooltip("압력 100%일 때 가루 높이 배율 (1 = 안 낮아짐)")]
    [SerializeField] private float powderMinHeightRatio = 0.3f;
    [Tooltip("원판 색: 기본 / 압력 띠 안 / 기울기 허용 밖")]
    [SerializeField] private Color diskNormalColor = new Color(0.75f, 0.75f, 0.78f);
    [SerializeField] private Color diskGoodColor = new Color(0.3f, 0.75f, 0.3f);
    [SerializeField] private Color diskTiltedColor = new Color(0.85f, 0.2f, 0.15f);
    [Tooltip("가루 터짐: 튀는 속도(m/초), 지속(초), 중력(m/초²)")]
    [SerializeField] private float burstSpeed = 0.35f;
    [SerializeField] private float burstSeconds = 0.6f;
    [SerializeField] private float burstGravity = 1.2f;

    // 일시정지 판정: timeScale이 이 값 이하면 입력을 받지 않는다
    private const float PausedTimeScale = 0f;
    private const float PercentScale = 100f;
    private const float ScreenCenterRatio = 0.5f;
    private const float NoiseCenter = 0.5f;
    private const float NoiseToUnit = 2f;
    // PerlinNoise가 0.2~0.8에 몰려서 늘려 쓴다
    private const float DriftNoiseGain = 1.6f;
    private const float NoiseSeedRange = 100f;

    // 결과 문구
    private const string TextSuccess = "꾹꾹 다졌어!";
    private const string TextTooHard = "너무 세게 눌렀어!";
    private const string TextAlmost = "덜 다졌어!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private float pressure;
    private float tilt;
    private float elapsed;
    private float noiseSeed;
    private float hold;
    private float graceTimer;
    private float pressureScoreSum;
    private float levelScoreSum;
    private bool wasInBand;

    private MaterialPropertyBlock block;
    private Vector3 pivotRestLocal;
    private Vector3 powderRestScale;
    private Vector3[] burstRestLocal;
    private Vector3[] burstVelocity;
    private float burstElapsed;
    private bool bursting;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        pivotRestLocal = tamperPivot.localPosition;
        powderRestScale = powder.localScale;
        burstRestLocal = new Vector3[burstPieces.Length];
        burstVelocity = new Vector3[burstPieces.Length];
        for (int i = 0; i < burstPieces.Length; i++)
        {
            burstRestLocal[i] = burstPieces[i].localPosition;
        }
    }

    protected override void ResetGame()
    {
        pressure = 0f;
        tilt = 0f;
        elapsed = 0f;
        hold = 0f;
        graceTimer = graceSeconds;
        pressureScoreSum = 0f;
        levelScoreSum = 0f;
        wasInBand = false;
        // 판마다 쏠리는 모양이 다르다
        noiseSeed = Random.value * NoiseSeedRange;
        StopBurst();

        hud.Configure(pressureMin, pressureMax, pressureLimit, driftMax, levelTolerance, holdNeeded);
        UpdateTilt();
        ApplyVisuals(false);
    }

    private void Update()
    {
        UpdateBurst();
        if (!isPlaying) return;

        // 일시정지 중에는 입력도 시간도 흘리지 않는다
        if (Time.timeScale <= PausedTimeScale) return;

        float dt = Time.deltaTime;
        elapsed += dt;

        if (ClickHeld()) pressure += pressureRise * dt;
        else pressure = Mathf.Max(0f, pressure - pressureFall * dt);

        if (pressure > pressureLimit)
        {
            pressure = pressureLimit;
            ApplyVisuals(false);
            StartBurst();
            Finish(false, TextTooHard + "\n<size=50%>압력이 " + Mathf.RoundToInt(pressureLimit * PercentScale) + "%를 넘었어요</size>");
            return;
        }

        UpdateTilt();

        bool inBand = pressure >= pressureMin && pressure <= pressureMax;
        if (inBand && !wasInBand) MinigameSfx.Play("sfx_thud");
        wasInBand = inBand;
        bool level = Mathf.Abs(tilt) <= levelTolerance;
        bool tamping = inBand && level;
        if (tamping)
        {
            hold += dt;
            graceTimer = 0f;
            pressureScoreSum += QualityOf(pressure, pressureMin, pressureMax) * dt;
            levelScoreSum += Mathf.Clamp01(1f - Mathf.Abs(tilt) / levelTolerance) * dt;
        }
        else
        {
            graceTimer += dt;
        }

        bool showTamping = tamping || graceTimer <= graceSeconds;
        ApplyVisuals(showTamping);

        if (hold >= holdNeeded)
        {
            quality = QualityOfRun();
            MinigameSfx.Play("sfx_thud");
            Finish(true, TextSuccess);
        }
    }

    // 기울기 = 저절로 쏠리는 값 + 마우스 보정 (화면 가운데에서 벗어난 만큼)
    private void UpdateTilt()
    {
        float noise = (Mathf.PerlinNoise(noiseSeed, elapsed * driftSpeed) - NoiseCenter) * NoiseToUnit;
        float drift = Mathf.Clamp(noise * DriftNoiseGain, -1f, 1f) * driftMax;
        float edge = (PointerPosition().x / Mathf.Max(1f, Screen.width) - ScreenCenterRatio) / ScreenCenterRatio;
        tilt = drift + Mathf.Clamp(edge, -1f, 1f) * tiltAtEdge;
    }

    // 누적 구간 평균: 압력 한가운데 근접도와 수평 정도
    private float QualityOfRun()
    {
        if (hold <= 0f) return 0f;
        float pressureScore = pressureScoreSum / hold;
        float levelScore = levelScoreSum / hold;
        return Mathf.Clamp01(pressureQualityWeight * pressureScore + (1f - pressureQualityWeight) * levelScore);
    }

    public override void TimeOver()
    {
        string progress = "\n<size=50%>" + hold.ToString("0.0") + " / " + holdNeeded.ToString("0.0") + "초 다짐</size>";
        if (hold >= holdNeeded * almostRatio) Finish(false, TextAlmost + progress);
        else Finish(false, TextTimeOver + progress);
    }

    private void ApplyVisuals(bool tamping)
    {
        float p01 = Mathf.Clamp01(pressure / pressureLimit);
        tamperPivot.localPosition = pivotRestLocal + Vector3.down * tamperDrop * p01;
        tamperPivot.localRotation = Quaternion.Euler(0f, 0f, tilt);

        Vector3 s = powderRestScale;
        s.y = powderRestScale.y * Mathf.Lerp(1f, powderMinHeightRatio, p01);
        powder.localScale = s;

        Color c = diskNormalColor;
        if (Mathf.Abs(tilt) > levelTolerance) c = diskTiltedColor;
        else if (tamping) c = diskGoodColor;
        tamperDisk.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        tamperDisk.SetPropertyBlock(block);

        hud.SetPressure(pressure * PercentScale, tamping);
        hud.SetTilt(tilt, Mathf.Abs(tilt) <= levelTolerance);
        hud.SetProgress(hold);
    }

    // 가루 터짐: 알갱이를 켜고 바깥·위로 튕긴다
    private void StartBurst()
    {
        bursting = true;
        burstElapsed = 0f;
        for (int i = 0; i < burstPieces.Length; i++)
        {
            burstPieces[i].localPosition = burstRestLocal[i];
            burstPieces[i].gameObject.SetActive(true);
            float angle = (i + Random.value) / Mathf.Max(1, burstPieces.Length) * Mathf.PI * 2f;
            burstVelocity[i] = new Vector3(Mathf.Cos(angle), 1f + Random.value, Mathf.Sin(angle)) * burstSpeed;
        }
    }

    private void StopBurst()
    {
        bursting = false;
        for (int i = 0; i < burstPieces.Length; i++)
        {
            burstPieces[i].localPosition = burstRestLocal[i];
            burstPieces[i].gameObject.SetActive(false);
        }
    }

    // 끝난 뒤에도 터짐은 마저 보여 준다. 일시정지 중에는 멈춘다
    private void UpdateBurst()
    {
        if (!bursting || Time.timeScale <= PausedTimeScale) return;
        float dt = Time.deltaTime;
        burstElapsed += dt;
        for (int i = 0; i < burstPieces.Length; i++)
        {
            burstVelocity[i] += Vector3.down * burstGravity * dt;
            burstPieces[i].localPosition += burstVelocity[i] * dt;
        }
        if (burstElapsed >= burstSeconds) bursting = false;
    }
}
