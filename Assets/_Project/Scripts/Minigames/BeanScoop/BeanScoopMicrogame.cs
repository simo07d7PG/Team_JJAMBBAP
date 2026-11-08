using UnityEngine;
using UnityEngine.InputSystem;

// "원두 넣어라!" — 주걱으로 원두 주머니에서 원두를 퍼서 그라인더에 던져 넣는다.
// 주머니 안에서 좌클릭을 누르고 있으면 담기고, Q를 누르면 주걱 안 원두가 앞으로 날아간다. 필요한 양만 넘기면 성공.
public class BeanScoopMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private MouseSpringFollower scoop;
    [Tooltip("새 원두가 생기는 위치 (주걱 안쪽)")]
    [SerializeField] private Transform scoopFillPoint;
    [Tooltip("주걱 안을 덮는 트리거. 주걱에 담긴 원두를 고를 때 쓴다")]
    [SerializeField] private Collider scoopZone;
    [Tooltip("원두 주머니 영역 트리거. 주걱이 이 안에 있을 때만 퍼진다")]
    [SerializeField] private Collider bagZone;
    [Tooltip("그라인더 통 안을 덮는 트리거. 여기 들어간 원두를 센다")]
    [SerializeField] private Collider hopperZone;
    [Tooltip("원두 알갱이 풀 (얼음 풀과 같은 컴포넌트)")]
    [SerializeField] private IcePiecePool beanPool;
    [SerializeField] private IceScoopHud hud;
    [Tooltip("던지면 떨어질 자리 표시. 그라인더 입구 높이에 둔다. 비우면 표시 없음")]
    [SerializeField] private Transform landingMarker;
    [Tooltip("던지기 입력 (InputSystem_Actions > UI/Throw, Q)")]
    [SerializeField] private InputActionReference throwAction;

    [Header("손맛")]
    [Tooltip("원두가 생길 때 위치를 흩뜨리는 반경")]
    [SerializeField] private float spawnJitter = 0.03f;
    [Tooltip("원두 크기 무작위 폭(기본 크기의 ±비율)")]
    [SerializeField] private float sizeVariation = 0.15f;
    [Tooltip("던질 때 알갱이마다 속도를 흩뜨리는 정도(m/초)")]
    [SerializeField] private float throwJitter = 0.12f;
    [Tooltip("던진 뒤 주걱이 튕기는 시간(초)")]
    [SerializeField] private float flickSeconds = 0.12f;
    [Tooltip("던지는 순간 원두를 띄우는 높이. 주걱 앞턱에 걸리지 않게 한다")]
    [SerializeField] private float throwLift = 0.03f;

    [Header("수치")]
    [Tooltip("그라인더에 이만큼 들어가면 성공")]
    [SerializeField] private int neededCount = 10;
    [Tooltip("주머니 안에서 누르고 있을 때 초당 담기는 원두 수")]
    [SerializeField] private float scoopRate = 12f;
    [Tooltip("주걱에 한 번에 담기는 최대 개수")]
    [SerializeField] private int scoopCapacity = 8;
    [Tooltip("던지는 방향 (미니게임 루트 기준)")]
    [SerializeField] private Vector3 throwDirection = Vector3.right;
    [Tooltip("던질 때 앞으로 가는 속도(m/초)")]
    [SerializeField] private float throwSpeed = 1.5f;
    [Tooltip("던질 때 위로 뜨는 속도(m/초)")]
    [SerializeField] private float throwUpSpeed = 1.8f;

    // 점이 트리거 영역 안인지 판정할 때 허용하는 거리 제곱 오차
    private const float InsideSqrTolerance = 0.000001f;
    // 넘침·구간이 없어 등급은 항상 '좋아요' (완벽 0.7 미만, 좋아요 0.3 이상)
    private const float FixedQuality = 0.5f;

    private Vector3 scoopStartPos;
    private Quaternion scoopStartRot;
    private float spawnTimer;
    private float flickTimer;
    private int countInHopper;

    private void Awake()
    {
        scoopStartPos = scoop.transform.position;
        scoopStartRot = scoop.transform.rotation;
    }

    protected override void ResetGame()
    {
        throwAction.action.Enable();
        beanPool.DespawnAll();
        scoop.viewCamera = viewCamera;
        scoop.ResetTo(scoopStartPos, scoopStartRot);
        scoop.inputEnabled = true;
        spawnTimer = 0f;
        flickTimer = 0f;
        countInHopper = 0;

        // 칸 수 = 필요한 개수. 마지막 칸이 목표 칸
        hud.Configure(neededCount, neededCount, neededCount);
        UpdateLandingMarker();
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

        // 일시정지(timeScale 0) 중에는 던지지 않는다
        if (Time.timeScale > 0f && throwAction.action.WasPressedThisFrame()) ThrowBeans();
        if (flickTimer > 0f) flickTimer -= dt;
        scoop.tiltHeld = flickTimer > 0f;

        FillScoop(held, dt);
        UpdateLandingMarker();

        countInHopper = CountInHopper();
        hud.SetState(countInHopper, Mathf.Clamp01((float)countInHopper / neededCount));
        if (countInHopper >= neededCount) EndGame(true, "성공!");
    }

    // 주머니 안에서 누르고 있으면 주걱에 원두가 담긴다
    private void FillScoop(bool held, float dt)
    {
        if (held && IsInside(bagZone, scoopFillPoint.position))
        {
            spawnTimer += dt * scoopRate;
            while (spawnTimer >= 1f)
            {
                spawnTimer -= 1f;
                if (CountInScoop() >= scoopCapacity)
                {
                    spawnTimer = 0f;
                    break;
                }
                SpawnBeanInScoop();
            }
        }
        else
        {
            spawnTimer = 0f;
        }
    }

    // 주걱 안 원두를 전부 앞으로 던진다. 주걱은 잠깐 튕긴다
    private void ThrowBeans()
    {
        Vector3 velocity = ThrowForward() * throwSpeed + Vector3.up * throwUpSpeed;
        for (int i = 0; i < beanPool.active.Count; i++)
        {
            IcePiece bean = beanPool.active[i];
            if (!IsInside(scoopZone, bean.transform.position)) continue;
            bean.body.position = bean.body.position + Vector3.up * throwLift;
            bean.body.linearVelocity = velocity + Random.insideUnitSphere * throwJitter;
        }
        flickTimer = flickSeconds;
    }

    private Vector3 ThrowForward()
    {
        return transform.TransformDirection(throwDirection).normalized;
    }

    // 지금 던지면 떨어질 자리에 표시를 둔다 (표시 높이까지 떨어지는 동안 가는 거리)
    private void UpdateLandingMarker()
    {
        if (landingMarker == null) return;
        float drop = scoopFillPoint.position.y - landingMarker.position.y;
        float gravity = -Physics.gravity.y;
        float fallSeconds = (throwUpSpeed + Mathf.Sqrt(Mathf.Max(0f, throwUpSpeed * throwUpSpeed + 2f * gravity * drop))) / gravity;
        Vector3 p = scoopFillPoint.position + ThrowForward() * (throwSpeed * fallSeconds);
        p.y = landingMarker.position.y;
        landingMarker.position = p;
    }

    // 시간 초과. 필요한 양을 못 채웠으면 실패
    public override void TimeOver()
    {
        if (countInHopper > 0) EndGame(false, "원두가 모자라!");
        else EndGame(false, "시간 초과!");
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        scoop.inputEnabled = false;
        if (success) quality = FixedQuality;
        Finish(success, text);
    }

    private void SpawnBeanInScoop()
    {
        Vector2 j = Random.insideUnitCircle * spawnJitter;
        Vector3 pos = scoopFillPoint.position + new Vector3(j.x, 0f, j.y);
        float size = 1f + (Random.value * 2f - 1f) * sizeVariation;
        IcePiece bean = beanPool.Spawn(pos, Random.rotationUniform, size);
        if (bean != null) bean.body.linearVelocity = scoop.GetBody().linearVelocity;
    }

    // 그라인더 통 안 원두를 센다. 주걱에 담긴 채로 통 영역에 걸친 원두는 세지 않는다
    private int CountInHopper()
    {
        int count = 0;
        for (int i = 0; i < beanPool.active.Count; i++)
        {
            Vector3 p = beanPool.active[i].transform.position;
            if (IsInside(hopperZone, p) && !IsInside(scoopZone, p)) count++;
        }
        return count;
    }

    private int CountInScoop()
    {
        int count = 0;
        for (int i = 0; i < beanPool.active.Count; i++)
        {
            if (IsInside(scoopZone, beanPool.active[i].transform.position)) count++;
        }
        return count;
    }

    // 트리거 영역 안에 점이 있는지. 영역은 Box/Sphere/Capsule 콜라이더여야 한다
    private bool IsInside(Collider zone, Vector3 point)
    {
        if (!zone.enabled || !zone.gameObject.activeInHierarchy) return false;
        return (zone.ClosestPoint(point) - point).sqrMagnitude < InsideSqrTolerance;
    }
}
