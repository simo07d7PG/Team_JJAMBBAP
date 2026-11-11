using UnityEngine;

// "우유 데워라!" — 마우스 위아래 = 스팀 노즐 깊이, 좌클릭을 누르는 동안 분사, 떼면 한 번 판정(재분사 불가).
// 주의: GAME_DIRECTION이 지정한 "조커위커 방식"은 링크를 열 수 없어 확인하지 못했다. 이 파일은 일반 스팀 원리(공기 → 가열)로 만든 대체안이다.
// 깊이 구간: 표면 위(튐 누적 시 실패) / 공기(거품 + 약한 가열) / 가열(빠른 가열). 컵은 건드리지 않는다(피처 안 우유).
public class SteamMilkMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private SteamMilkHud hud;
    [Tooltip("스팀 완드 피벗. 깊이에 따라 nozzleTop ~ nozzleBottom 사이를 오간다")]
    [SerializeField] private Transform wand;
    [Tooltip("깊이 0(우유 표면 위)일 때 완드 위치")]
    [SerializeField] private Transform nozzleTop;
    [Tooltip("깊이 1(바닥 근처)일 때 완드 위치")]
    [SerializeField] private Transform nozzleBottom;
    [Tooltip("분사 중에만 켜는 스팀 연출(노즐 끝 아래 흰 원뿔·구)")]
    [SerializeField] private GameObject steamEffect;
    [Tooltip("거품층. 피벗이 아래쪽인 묶음. localScale.y = 거품 높이")]
    [SerializeField] private Transform foamLayer;
    [Tooltip("우유 렌더러. 온도에 따라 색이 변한다")]
    [SerializeField] private Renderer milkRenderer;
    [Tooltip("튐 연출(표면 위 분사 중에만 켠다, 비워도 됨)")]
    [SerializeField] private GameObject splashEffect;

    [Header("깊이 구간 (0 = 표면 위, 1 = 바닥 근처)")]
    [Tooltip("이 값 미만 = 표면 위(튐)")]
    [SerializeField] private float airStartDepth = 0.12f;
    [Tooltip("이 값 이상 = 가열 구간. 그 사이가 공기 구간")]
    [SerializeField] private float heatStartDepth = 0.4f;

    [Header("온도 (℃)")]
    [SerializeField] private float startTemp = 5f;
    [Tooltip("이 온도에 닿으면 자동 실패")]
    [SerializeField] private float failTemp = 75f;
    [Tooltip("성공 온도 구간")]
    [SerializeField] private float tempMin = 55f;
    [SerializeField] private float tempMax = 70f;
    [Tooltip("등급 기준 온도(한가운데)")]
    [SerializeField] private float tempBest = 65f;
    [Tooltip("공기 구간에서 초당 오르는 온도")]
    [SerializeField] private float airHeatPerSec = 8f;
    [Tooltip("가열 구간에서 초당 오르는 온도")]
    [SerializeField] private float deepHeatPerSec = 14f;

    [Header("거품 (%)")]
    [Tooltip("공기 구간에서 초당 오르는 거품(%p)")]
    [SerializeField] private float foamPerSec = 26f;
    [Tooltip("성공 거품 구간")]
    [SerializeField] private float foamMin = 15f;
    [SerializeField] private float foamMax = 60f;
    [Tooltip("등급 기준 거품(한가운데)")]
    [SerializeField] private float foamBest = 35f;

    [Header("판정")]
    [Tooltip("표면 위에서 분사한 시간이 이만큼 쌓이면 \"튀었어!\"")]
    [SerializeField] private float splashLimitSeconds = 0.8f;
    [Tooltip("누른 지 이 시간 안에 뗀 것은 판정하지 않는다(초)")]
    [SerializeField] private float minSpraySeconds = 0.2f;
    [Tooltip("등급에서 온도 근접도가 차지하는 비율 (나머지는 거품)")]
    [SerializeField] private float tempQualityWeight = 0.6f;
    [Tooltip("온도 근접도 0이 되는 기준 온도와의 거리(℃)")]
    [SerializeField] private float tempQualityRange = 10f;
    [Tooltip("거품 근접도 0이 되는 기준 거품과의 거리(%p)")]
    [SerializeField] private float foamQualityRange = 25f;

    [Header("연출")]
    [Tooltip("거품 100%일 때 거품층 높이(m)")]
    [SerializeField] private float foamHeightAtFull = 0.08f;
    [Tooltip("우유 색: 차가울 때 / 75℃일 때")]
    [SerializeField] private Color milkColdColor = new Color(1f, 1f, 1f);
    [SerializeField] private Color milkHotColor = new Color(1f, 0.88f, 0.84f);
    [Tooltip("온도계 꼭대기(℃). 한계선이 보이도록 failTemp보다 조금 크게")]
    [SerializeField] private float gaugeMaxTemp = 80f;

    // 일시정지 판정: timeScale이 이 값 이하면 입력을 받지 않는다
    private const float PausedTimeScale = 0f;
    private const float PercentScale = 100f;

    // 결과 문구
    private const string TextSuccess = "따끈해!";
    private const string TextTooHot = "너무 뜨거워!";
    private const string TextCold = "미지근해!";
    private const string TextNoFoam = "거품이 없어!";
    private const string TextTooFoamy = "거품이 너무 많아!";
    private const string TextSplash = "튀었어!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock block;
    private float temp;
    private float foam;
    private float depth;
    private float splashTimer;
    private float sprayTimer;
    private bool spraying;
    private bool inSplash;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
    }

    protected override void ResetGame()
    {
        temp = startTemp;
        foam = 0f;
        depth = 0f;
        splashTimer = 0f;
        sprayTimer = 0f;
        spraying = false;
        inSplash = false;
        hud.Configure(gaugeMaxTemp, tempMin, tempMax, failTemp, foamMin, foamMax, airStartDepth, heatStartDepth);
        UpdateVisuals();
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (Time.timeScale <= PausedTimeScale) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        depth = DepthFromPointer();

        if (!spraying && ClickPressed())
        {
            spraying = true;
            sprayTimer = 0f;
        }

        inSplash = false;
        if (spraying)
        {
            if (ClickHeld())
            {
                sprayTimer += dt;
                Spray(dt);
                if (isFinished) return;
            }
            else if (sprayTimer < minSpraySeconds)
            {
                // 누르자마자 뗀 것은 판정하지 않고 다시 누를 수 있게 둔다
                spraying = false;
            }
            else
            {
                Judge();
                return;
            }
        }
        UpdateVisuals();
    }

    // 마우스 Y를 화면 높이 비율로 바꾼다. 위 = 0(얕게), 아래 = 1(깊게). 해상도와 무관
    private float DepthFromPointer()
    {
        return Mathf.Clamp01(1f - PointerPosition().y / Mathf.Max(1f, Screen.height));
    }

    // 누르고 있는 동안 깊이 구간에 따라 변화를 쌓는다
    private void Spray(float dt)
    {
        if (depth < airStartDepth)
        {
            inSplash = true;
            splashTimer += dt;
            if (splashTimer >= splashLimitSeconds)
            {
                spraying = false;
                UpdateVisuals();
                Finish(false, TextSplash);
                return;
            }
        }
        else if (depth < heatStartDepth)
        {
            temp += airHeatPerSec * dt;
            foam = Mathf.Min(PercentScale, foam + foamPerSec * dt);
        }
        else
        {
            temp += deepHeatPerSec * dt;
        }

        if (temp >= failTemp)
        {
            temp = failTemp;
            spraying = false;
            UpdateVisuals();
            Finish(false, TextTooHot);
        }
    }

    // 뗀 순간 한 번 판정
    private void Judge()
    {
        spraying = false;
        UpdateVisuals();
        if (temp < tempMin) Finish(false, TextCold);
        else if (temp > tempMax) Finish(false, TextTooHot);
        else if (foam < foamMin) Finish(false, TextNoFoam);
        else if (foam > foamMax) Finish(false, TextTooFoamy);
        else
        {
            float tempScore = Mathf.Clamp01(1f - Mathf.Abs(temp - tempBest) / tempQualityRange);
            float foamScore = Mathf.Clamp01(1f - Mathf.Abs(foam - foamBest) / foamQualityRange);
            quality = Mathf.Clamp01(tempQualityWeight * tempScore + (1f - tempQualityWeight) * foamScore);
            Finish(true, TextSuccess);
        }
    }

    public override void TimeOver()
    {
        spraying = false;
        UpdateVisuals();
        Finish(false, TextTimeOver);
    }

    private void UpdateVisuals()
    {
        wand.position = Vector3.Lerp(nozzleTop.position, nozzleBottom.position, depth);
        steamEffect.SetActive(spraying && isPlaying);
        if (splashEffect != null) splashEffect.SetActive(inSplash && isPlaying);

        float foam01 = foam / PercentScale;
        foamLayer.gameObject.SetActive(foam01 > 0f);
        foamLayer.localScale = new Vector3(foamLayer.localScale.x, foamHeightAtFull * foam01, foamLayer.localScale.z);

        float heat01 = Mathf.Clamp01((temp - startTemp) / (failTemp - startTemp));
        Color c = Color.Lerp(milkColdColor, milkHotColor, heat01);
        milkRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        milkRenderer.SetPropertyBlock(block);

        hud.SetState(temp, foam, depth, splashTimer / splashLimitSeconds, inSplash);
    }
}
