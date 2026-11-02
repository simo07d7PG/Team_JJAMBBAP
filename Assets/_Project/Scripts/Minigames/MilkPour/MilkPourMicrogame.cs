using UnityEngine;

// "우유 부어라!" — 좌클릭으로 우유팩을 잡고 우클릭으로 기울여 컵을 목표 선까지 채운 뒤 다시 세운다.
// 유체 시뮬레이션 없이 "레이 판정 + 양 계산"으로 하고, 줄기는 보이기용이다.
public class MilkPourMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private MouseSpringFollower carton;
    [Tooltip("우유팩 입구")]
    [SerializeField] private Transform spout;
    [Tooltip("컵 입구를 덮는 콜라이더. 입구에서 아래로 쏜 레이가 여기 맞으면 컵에 들어간 것")]
    [SerializeField] private Collider cupMouth;
    [Tooltip("바닥 높이 기준 (줄기 길이)")]
    [SerializeField] private Transform floor;
    [SerializeField] private CupVisual cupVisual;
    [SerializeField] private MilkPourHud hud;
    [Tooltip("우유 줄기 피벗. localScale.y = 길이, 위쪽이 원점")]
    [SerializeField] private Transform stream;

    [Header("손맛")]
    [SerializeField] private float cupCapacityMl = 300f;
    [Tooltip("얼음 1개가 차지하는 부피(ml). 액체 높이 계산에 더한다")]
    [SerializeField] private float iceDisplacementMl = 8f;
    [Tooltip("이 각도부터 우유가 나온다")]
    [SerializeField] private float pourStartAngle = 45f;
    [Tooltip("이 각도에서 최대 속도")]
    [SerializeField] private float pourFullAngle = 110f;
    [Tooltip("붓기 시작 각도보다 이만큼 세우면 '다시 세웠다'로 본다")]
    [SerializeField] private float stopHysteresis = 8f;
    [Tooltip("이 속도(도/초) 이상으로 기울이며 붓기 시작하면 콸 구간")]
    [SerializeField] private float glugTiltSpeed = 120f;
    [SerializeField] private float glugSeconds = 0.35f;
    [SerializeField] private float streamWidth = 0.025f;

    [Header("수치")]
    [Tooltip("목표 높이 구간(컵 용량 대비 0~1)")]
    [Range(0f, 1f)] [SerializeField] private float targetMin01 = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float targetMax01 = 0.85f;
    [Tooltip("주문 변형(우유 적게·많이)마다 목표 구간을 옮기는 비율")]
    [SerializeField] private float variantShift01 = 0.15f;
    [Tooltip("최대로 기울였을 때 초당 붓는 양(ml)")]
    [SerializeField] private float maxFlowMlPerSec = 140f;
    [Tooltip("컵 밖으로 부어도 되는 한도(ml)")]
    [SerializeField] private float maxOutsideMl = 60f;
    [Tooltip("휙 기울였을 때 처음에 콸 쏟아지는 배율")]
    [SerializeField] private float glugMultiplier = 2f;

    // 경계값을 성공으로 인정하는 여유
    private const float Epsilon = 0.0001f;
    // 목표 구간 하한(curMin01)이 가질 수 있는 범위
    private const float MinBandLow01 = 0.1f;
    private const float MinBandHigh01 = 0.85f;
    // 목표 구간 상한은 하한보다 최소 이만큼 위
    private const float MinBandGap01 = 0.1f;
    // 목표 구간 상한(curMax01)의 최댓값
    private const float MaxBandHigh01 = 0.96f;
    // 입구에서 컵 입구 콜라이더를 찾는 레이 길이
    private const float MouthRayLength = 5f;

    private Vector3 cartonStartPos;
    private Quaternion cartonStartRot;
    private Vector3 streamRestScale;

    private bool pouring;
    private float lastAngle;
    private float glugRemaining;
    // 시작할 때 컵에 이미 있던 양과 지금 컵 안의 총량(ml 환산, 용량을 넘지 않음)
    private float initialMl;
    private float liquidMl;
    private float outsideMl;
    // 이번 주문의 목표 구간 (variant 반영)
    private float curMin01;
    private float curMax01;

    private void Awake()
    {
        cartonStartPos = carton.transform.position;
        cartonStartRot = carton.transform.rotation;
        streamRestScale = stream.localScale;
    }

    protected override void ResetGame()
    {
        carton.viewCamera = viewCamera;
        carton.ResetTo(cartonStartPos, cartonStartRot);
        carton.inputEnabled = true;
        pouring = false;
        lastAngle = 0f;
        glugRemaining = 0f;
        HideStream();

        // 기존 내용물(에스프레소·얼음)을 액체 높이에 더한다
        initialMl = Mathf.Clamp(cup.TotalLiquid() + cup.ice * iceDisplacementMl, 0f, cupCapacityMl);
        liquidMl = initialMl;
        outsideMl = 0f;

        curMin01 = Mathf.Clamp(targetMin01 + variant * variantShift01, MinBandLow01, MinBandHigh01);
        curMax01 = Mathf.Clamp(targetMax01 + variant * variantShift01, curMin01 + MinBandGap01, MaxBandHigh01);
        hud.Configure(curMin01, curMax01);
        cupVisual.SetIceVisible(cup.ice > 0);
        UpdateCupDisplay();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            carton.inputEnabled = false;
            return;
        }

        carton.pointerPosition = PointerPosition();
        carton.grabHeld = ClickHeld();
        carton.tiltHeld = RightHeld();

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float angle = carton.ActualTilt();
        float tiltSpeed = (angle - lastAngle) / dt;
        lastAngle = angle;

        if (!pouring)
        {
            if (angle < pourStartAngle) return;
            pouring = true;
            // 휙 기울이면 처음에 콸 쏟아진다
            glugRemaining = tiltSpeed >= glugTiltSpeed ? glugSeconds : 0f;
        }
        else if (angle < pourStartAngle - stopHysteresis)
        {
            // 팩을 다시 세웠다 → 판정
            pouring = false;
            HideStream();
            StopPouring();
            return;
        }

        if (angle < pourStartAngle)
        {
            // 히스테리시스 구간: 아직 멈춘 건 아니지만 나오지는 않는다
            HideStream();
            return;
        }

        float ml = ComputeFlowMl(angle, dt);
        if (ml <= 0f)
        {
            HideStream();
            return;
        }

        float hitDistance;
        if (TryHitCup(out hitDistance)) PourIntoCup(ml, hitDistance);
        else SpillOutside(ml);
    }

    // 이번 프레임에 나오는 양(ml). 콸 구간이면 배율을 적용하고 남은 시간을 줄인다
    private float ComputeFlowMl(float angle, float dt)
    {
        float flow = maxFlowMlPerSec * Mathf.InverseLerp(pourStartAngle, pourFullAngle, angle);
        if (glugRemaining > 0f)
        {
            flow *= glugMultiplier;
            glugRemaining -= dt;
        }
        return flow * dt;
    }

    // 컵 안으로 들어간 경우: 줄기는 컵 입구까지, 용량을 넘치면 실패
    private void PourIntoCup(float ml, float hitDistance)
    {
        SetStream(true, hitDistance);
        liquidMl += ml;
        bool overflow = liquidMl > cupCapacityMl + Epsilon;
        if (overflow) liquidMl = cupCapacityMl;
        UpdateCupDisplay();
        if (overflow) EndGame(false, "우유가 넘쳤어!");
    }

    // 컵 밖으로 흘린 경우: 줄기는 바닥까지, 한도를 넘기면 실패
    private void SpillOutside(float ml)
    {
        SetStream(true, DistanceToFloor());
        outsideMl += ml;
        hud.SetSpill(outsideMl / maxOutsideMl);
        if (outsideMl > maxOutsideMl + Epsilon) EndGame(false, "우유를 흘렸어!");
    }

    // 입구에서 바닥까지의 높이(줄기 길이)
    private float DistanceToFloor()
    {
        return Mathf.Max(0f, spout.position.y - floor.position.y);
    }

    private float PouredInMl()
    {
        return liquidMl - initialMl;
    }

    // 컵에 우유가 한 방울이라도 들어갔는지
    private bool HasPoured()
    {
        return PouredInMl() > 0f;
    }

    private float Fill01()
    {
        return liquidMl / cupCapacityMl;
    }

    private bool InTarget()
    {
        float fill = Fill01();
        return fill >= curMin01 - Epsilon && fill <= curMax01 + Epsilon;
    }

    // 팩을 다시 세웠을 때. 모자라거나 컵에 한 방울도 안 들어갔으면 계속 진행
    private void StopPouring()
    {
        if (!HasPoured()) return;
        EndIfInTargetOrOver();
    }

    public override void TimeOver()
    {
        if (!HasPoured()) EndGame(false, "시간 초과!");
        else if (!EndIfInTargetOrOver()) EndGame(false, "너무 적게 부었어!");
    }

    // 목표 구간이면 성공, 넘었으면 실패로 끝내고 true. 아직 모자라면 false
    private bool EndIfInTargetOrOver()
    {
        if (InTarget())
        {
            EndGame(true, "성공!");
            return true;
        }
        if (Fill01() > curMax01)
        {
            EndGame(false, "너무 많이 부었어!");
            return true;
        }
        return false;
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        pouring = false;
        HideStream();
        carton.inputEnabled = false;
        if (success)
        {
            cup.milk += Mathf.Max(0f, PouredInMl());
            quality = QualityOf(Fill01(), curMin01, curMax01);
        }
        Finish(success, text);
    }

    // 입구에서 아래로 레이를 쏴서 컵 입구 콜라이더에 맞는지. 다른 콜라이더는 보지 않는다
    private bool TryHitCup(out float distance)
    {
        distance = 0f;
        Ray ray = new Ray(spout.position, Vector3.down);
        RaycastHit hit;
        if (!cupMouth.Raycast(ray, out hit, MouthRayLength)) return false;
        distance = hit.distance;
        return true;
    }

    private void HideStream()
    {
        SetStream(false, 0f);
    }

    private void SetStream(bool on, float length)
    {
        on = on && length > 0f;
        stream.gameObject.SetActive(on);
        if (!on)
        {
            stream.localScale = streamRestScale;
            return;
        }
        // 줄기는 항상 수직 아래로
        stream.SetPositionAndRotation(spout.position, Quaternion.identity);
        stream.localScale = new Vector3(streamWidth, length, streamWidth);
    }

    private void UpdateCupDisplay()
    {
        cupVisual.SetFill(Fill01());
        cupVisual.SetColorFrom(cup, 0f, Mathf.Max(0f, PouredInMl()));
        hud.SetFill(Fill01());
    }
}
