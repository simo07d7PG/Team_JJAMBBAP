using UnityEngine;

// "샷 내려라!" — 좌클릭을 누르고 있는 동안 추출, 목표 구간에서 떼면 성공
public class EspressoShotMicrogame : Minigame
{
    [Header("참조")]
    [Tooltip("추출구 위치")]
    [SerializeField] private Transform spout;
    [Tooltip("컵 루트 (원점이 컵 바닥)")]
    [SerializeField] private Transform cupModel;
    [SerializeField] private CupVisual cupVisual;
    [SerializeField] private EspressoShotHud hud;
    [Tooltip("추출 줄기 피벗. 추출구에 두고 localScale.y = 길이")]
    [SerializeField] private Transform stream;
    [Tooltip("추출 버튼 (눌리면 살짝 들어간다)")]
    [SerializeField] private Transform extractButton;

    [Header("손맛")]
    [SerializeField] private float buttonPressDepth = 0.02f;
    [Tooltip("줄기 굵기 배율 (추출 속도에 비례)")]
    [SerializeField] private float streamWidthPerFlow = 0.02f;

    [Header("수치")]
    [Tooltip("목표 구간(ml). 이 안에서 떼면 성공")]
    [SerializeField] private float targetMinMl = 25f;
    [SerializeField] private float targetMaxMl = 40f;
    [Tooltip("주문 변형(연하게·진하게)마다 목표 구간을 옮기는 양(ml)")]
    [SerializeField] private float variantShiftMl = 10f;
    [Tooltip("컵 용량(ml). 넘으면 넘침 실패")]
    [SerializeField] private float capacityMl = 60f;
    [Tooltip("평균 추출 속도(ml/초)")]
    [SerializeField] private float flowMlPerSec = 12f;
    [Tooltip("추출 속도 출렁임 비율(0~1)")]
    [SerializeField] private float flowNoise = 0.25f;
    [Tooltip("출렁임 빠르기")]
    [SerializeField] private float noiseFrequency = 2f;

    [Header("추출 가속 (처음엔 졸졸, 목표 하한에 가까울수록 콸콸)")]
    [Tooltip("시작 속도 배율")]
    [SerializeField] private float flowStartScale = 0.5f;
    [Tooltip("목표 하한에 닿았을 때 속도 배율. 그 뒤로는 이 속도 유지")]
    [SerializeField] private float flowPeakScale = 1.9f;

    [Header("거품 치직 (한 판에 한 번 추출이 잠깐 멈춤)")]
    [Tooltip("멈출 확률(0~1)")]
    [SerializeField] private float sputterChance = 1f;
    [Tooltip("멈추는 시점. 목표 하한 대비 비율 범위")]
    [SerializeField] private float sputterFromRatio = 0.35f;
    [SerializeField] private float sputterToRatio = 0.8f;
    [Tooltip("멈춰 있는 시간(초)")]
    [SerializeField] private float sputterSeconds = 0.7f;
    [Tooltip("멈춘 뒤 확 쏟아지는 시간(초)과 배율")]
    [SerializeField] private float burstSeconds = 0.35f;
    [SerializeField] private float burstScale = 1.6f;
    [Tooltip("치직 소리 (없으면 무음)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip sputterClip;

    [Header("연출")]
    [Tooltip("줄기 색: 목표 전(진한 갈색) / 목표 안(황금 크레마) / 넘김(묽은 색)")]
    [SerializeField] private Color streamDarkColor = new Color(0.28f, 0.13f, 0.05f);
    [SerializeField] private Color streamCremaColor = new Color(0.9f, 0.58f, 0.2f);
    [SerializeField] private Color streamBlondeColor = new Color(0.95f, 0.86f, 0.62f);
    [Tooltip("넘침이 가까울 때 컵 떨림 최대 폭")]
    [SerializeField] private float overflowShake = 0.008f;
    [Tooltip("치직할 때 컵 떨림 폭")]
    [SerializeField] private float sputterShake = 0.004f;

    // 경계값을 성공으로 인정하는 여유
    private const float Epsilon = 0.0001f;

    private Vector3 buttonRestLocal;
    private Vector3 streamRestScale;
    private Vector3 cupRestLocal;
    private Renderer streamRenderer;
    private MaterialPropertyBlock streamBlock;
    private bool extracting;
    private float extracted;
    private float noiseTime;
    private float noiseSeed;
    // 이번 주문의 목표 구간 (variant 반영)
    private float curMinMl;
    private float curMaxMl;
    // 거품 치직: 멈출 양, 남은 멈춤 시간, 남은 쏟아짐 시간
    private float sputterAtMl;
    private float sputterTimer;
    private float burstTimer;
    private bool sputterDone;

    private void Awake()
    {
        buttonRestLocal = extractButton.localPosition;
        streamRestScale = stream.localScale;
        cupRestLocal = cupModel.localPosition;
        streamRenderer = stream.GetComponentInChildren<Renderer>(true);
        streamBlock = new MaterialPropertyBlock();
    }

    protected override void ResetGame()
    {
        extracting = false;
        extracted = 0f;
        noiseTime = 0f;
        noiseSeed = Random.value * 100f;

        SetStream(0f, 0f);
        extractButton.localPosition = buttonRestLocal;

        curMinMl = Mathf.Clamp(targetMinMl + variant * variantShiftMl, 5f, capacityMl - 10f);
        curMaxMl = Mathf.Clamp(targetMaxMl + variant * variantShiftMl, curMinMl + 5f, capacityMl - 5f);

        sputterTimer = 0f;
        burstTimer = 0f;
        sputterDone = Random.value >= sputterChance;
        sputterAtMl = curMinMl * Random.Range(sputterFromRatio, sputterToRatio);
        cupModel.localPosition = cupRestLocal;
        SetStreamColor(streamDarkColor);

        hud.Configure(curMinMl, curMaxMl, capacityMl);
        UpdateCupVisual();
        cupVisual.SetIceVisible(cup.ice > 0);
    }

    private void Update()
    {
        if (!isPlaying)
        {
            if (extracting) StopExtraction();
            if (sputterTimer > 0f)
            {
                sputterTimer = 0f;
                hud.HideAlert();
            }
            cupModel.localPosition = cupRestLocal;
            return;
        }

        // 치직 시간은 버튼을 떼도 흐른다
        if (sputterTimer > 0f)
        {
            sputterTimer -= Time.deltaTime;
            if (sputterTimer <= 0f) EndSputter();
        }

        if (!extracting && ClickPressed()) StartExtraction();
        if (!extracting)
        {
            cupModel.localPosition = cupRestLocal;
            return;
        }

        if (ClickHeld())
        {
            Extract(Time.deltaTime);
        }
        else
        {
            StopExtraction();
            if (extracted > 0f) Release();
        }
    }

    private void StartExtraction()
    {
        extracting = true;
        extractButton.localPosition = buttonRestLocal + Vector3.down * buttonPressDepth;
    }

    private void StopExtraction()
    {
        extracting = false;
        SetStream(0f, 0f);
        extractButton.localPosition = buttonRestLocal;
    }

    private void Extract(float dt)
    {
        float length = Mathf.Max(0f, spout.position.y - cupModel.position.y);

        // 거품 치직: 양은 늘지 않고, 굵기·길이가 다른 짧은 물줄기가 툭툭 끊겨 떨어진다(컵까지 못 가기도 함)
        if (sputterTimer > 0f)
        {
            float elapsed = sputterSeconds - sputterTimer;
            float spurt = Mathf.PerlinNoise(noiseSeed + 7f, elapsed * 14f);
            if (spurt > 0.5f)
            {
                float k = (spurt - 0.5f) * 2f;
                SetStream(flowMlPerSec * Mathf.Lerp(0.15f, 0.45f, k), length * Mathf.Lerp(0.3f, 1f, k));
            }
            else
            {
                SetStream(0f, 0f);
            }
            SetStreamColor(Color.Lerp(streamDarkColor, streamBlondeColor, 0.55f));
            ShakeCup(sputterShake);
            // 추출 버튼이 덜컹거린다
            extractButton.localPosition = buttonRestLocal + Vector3.down * buttonPressDepth * (0.6f + 0.4f * Mathf.Sin(elapsed * 40f));
            return;
        }
        extractButton.localPosition = buttonRestLocal + Vector3.down * buttonPressDepth;
        if (!sputterDone && extracted >= sputterAtMl)
        {
            StartSputter();
            return;
        }

        noiseTime += dt;
        float noise = Mathf.PerlinNoise(noiseSeed, noiseTime * noiseFrequency) * 2f - 1f;
        // 목표 하한까지 제곱 곡선으로 빨라진다
        float t = Mathf.Clamp01(extracted / curMinMl);
        float scale = Mathf.Lerp(flowStartScale, flowPeakScale, t * t);
        if (burstTimer > 0f)
        {
            burstTimer -= dt;
            scale *= burstScale;
        }
        float flow = flowMlPerSec * scale * Mathf.Max(0f, 1f + flowNoise * noise);

        SetStream(flow, length);

        float before = extracted;
        extracted += flow * dt;
        bool overflow = extracted > capacityMl + Epsilon;
        if (overflow) extracted = capacityMl;
        UpdateCupVisual();
        hud.SetAmount(extracted, capacityMl);
        UpdateZone(before);
        if (overflow) EndGame(false, "컵이 넘쳤어!");
    }

    private void StartSputter()
    {
        sputterDone = true;
        sputterTimer = sputterSeconds;
        hud.ShowSputter(spout.position, viewCamera, sputterSeconds);
        if (sfxSource != null && sputterClip != null) sfxSource.PlayOneShot(sputterClip);
    }

    // 멈춤이 끝나면 "푸슉!"과 함께 확 쏟아진다
    private void EndSputter()
    {
        sputterTimer = 0f;
        burstTimer = burstSeconds;
        // 버튼을 뗀 채 끝났으면 쏟아질 게 없으니 글씨만 지운다
        if (extracting) hud.ShowBurst(burstSeconds + 0.15f);
        else hud.HideAlert();
    }

    // 구간에 따라 줄기 색·게이지 색을 바꾸고, 한가운데를 지나는 순간 반짝, 넘친 뒤엔 컵을 떤다
    private void UpdateZone(float before)
    {
        float mid = (curMinMl + curMaxMl) * 0.5f;
        if (before < mid && extracted >= mid) hud.Flash();

        if (extracted < curMinMl)
        {
            SetStreamColor(streamDarkColor);
            hud.SetZone(0);
            cupModel.localPosition = cupRestLocal;
        }
        else if (extracted <= curMaxMl)
        {
            // 하한에서 크레마로 넘어가 한가운데에서 가장 진하다
            SetStreamColor(Color.Lerp(streamDarkColor, streamCremaColor, QualityOf(extracted, curMinMl, curMaxMl) * 0.6f + 0.4f));
            hud.SetZone(1);
            cupModel.localPosition = cupRestLocal;
        }
        else
        {
            SetStreamColor(streamBlondeColor);
            hud.SetZone(2);
            float danger = Mathf.Clamp01((extracted - curMaxMl) / Mathf.Max(1f, capacityMl - curMaxMl));
            ShakeCup(overflowShake * danger);
        }
    }

    // 부드러운 노이즈로 흔든다 (매 프레임 무작위면 화면이 지저분해진다)
    private void ShakeCup(float amount)
    {
        float t = Time.time * 30f;
        float x = Mathf.PerlinNoise(t, noiseSeed) * 2f - 1f;
        float z = Mathf.PerlinNoise(noiseSeed, t) * 2f - 1f;
        cupModel.localPosition = cupRestLocal + new Vector3(x, 0f, z) * amount;
    }

    private void SetStreamColor(Color c)
    {
        if (streamRenderer == null) return;
        streamRenderer.GetPropertyBlock(streamBlock);
        streamBlock.SetColor("_BaseColor", c);
        streamBlock.SetColor("_Color", c);
        streamBlock.SetColor("_EmissionColor", c * 0.5f);
        streamRenderer.SetPropertyBlock(streamBlock);
    }

    // 추출 버튼을 뗐을 때. 구간 안이면 성공, 넘었으면 실패, 모자라면 계속 진행
    private void Release()
    {
        if (InTarget()) EndGame(true, "성공!");
        else if (extracted > curMaxMl) EndGame(false, "너무 많이 뽑았어!");
    }

    // 시간 초과. 구간 안이면 성공으로 인정
    public override void TimeOver()
    {
        if (InTarget()) EndGame(true, "성공!");
        else if (extracted > curMaxMl) EndGame(false, "너무 많이 뽑았어!");
        else if (extracted > 0f) EndGame(false, "샷이 모자라!");
        else EndGame(false, "시간 초과!");
    }

    private bool InTarget()
    {
        return extracted >= curMinMl - Epsilon && extracted <= curMaxMl + Epsilon;
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        if (extracting) StopExtraction();
        if (success)
        {
            cup.espresso += extracted;
            quality = QualityOf(extracted, curMinMl, curMaxMl);
        }
        Finish(success, text);
    }

    private void SetStream(float flow, float length)
    {
        bool on = flow > 0f && length > 0f;
        stream.gameObject.SetActive(on);
        if (!on)
        {
            stream.localScale = streamRestScale;
            return;
        }
        float w = Mathf.Max(0.004f, flow * streamWidthPerFlow * 0.1f);
        stream.localScale = new Vector3(w, length, w);
    }

    private void UpdateCupVisual()
    {
        float existing = cup.TotalLiquid();
        float cap = existing + capacityMl;
        cupVisual.SetFill((existing + extracted) / cap);
        cupVisual.SetColorFrom(cup, extracted, 0f);
    }
}
