using UnityEngine;

// "얼음 퍼라!" — 스쿱으로 제빙기에서 얼음을 퍼서 컵에 목표 개수만큼 담는다.
// 제빙기 안에서 좌클릭을 누르고 있으면 담기고, 우클릭을 누르고 있으면 스쿱이 기울어 쏟아진다.
public class IceScoopMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private MouseSpringFollower scoop;
    [Tooltip("새 얼음이 생기는 위치 (스쿱 안쪽)")]
    [SerializeField] private Transform scoopFillPoint;
    [Tooltip("스쿱 안을 덮는 트리거. 스쿱에 담긴 개수를 셀 때 쓴다")]
    [SerializeField] private Collider scoopZone;
    [Tooltip("제빙기 얼음통 영역 트리거. 스쿱 입구가 이 안에 있을 때만 퍼진다")]
    [SerializeField] private Collider binZone;
    [Tooltip("컵 안을 덮는 트리거. 여기 들어간 얼음을 센다")]
    [SerializeField] private Collider cupZone;
    [SerializeField] private IcePiecePool icePool;
    [SerializeField] private CupVisual cupVisual;
    [SerializeField] private IceScoopHud hud;

    [Header("손맛")]
    [Tooltip("얼음이 생길 때 위치를 흩뜨리는 반경")]
    [SerializeField] private float spawnJitter = 0.03f;
    [Tooltip("얼음 크기 무작위 폭(기본 크기의 ±비율)")]
    [SerializeField] private float sizeVariation = 0.15f;
    [Tooltip("컵 액체 높이 표시용 컵 용량(ml)")]
    [SerializeField] private float cupCapacityMl = 300f;

    [Header("수치")]
    [Tooltip("목표 개수 구간")]
    [SerializeField] private int targetMin = 4;
    [SerializeField] private int targetMax = 7;
    [Tooltip("이 개수를 넘으면 넘침 실패")]
    [SerializeField] private int cupMax = 10;
    [Tooltip("목표 구간에 머물러야 하는 시간(초)")]
    [SerializeField] private float holdSeconds = 1f;
    [Tooltip("제빙기 안에서 누르고 있을 때 초당 담기는 얼음 수")]
    [SerializeField] private float scoopRate = 8f;
    [Tooltip("스쿱에 한 번에 담기는 최대 개수")]
    [SerializeField] private int scoopCapacity = 8;

    private Vector3 scoopStartPos;
    private Quaternion scoopStartRot;
    private float spawnTimer;
    private int countInCup;
    private float holdTimer;

    private void Awake()
    {
        scoopStartPos = scoop.transform.position;
        scoopStartRot = scoop.transform.rotation;
    }

    protected override void ResetGame()
    {
        icePool.DespawnAll();
        scoop.viewCamera = viewCamera;
        scoop.ResetTo(scoopStartPos, scoopStartRot);
        scoop.inputEnabled = true;
        spawnTimer = 0f;
        countInCup = 0;
        holdTimer = 0f;

        hud.Configure(targetMin, targetMax, cupMax);
        // 컵 모델 상태를 이전 내용물에 맞춘다
        cupVisual.SetIceVisible(cup.ice > 0);
        cupVisual.SetFill(cup.TotalLiquid() / cupCapacityMl);
        cupVisual.SetColorFrom(cup, 0f, 0f);
    }

    private void Update()
    {
        if (!isPlaying)
        {
            scoop.inputEnabled = false;
            return;
        }

        float dt = Time.deltaTime;
        bool held = ClickHeld();
        scoop.pointerPosition = PointerPosition();
        scoop.grabHeld = held;
        scoop.tiltHeld = RightHeld();

        // 제빙기 안에서 누르고 있으면 스쿱에 얼음이 담긴다
        if (held && IsInside(binZone, scoopFillPoint.position))
        {
            spawnTimer += dt * scoopRate;
            while (spawnTimer >= 1f)
            {
                spawnTimer -= 1f;
                if (CountInZone(scoopZone) >= scoopCapacity)
                {
                    spawnTimer = 0f;
                    break;
                }
                SpawnIceInScoop();
            }
        }
        else
        {
            spawnTimer = 0f;
        }

        countInCup = CountInCup();
        Judge(dt);

        float hold01 = 1f;
        if (holdSeconds > 0f) hold01 = Mathf.Clamp01(holdTimer / holdSeconds);
        hud.SetState(countInCup, hold01);
    }

    // 넘치면 실패, 목표 구간에 holdSeconds 동안 머물면 성공
    private void Judge(float dt)
    {
        if (countInCup > cupMax)
        {
            EndGame(false, "얼음이 넘쳤어!");
            return;
        }

        if (countInCup >= targetMin && countInCup <= targetMax)
        {
            holdTimer += dt;
            if (holdTimer >= holdSeconds - 0.0001f) EndGame(true, "성공!");
        }
        else
        {
            holdTimer = 0f;
        }
    }

    // 시간 초과. 유지 시간을 못 채웠으면 구간 안이어도 실패
    public override void TimeOver()
    {
        if (countInCup >= targetMin && countInCup <= targetMax) EndGame(false, "시간 초과!");
        else if (countInCup > targetMax) EndGame(false, "얼음이 너무 많아!");
        else if (countInCup > 0) EndGame(false, "얼음이 모자라!");
        else EndGame(false, "시간 초과!");
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        scoop.inputEnabled = false;
        if (success)
        {
            cup.ice += countInCup;
            cupVisual.SetIceVisible(true);
        }
        Finish(success, text);
    }

    private void SpawnIceInScoop()
    {
        Vector2 j = Random.insideUnitCircle * spawnJitter;
        Vector3 pos = scoopFillPoint.position + new Vector3(j.x, 0f, j.y);
        float size = 1f + (Random.value * 2f - 1f) * sizeVariation;
        IcePiece piece = icePool.Spawn(pos, Random.rotationUniform, size);
        if (piece != null) piece.body.linearVelocity = scoop.GetBody().linearVelocity;
    }

    // 컵 안 얼음을 센다. 스쿱에 담긴 채로 컵 영역에 들어간 얼음은 세지 않는다(기울여서 쏟아야 한다)
    private int CountInCup()
    {
        int count = 0;
        for (int i = 0; i < icePool.active.Count; i++)
        {
            Vector3 p = icePool.active[i].transform.position;
            if (IsInside(cupZone, p) && !IsInside(scoopZone, p)) count++;
        }
        return count;
    }

    private int CountInZone(Collider zone)
    {
        int count = 0;
        for (int i = 0; i < icePool.active.Count; i++)
        {
            if (IsInside(zone, icePool.active[i].transform.position)) count++;
        }
        return count;
    }

    // 트리거 영역 안에 점이 있는지. 영역은 Box/Sphere/Capsule 콜라이더여야 한다
    private bool IsInside(Collider zone, Vector3 point)
    {
        if (!zone.enabled || !zone.gameObject.activeInHierarchy) return false;
        return (zone.ClosestPoint(point) - point).sqrMagnitude < 0.000001f;
    }
}
