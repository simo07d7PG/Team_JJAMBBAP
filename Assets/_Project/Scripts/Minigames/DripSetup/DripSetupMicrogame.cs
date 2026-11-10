using UnityEngine;

// "원두랑 물 넣어라!" — 가루 통은 드리퍼에, 물병은 주전자에 붓는다.
// 물건 위에서 좌클릭을 누르고 있으면 잡히고, 잡은 채 우클릭을 누르고 있으면 기울어져 붓는다. 알갱이·액체 시뮬레이션 없이 게이지만 찬다.
public class DripSetupMicrogame : Minigame
{
    [Header("참조 - 가루 통 (왼쪽)")]
    [SerializeField] private MouseSpringFollower groundsJar;
    [Tooltip("잡기 판정용 콜라이더 (가루 통 몸통)")]
    [SerializeField] private Collider groundsJarGrab;
    [Tooltip("가루 통 입구. 여기서 아래로 쏜 레이로 목표를 찾는다")]
    [SerializeField] private Transform groundsSpout;
    [Tooltip("가루 줄기 피벗. localScale.y = 길이, 위쪽이 원점")]
    [SerializeField] private Transform groundsStream;
    [Tooltip("잡을 수 있을 때 켜는 얇은 강조")]
    [SerializeField] private GameObject groundsHighlight;

    [Header("참조 - 물병 (오른쪽)")]
    [SerializeField] private MouseSpringFollower waterBottle;
    [Tooltip("잡기 판정용 콜라이더 (물병 몸통)")]
    [SerializeField] private Collider waterBottleGrab;
    [Tooltip("물병 입구. 여기서 아래로 쏜 레이로 목표를 찾는다")]
    [SerializeField] private Transform waterSpout;
    [Tooltip("물 줄기 피벗. localScale.y = 길이, 위쪽이 원점")]
    [SerializeField] private Transform waterStream;
    [Tooltip("잡을 수 있을 때 켜는 얇은 강조")]
    [SerializeField] private GameObject waterHighlight;

    [Header("참조 - 목표")]
    [Tooltip("드리퍼 입구를 덮는 트리거 콜라이더")]
    [SerializeField] private Collider dripperMouth;
    [Tooltip("드리퍼 안 가루 피벗. 프리팹에 둔 크기 = 100%, localScale.y만 채움 비율로 줄인다. 아래쪽이 원점")]
    [SerializeField] private Transform dripperContent;
    [Tooltip("주전자 입구를 덮는 트리거 콜라이더")]
    [SerializeField] private Collider kettleMouth;
    [Tooltip("주전자 안 물 피벗. 프리팹에 둔 크기 = 100%, localScale.y만 채움 비율로 줄인다. 아래쪽이 원점")]
    [SerializeField] private Transform kettleContent;
    [Tooltip("바닥 높이 기준 (목표 밖으로 부을 때 줄기 길이)")]
    [SerializeField] private Transform floor;
    [SerializeField] private DripSetupHud hud;

    [Header("손맛")]
    [Tooltip("이 각도 이상 기울면 붓는 중")]
    [SerializeField] private float pourAngle = 45f;
    [SerializeField] private float streamWidth = 0.025f;

    [Header("수치")]
    [Tooltip("가루 → 드리퍼 초당 채움(0~1). 0.55 = 초당 55%p")]
    [SerializeField] private float groundsRate = 0.55f;
    [Tooltip("물 → 주전자 초당 채움(0~1). 0.45 = 초당 45%p")]
    [SerializeField] private float waterRate = 0.45f;
    [Tooltip("가루 목표 구간(0~1)")]
    [Range(0f, 1f)] [SerializeField] private float groundsMin01 = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float groundsMax01 = 0.9f;
    [Tooltip("물 목표 구간(0~1)")]
    [Range(0f, 1f)] [SerializeField] private float waterMin01 = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float waterMax01 = 0.8f;
    [Tooltip("둘 다 구간 안이고 붓기를 멈춘 채 이만큼 지나면 성공(초)")]
    [SerializeField] private float settleSeconds = 0.3f;
    [Tooltip("엉뚱한 목표에 이만큼 이어서 부으면 실패(초)")]
    [SerializeField] private float wrongSeconds = 0.3f;

    // 물건·목표 번호. 물건 i는 목표 i에 부어야 한다
    private const int Grounds = 0;
    private const int Water = 1;
    private const int Count = 2;
    // 경계값을 성공으로 인정하는 여유
    private const float Epsilon = 0.0001f;
    // 입구에서 목표 입구를 찾는 레이 길이, 잡기 레이 길이
    private const float MouthRayLength = 5f;
    private const float GrabRayLength = 100f;

    private MouseSpringFollower[] items;
    private Collider[] grabColliders;
    private Transform[] spouts;
    private Transform[] streams;
    private GameObject[] highlights;
    private Collider[] mouths;
    private Transform[] contents;
    private float[] rates;

    private Vector3[] startPos;
    private Quaternion[] startRot;
    private Vector3[] streamRestScale;
    private Vector3[] contentRestScale;

    // 목표별 채움(0~1)
    private float[] fill;
    // 잡고 있는 물건 번호. -1 = 없음
    private int heldIndex;
    private float settleTimer;
    private float wrongTimer;

    private void Awake()
    {
        items = new MouseSpringFollower[] { groundsJar, waterBottle };
        grabColliders = new Collider[] { groundsJarGrab, waterBottleGrab };
        spouts = new Transform[] { groundsSpout, waterSpout };
        streams = new Transform[] { groundsStream, waterStream };
        highlights = new GameObject[] { groundsHighlight, waterHighlight };
        mouths = new Collider[] { dripperMouth, kettleMouth };
        contents = new Transform[] { dripperContent, kettleContent };
        rates = new float[] { groundsRate, waterRate };

        startPos = new Vector3[Count];
        startRot = new Quaternion[Count];
        streamRestScale = new Vector3[Count];
        contentRestScale = new Vector3[Count];
        fill = new float[Count];
        for (int i = 0; i < Count; i++)
        {
            startPos[i] = items[i].transform.position;
            startRot[i] = items[i].transform.rotation;
            streamRestScale[i] = streams[i].localScale;
            contentRestScale[i] = contents[i].localScale;
        }
    }

    protected override void ResetGame()
    {
        // 인스펙터에서 바꾼 속도를 판마다 반영
        rates[Grounds] = groundsRate;
        rates[Water] = waterRate;
        for (int i = 0; i < Count; i++)
        {
            items[i].viewCamera = viewCamera;
            items[i].ResetTo(startPos[i], startRot[i]);
            items[i].inputEnabled = true;
            fill[i] = 0f;
            HideStream(i);
            UpdateContent(i);
        }
        heldIndex = -1;
        settleTimer = 0f;
        wrongTimer = 0f;
        hud.Configure(groundsMin01, groundsMax01, waterMin01, waterMax01);
        UpdateHighlights();
    }

    private void Update()
    {
        if (!isPlaying)
        {
            for (int i = 0; i < Count; i++) items[i].inputEnabled = false;
            return;
        }
        // 일시정지 중에는 잡기·붓기·타이머를 멈춘다
        if (Time.timeScale <= 0f) return;
        float dt = Time.deltaTime;

        UpdateGrab();
        UpdateHighlights();

        bool anyPouring = false;
        bool wrongPouring = false;
        for (int i = 0; i < Count; i++)
        {
            if (items[i].ActualTilt() < pourAngle)
            {
                HideStream(i);
                continue;
            }
            anyPouring = true;
            if (PourFrom(i, dt)) wrongPouring = true;
            if (!isPlaying) return;
        }
        hud.SetFills(fill[Grounds], fill[Water]);

        // 엉뚱한 목표: 이어서 부은 시간만 센다(멈추면 0으로)
        if (wrongPouring) wrongTimer += dt;
        else wrongTimer = 0f;
        if (wrongTimer >= wrongSeconds)
        {
            EndGame(false, "거기가 아니야!");
            return;
        }

        if (!anyPouring && BothInTarget()) settleTimer += dt;
        else settleTimer = 0f;
        if (settleTimer >= settleSeconds) EndGame(true, "성공!");
    }

    // 좌클릭을 누르는 동안 처음 고른 물건만 잡는다(끌다가 다른 물건 위를 지나도 바뀌지 않음)
    private void UpdateGrab()
    {
        bool click = ClickHeld();
        if (!click) heldIndex = -1;
        else if (heldIndex < 0) heldIndex = ItemUnderPointer();

        Vector2 pointer = PointerPosition();
        bool right = RightHeld();
        for (int i = 0; i < Count; i++)
        {
            bool held = i == heldIndex;
            items[i].pointerPosition = pointer;
            items[i].grabHeld = held;
            items[i].tiltHeld = held && right;
        }
    }

    // 포인터 아래 가장 가까운 물건 번호. 없으면 -1
    private int ItemUnderPointer()
    {
        Ray ray = viewCamera.ScreenPointToRay(PointerPosition());
        int best = -1;
        float bestDistance = GrabRayLength;
        for (int i = 0; i < Count; i++)
        {
            RaycastHit hit;
            if (grabColliders[i].Raycast(ray, out hit, bestDistance))
            {
                best = i;
                bestDistance = hit.distance;
            }
        }
        return best;
    }

    // 아무것도 안 잡았으면 둘 다, 잡았으면 잡은 것만 강조
    private void UpdateHighlights()
    {
        for (int i = 0; i < Count; i++)
        {
            highlights[i].SetActive(heldIndex < 0 || heldIndex == i);
        }
    }

    // 물건 i가 붓는 중. 맞는 목표 위면 채우고, 엉뚱한 목표 위면 true
    private bool PourFrom(int i, float dt)
    {
        Ray ray = new Ray(spouts[i].position, Vector3.down);
        for (int t = 0; t < Count; t++)
        {
            RaycastHit hit;
            if (!mouths[t].Raycast(ray, out hit, MouthRayLength)) continue;
            SetStream(i, hit.distance);
            if (t != i) return true;
            AddFill(t, rates[i] * dt);
            return false;
        }
        // 목표 밖: 바닥까지 흘러내리기만 하고 판정 없음
        SetStream(i, Mathf.Max(0f, spouts[i].position.y - floor.position.y));
        return false;
    }

    private void AddFill(int t, float amount)
    {
        fill[t] += amount;
        bool overflow = fill[t] > 1f + Epsilon;
        if (overflow) fill[t] = 1f;
        UpdateContent(t);
        if (overflow)
        {
            hud.SetFills(fill[Grounds], fill[Water]);
            EndGame(false, "넘쳤어!");
        }
    }

    private float MinOf(int t)
    {
        return t == Grounds ? groundsMin01 : waterMin01;
    }

    private float MaxOf(int t)
    {
        return t == Grounds ? groundsMax01 : waterMax01;
    }

    private bool InTarget(int t)
    {
        return fill[t] >= MinOf(t) - Epsilon && fill[t] <= MaxOf(t) + Epsilon;
    }

    private bool OverTarget(int t)
    {
        return fill[t] > MaxOf(t) + Epsilon;
    }

    private bool BothInTarget()
    {
        return InTarget(Grounds) && InTarget(Water);
    }

    // 시간 초과. 순서: 하나라도 구간 넘김 → 둘 다 구간 안(붓는 중이어도 성공) → 하나도 안 부음 → 모자람
    public override void TimeOver()
    {
        if (OverTarget(Grounds) || OverTarget(Water)) EndGame(false, "너무 많아!");
        else if (BothInTarget()) EndGame(true, "성공!");
        else if (fill[Grounds] <= 0f && fill[Water] <= 0f) EndGame(false, "시간 초과!");
        else EndGame(false, "모자라!");
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        for (int i = 0; i < Count; i++)
        {
            items[i].inputEnabled = false;
            HideStream(i);
        }
        heldIndex = -1;
        if (success)
        {
            float g = QualityOf(fill[Grounds], groundsMin01, groundsMax01);
            float w = QualityOf(fill[Water], waterMin01, waterMax01);
            quality = (g + w) * 0.5f;
        }
        Finish(success, text);
    }

    private void HideStream(int i)
    {
        SetStream(i, 0f);
    }

    private void SetStream(int i, float length)
    {
        Transform stream = streams[i];
        bool on = length > 0f;
        stream.gameObject.SetActive(on);
        if (!on)
        {
            stream.localScale = streamRestScale[i];
            return;
        }
        // 줄기는 항상 수직 아래로
        stream.SetPositionAndRotation(spouts[i].position, Quaternion.identity);
        stream.localScale = new Vector3(streamWidth, length, streamWidth);
    }

    // 목표 안 내용물 높이 = 프리팹 높이 x 채움 비율
    private void UpdateContent(int t)
    {
        Transform content = contents[t];
        content.gameObject.SetActive(fill[t] > 0f);
        Vector3 s = contentRestScale[t];
        content.localScale = new Vector3(s.x, s.y * fill[t], s.z);
    }
}
