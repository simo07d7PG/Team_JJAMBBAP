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

    // 경계값을 성공으로 인정하는 여유
    private const float Epsilon = 0.0001f;

    private Vector3 buttonRestLocal;
    private Vector3 streamRestScale;
    private bool extracting;
    private float extracted;
    private float noiseTime;
    private float noiseSeed;
    // 이번 주문의 목표 구간 (variant 반영)
    private float curMinMl;
    private float curMaxMl;

    private void Awake()
    {
        buttonRestLocal = extractButton.localPosition;
        streamRestScale = stream.localScale;
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
        hud.Configure(curMinMl, curMaxMl, capacityMl);
        UpdateCupVisual();
        cupVisual.SetIceVisible(cup.ice > 0);
    }

    private void Update()
    {
        if (!isPlaying)
        {
            if (extracting) StopExtraction();
            return;
        }

        if (!extracting && ClickPressed()) StartExtraction();
        if (!extracting) return;

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
        noiseTime += dt;
        float noise = Mathf.PerlinNoise(noiseSeed, noiseTime * noiseFrequency) * 2f - 1f;
        float flow = flowMlPerSec * Mathf.Max(0f, 1f + flowNoise * noise);

        SetStream(flow, Mathf.Max(0f, spout.position.y - cupModel.position.y));

        extracted += flow * dt;
        bool overflow = extracted > capacityMl + Epsilon;
        if (overflow) extracted = capacityMl;
        UpdateCupVisual();
        hud.SetAmount(extracted, capacityMl);
        if (overflow) EndGame(false, "컵이 넘쳤어!");
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
