using UnityEngine;
using UnityEngine.InputSystem;

// "타이밍 맞춰 갈아라!" — 링 위를 시계 방향으로 도는 바늘이 밝은 구간에 있을 때 Space.
// 4번 맞추면 성공, 3번 놓치면 실패. 맞출 때마다 바늘이 빨라진다. 각도는 12시 = 0도, 시계 방향이 +
public class GrindTimingMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private GrindTimingHud hud;
    [Tooltip("그라인더 본체. 판정 때 흔들린다")]
    [SerializeField] private Transform grinderBody;
    [Tooltip("가루 더미(원점이 바닥). localScale.y = 성공 횟수 비율")]
    [SerializeField] private Transform powderPile;
    [Tooltip("맞추기 입력 (InputSystem_Actions > UI/Space)")]
    [SerializeField] private InputActionReference spaceAction;

    [Header("구간 (도)")]
    [Tooltip("좋음 구간 폭")]
    [SerializeField] private float goodArcDegrees = 60f;
    [Tooltip("좋음 구간 한가운데 완벽 구간 폭")]
    [SerializeField] private float perfectArcDegrees = 20f;
    [Tooltip("판정 여유. 좋음 구간 양끝에 더해지고, 화면 호도 이 폭까지 그린다")]
    [SerializeField] private float forgiveDegrees = 4f;
    [Tooltip("판정 구간 시작 기준 최소 각도(12시 기준). 바늘이 최소 이만큼 돈 뒤에 판정 구간에 닿는다")]
    [SerializeField] private float minCenterDegrees = 110f;
    [Tooltip("구간 한가운데 기준 최대 각도")]
    [SerializeField] private float maxCenterDegrees = 300f;

    [Header("속도·진행")]
    [Tooltip("바늘 속도(도/초). 300 = 한 바퀴 1.2초")]
    [SerializeField] private float startSpeed = 300f;
    [Tooltip("맞출 때마다 빨라지는 비율 (0.1 = 10%)")]
    [SerializeField] private float speedUpRatio = 0.1f;
    [Tooltip("시작 직후 바늘이 멈춰 있는 시간(초)")]
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("판정 뒤 쉬는 시간(초)")]
    [SerializeField] private float betweenSeconds = 0.4f;
    [Tooltip("성공에 필요한 맞춤 수")]
    [SerializeField] private int neededHits = 4;
    [Tooltip("이만큼 놓치면 실패")]
    [SerializeField] private int maxMisses = 3;
    [Tooltip("시간 초과 때 이만큼 맞췄으면 \"덜 갈렸어!\"")]
    [SerializeField] private int halfHits = 2;

    [Header("등급")]
    [SerializeField] private float goodScore = 0.5f;
    [SerializeField] private float perfectScore = 1f;
    [Tooltip("놓칠 때마다 quality에서 빼는 값")]
    [SerializeField] private float missPenalty = 0.15f;

    [Header("연출")]
    [Tooltip("맞췄을 때 / 놓쳤을 때 본체 흔들림 크기(월드 단위)와 시간(초)")]
    [SerializeField] private float hitShakeAmount = 0.01f;
    [SerializeField] private float missShakeAmount = 0.03f;
    [SerializeField] private float shakeSeconds = 0.2f;

    private const string TextSuccess = "성공!";
    private const string TextTooManyMisses = "너무 놓쳤어!";
    private const string TextNotEnough = "덜 갈렸어!";
    private const string JudgePerfect = "완벽!";
    private const string JudgeGood = "좋아!";
    private const string JudgeMiss = "삑!";

    // 구간 폭이 0이 되지 않게 하는 하한(도)
    private const float MinArcDegrees = 1f;

    private Vector3 bodyRestPos;
    private Vector3 pileRestScale;

    private float needleAngle;
    private float speed;
    private float startTimer;
    private float restTimer;
    private bool resting;
    private float hitStart;
    private float hitEnd;
    private float perfectStart;
    private float perfectEnd;
    private int hits;
    private int misses;
    private float score;

    private float shakeTimer;
    private float shakeAmountNow;

    private void Awake()
    {
        bodyRestPos = grinderBody.localPosition;
        pileRestScale = powderPile.localScale;
    }

    protected override void ResetGame()
    {
        spaceAction.action.Enable();
        speed = startSpeed;
        startTimer = startDelay;
        restTimer = 0f;
        resting = false;
        hits = 0;
        misses = 0;
        score = 0f;
        shakeTimer = 0f;

        grinderBody.localPosition = bodyRestPos;
        UpdatePile();
        hud.Configure(neededHits, maxMisses);
        NewZone();
        hud.SetHits(hits, neededHits);
        hud.SetMisses(misses);
    }

    // 새 구간을 정하고 바늘을 12시로 되돌린다. HUD 호는 판정 범위와 같은 값으로 그린다
    private void NewZone()
    {
        float good = Mathf.Max(MinArcDegrees, goodArcDegrees);
        float perfect = Mathf.Clamp(perfectArcDegrees, MinArcDegrees, good);
        float minCenter = minCenterDegrees + good * 0.5f + forgiveDegrees;
        float center = Random.Range(minCenter, Mathf.Max(minCenter, maxCenterDegrees));

        hitStart = center - good * 0.5f - forgiveDegrees;
        hitEnd = center + good * 0.5f + forgiveDegrees;
        perfectStart = center - perfect * 0.5f;
        perfectEnd = center + perfect * 0.5f;

        needleAngle = 0f;
        resting = false;
        hud.ShowZone(hitStart, hitEnd - hitStart, perfectStart, perfectEnd - perfectStart);
        hud.SetNeedle(needleAngle);
    }

    private void Update()
    {
        UpdateShake();
        if (!isPlaying) return;

        float dt = Time.deltaTime;
        if (startTimer > 0f)
        {
            startTimer -= dt;
            return;
        }

        if (resting)
        {
            restTimer -= dt;
            if (restTimer <= 0f) NewZone();
            return;
        }

        // 입력은 화면에 보이던 각도(이번 프레임 이동 전)로 판정한다. 일시정지(timeScale 0) 중에는 무시
        if (Time.timeScale > 0f && spaceAction.action.WasPressedThisFrame())
        {
            Judge();
            return;
        }

        needleAngle += speed * dt;
        hud.SetNeedle(needleAngle);
        // 구간 끝을 지나치면 놓침
        if (needleAngle > hitEnd) Miss();
    }

    private void Judge()
    {
        if (needleAngle < hitStart || needleAngle > hitEnd)
        {
            Miss();
            return;
        }

        bool perfect = needleAngle >= perfectStart && needleAngle <= perfectEnd;
        hits++;
        MinigameSfx.Play("sfx_tick");
        score += perfect ? perfectScore : goodScore;
        speed *= 1f + speedUpRatio;
        hud.ShowJudge(perfect ? JudgePerfect : JudgeGood, true);
        hud.SetHits(hits, neededHits);
        UpdatePile();
        Shake(hitShakeAmount);

        if (hits >= neededHits)
        {
            quality = Mathf.Clamp01(score / neededHits - missPenalty * misses);
            Finish(true, TextSuccess);
            return;
        }
        StartRest();
    }

    private void Miss()
    {
        misses++;
        hud.ShowJudge(JudgeMiss, false);
        hud.SetMisses(misses);
        Shake(missShakeAmount);

        if (misses >= maxMisses)
        {
            Finish(false, TextTooManyMisses + "\n<size=50%>" + hits + " / " + neededHits + "번 성공</size>");
            return;
        }
        StartRest();
    }

    private void StartRest()
    {
        resting = true;
        restTimer = betweenSeconds;
    }

    public override void TimeOver()
    {
        string progress = "\n<size=50%>" + hits + " / " + neededHits + "번 성공</size>";
        if (hits >= halfHits) Finish(false, TextNotEnough + progress);
        else Finish(false, "시간 초과!" + progress);
    }

    private void UpdatePile()
    {
        float ratio = neededHits > 0 ? Mathf.Clamp01((float)hits / neededHits) : 0f;
        powderPile.gameObject.SetActive(ratio > 0f);
        powderPile.localScale = new Vector3(pileRestScale.x, pileRestScale.y * ratio, pileRestScale.z);
    }

    private void Shake(float amount)
    {
        shakeAmountNow = amount;
        shakeTimer = shakeSeconds;
    }

    // 흔들림이 끝나면 본체가 제자리로 돌아온다
    private void UpdateShake()
    {
        if (shakeTimer <= 0f) return;
        shakeTimer -= Time.deltaTime;
        if (shakeTimer <= 0f)
        {
            grinderBody.localPosition = bodyRestPos;
            return;
        }
        grinderBody.localPosition = bodyRestPos + Random.insideUnitSphere * shakeAmountNow;
    }
}
