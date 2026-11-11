using UnityEngine;

// "하트를 그려라!" — 위에서 내려다본 컵 위로 마우스 = 우유 줄기 위치, 좌클릭 누름 = 붓기.
// 누른 채 체크포인트를 순서대로 지나면 채워진다(한 점 건너뛰기 허용). 붓는 양은 누른 시간 누적으로 제한.
// 판정은 채운 점 개수뿐이라 컵 수치는 바꾸지 않는다. 주문 변형 없음
public class LatteArtMicrogame : Minigame
{
    [Header("참조")]
    [SerializeField] private CupVisual cupVisual;
    [SerializeField] private LatteArtHud hud;
    [Tooltip("컵 표면 기준 트랜스폼. 위치 = 표면 중심, up = 표면 법선, right = 도안의 오른쪽, forward = 도안의 위쪽(화면 위)")]
    [SerializeField] private Transform surface;
    [Tooltip("붓 끝 표시(작은 구·캡슐). 표면 위로 brushHeight만큼 떠서 따라다닌다")]
    [SerializeField] private Transform brushTip;
    [Tooltip("우유 줄기 피벗(원기둥). localScale.y = 길이, 위쪽이 원점. 누르는 동안만 켠다")]
    [SerializeField] private Transform pourStream;
    [Tooltip("체크포인트 표시 원본(작은 구). 도안 점 수만큼 복제해서 쓴다")]
    [SerializeField] private GameObject markerTemplate;
    [Tooltip("그린 자국 원본(납작한 구). trailPoolSize개를 복제해서 쓴다")]
    [SerializeField] private GameObject trailTemplate;
    [Tooltip("복제본을 모아 둘 부모(비우면 표면)")]
    [SerializeField] private Transform spawnRoot;

    [Header("도안")]
    [Tooltip("체크포인트(순서대로). 표면 지름을 1로 보고 가운데가 (0,0), x = 오른쪽, y = 위. 기본은 하트(아래 꼭짓점 > 오른쪽 윗볼 > 가운데 홈 > 왼쪽 윗볼)")]
    [SerializeField] private Vector2[] checkpoints = new Vector2[]
    {
        new Vector2(0f, -0.38f),
        new Vector2(0.19f, -0.20f),
        new Vector2(0.36f, 0.02f),
        new Vector2(0.34f, 0.28f),
        new Vector2(0.15f, 0.32f),
        new Vector2(0f, 0.18f),
        new Vector2(-0.15f, 0.32f),
        new Vector2(-0.34f, 0.28f),
        new Vector2(-0.36f, 0.02f),
        new Vector2(-0.19f, -0.20f),
    };
    [Tooltip("체크포인트 인정 반경(표면 지름 = 1 기준)")]
    [SerializeField] private float hitRadius = 0.09f;
    [Tooltip("성공에 필요한 채운 점 개수")]
    [SerializeField] private int needCount = 7;

    [Header("손맛")]
    [Tooltip("표면 지름(m). 마우스 위치와 도안 좌표를 맞추는 기준")]
    [SerializeField] private float surfaceDiameter = 0.16f;
    [Tooltip("붓 끝이 마우스를 따라오는 데 걸리는 시간(초). 작을수록 가볍다")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [Tooltip("누르고 있을 수 있는 누적 시간(초)")]
    [SerializeField] private float inkSeconds = 5.5f;
    [Tooltip("붓 끝이 표면에서 뜨는 높이(m)")]
    [SerializeField] private float brushHeight = 0.05f;

    [Header("연출")]
    [Tooltip("자국을 하나 찍는 이동 거리(표면 지름 = 1 기준)")]
    [SerializeField] private float trailSpacing = 0.02f;
    [SerializeField] private int trailPoolSize = 150;
    [Tooltip("자국 지름(m)")]
    [SerializeField] private float trailSize = 0.012f;
    [Tooltip("표시물이 표면 위로 뜨는 높이(m). 겹침 깜빡임 방지")]
    [SerializeField] private float markerLift = 0.002f;
    [Tooltip("체크포인트 지름(m)")]
    [SerializeField] private float markerSize = 0.014f;
    [Tooltip("다음 점이 깜빡이며 커지는 비율과 속도")]
    [SerializeField] private float pulseAmount = 0.35f;
    [SerializeField] private float pulseSpeed = 8f;
    [Tooltip("점 색: 대기 / 다음 / 채움")]
    [SerializeField] private Color waitingColor = new Color(1f, 1f, 1f, 0.35f);
    [SerializeField] private Color nextColor = new Color(0.98f, 0.84f, 0.44f, 1f);
    [SerializeField] private Color filledColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("라떼 아트 중 컵 액체 높이(0~1 고정). 앞 단계 양과 무관하게 같게 보여 surface 높이를 프리팹에서 한 번만 맞추면 된다")]
    [Range(0f, 1f)]
    [SerializeField] private float artFill = 0.85f;

    // 성공 등급: 채운 비율이 이 범위일 때 0~1
    private const float QualityRatioMin = 0.5f;
    private const float QualityRatioMax = 0.9f;
    // 표면 원판 반지름(지름 = 1 기준). 붓 끝이 이 밖으로 못 나간다
    private const float SurfaceRadius01 = 0.5f;
    // 자국은 점보다 얇게 납작
    private const float TrailFlatten = 0.25f;
    private const float TrailLiftRatio = 0.5f;
    private const float Epsilon = 0.0001f;

    private const string TextPerfect = "예쁜 하트!";
    private const string TextSuccess = "성공!";
    private const string TextBad = "하트가 일그러졌어!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Transform[] markers;
    private Renderer[] markerRenderers;
    private Transform[] trails;
    private bool[] filledFlags;
    private MaterialPropertyBlock block;
    private int trailIndex;
    private int nextIndex;
    private int filled;
    private float inkLeft;
    private bool tipReady;
    private bool wasPouring;
    private Vector2 tip;
    private Vector2 tipVelocity;
    private Vector2 lastPour;
    private float trailCarry;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        Transform root = spawnRoot != null ? spawnRoot : surface;

        markerTemplate.SetActive(false);
        markers = new Transform[checkpoints.Length];
        markerRenderers = new Renderer[checkpoints.Length];
        filledFlags = new bool[checkpoints.Length];
        for (int i = 0; i < checkpoints.Length; i++)
        {
            GameObject m = Instantiate(markerTemplate, root);
            m.SetActive(true);
            markers[i] = m.transform;
            markerRenderers[i] = m.GetComponent<Renderer>();
        }

        trailTemplate.SetActive(false);
        trails = new Transform[trailPoolSize];
        for (int i = 0; i < trailPoolSize; i++)
        {
            GameObject t = Instantiate(trailTemplate, root);
            trails[i] = t.transform;
        }
    }

    protected override void ResetGame()
    {
        nextIndex = 0;
        filled = 0;
        inkLeft = inkSeconds;
        tipReady = false;
        wasPouring = false;
        tipVelocity = Vector2.zero;
        trailCarry = 0f;
        trailIndex = 0;
        for (int i = 0; i < filledFlags.Length; i++) filledFlags[i] = false;
        for (int i = 0; i < trails.Length; i++) trails[i].gameObject.SetActive(false);

        for (int i = 0; i < markers.Length; i++)
        {
            markers[i].position = SurfaceToWorld(checkpoints[i], markerLift);
        }
        RefreshMarkers();

        pourStream.gameObject.SetActive(false);
        cupVisual.SetIceVisible(cup.ice > 0);
        cupVisual.SetFill(artFill);
        cupVisual.SetColorFrom(cup, 0f, 0f);
        brushTip.position = SurfaceToWorld(Vector2.zero, brushHeight);
        hud.SetState(1f, 0, checkpoints.Length);
    }

    private void Update()
    {
        if (!isPlaying) return;
        // 일시정지(timeScale 0) 중에는 입력을 받지 않는다
        if (Time.timeScale <= 0f) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector2 target;
        if (PointerOnSurface(out target))
        {
            if (!tipReady)
            {
                tip = target;
                tipVelocity = Vector2.zero;
                tipReady = true;
            }
            else
            {
                tip = Vector2.SmoothDamp(tip, target, ref tipVelocity, followSmoothTime, Mathf.Infinity, dt);
            }
        }
        if (!tipReady) return;
        tip = Vector2.ClampMagnitude(tip, SurfaceRadius01);

        bool pouring = ClickHeld() && inkLeft > 0f;
        if (pouring)
        {
            Vector2 from = wasPouring ? lastPour : tip;
            inkLeft = Mathf.Max(0f, inkLeft - dt);
            DropTrail(from, tip, wasPouring);
            CheckHits(from, tip);
        }
        wasPouring = pouring;
        lastPour = tip;

        ShowBrush(pouring);
        RefreshMarkers();
        hud.SetState(inkLeft / inkSeconds, filled, checkpoints.Length);

        // 더 채울 점이 없으면 바로 판정
        if (nextIndex >= checkpoints.Length) Judge();
    }

    // 마우스 레이를 표면 평면에 투영해 도안 좌표(지름 = 1)로 바꾼다. 해상도·카메라 위치와 무관
    private bool PointerOnSurface(out Vector2 point)
    {
        point = Vector2.zero;
        Ray ray = viewCamera.ScreenPointToRay(PointerPosition());
        Plane plane = new Plane(surface.up, surface.position);
        float enter;
        if (!plane.Raycast(ray, out enter)) return false;
        Vector3 offset = ray.GetPoint(enter) - surface.position;
        point = new Vector2(Vector3.Dot(offset, surface.right), Vector3.Dot(offset, surface.forward)) / surfaceDiameter;
        return true;
    }

    private Vector3 SurfaceToWorld(Vector2 p, float lift)
    {
        return surface.position + surface.right * (p.x * surfaceDiameter) + surface.forward * (p.y * surfaceDiameter) + surface.up * lift;
    }

    // 이번 프레임에 붓 끝이 지난 선분이 다음(또는 한 점 뒤) 체크포인트 반경 안을 지났는지 본다
    private void CheckHits(Vector2 from, Vector2 to)
    {
        if (nextIndex >= checkpoints.Length) return;
        if (SegmentDistance(checkpoints[nextIndex], from, to) <= hitRadius)
        {
            filledFlags[nextIndex] = true;
            filled++;
            nextIndex++;
            return;
        }
        int skip = nextIndex + 1;
        if (skip < checkpoints.Length && SegmentDistance(checkpoints[skip], from, to) <= hitRadius)
        {
            // 한 점은 놓친 것으로 치고 다음 점부터 계속
            filledFlags[skip] = true;
            filled++;
            nextIndex = skip + 1;
        }
    }

    // 점 p와 선분 a-b 사이 거리
    private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 <= Epsilon * Epsilon) return Vector2.Distance(p, a);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
        return Vector2.Distance(p, a + ab * t);
    }

    // 지난 구간을 따라 자국을 일정 간격으로 찍는다. 붓기를 막 시작한 프레임은 시작점에 하나 찍는다
    private void DropTrail(Vector2 from, Vector2 to, bool continuing)
    {
        if (!continuing)
        {
            PlaceTrail(to);
            trailCarry = 0f;
            return;
        }
        float dist = Vector2.Distance(from, to);
        float pos = trailSpacing - trailCarry;
        while (pos <= dist)
        {
            PlaceTrail(Vector2.Lerp(from, to, pos / dist));
            pos += trailSpacing;
        }
        trailCarry = dist - (pos - trailSpacing);
    }

    private void PlaceTrail(Vector2 p)
    {
        Transform t = trails[trailIndex];
        trailIndex = (trailIndex + 1) % trails.Length;
        t.SetPositionAndRotation(SurfaceToWorld(p, markerLift * TrailLiftRatio), surface.rotation);
        t.localScale = new Vector3(trailSize, trailSize * TrailFlatten, trailSize);
        t.gameObject.SetActive(true);
    }

    private void ShowBrush(bool pouring)
    {
        brushTip.position = SurfaceToWorld(tip, brushHeight);
        pourStream.gameObject.SetActive(pouring);
        if (!pouring) return;
        pourStream.SetPositionAndRotation(brushTip.position, surface.rotation);
        Vector3 s = pourStream.localScale;
        pourStream.localScale = new Vector3(s.x, brushHeight, s.z);
    }

    // 점 색: 채움 / 다음(깜빡임) / 대기. 건너뛴 점은 대기색 그대로 둔다
    private void RefreshMarkers()
    {
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        for (int i = 0; i < markers.Length; i++)
        {
            Color c = waitingColor;
            float size = markerSize;
            if (filledFlags[i])
            {
                c = filledColor;
            }
            else if (i == nextIndex)
            {
                c = nextColor;
                size = markerSize * pulse;
            }
            markers[i].localScale = Vector3.one * size;
            SetColor(markerRenderers[i], c);
        }
    }

    private void SetColor(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        r.SetPropertyBlock(block);
    }

    // 채운 점 개수로 결과를 낸다. 전부 채우면 즉시 성공
    private void Judge()
    {
        if (filled >= checkpoints.Length) EndGame(true, TextPerfect, 1f);
        else if (filled >= needCount) EndGame(true, TextSuccess, GradeOf(filled));
        else if (filled > 0) EndGame(false, TextBad, 0f);
        else EndGame(false, TextTimeOver, 0f);
    }

    public override void TimeOver()
    {
        Judge();
    }

    private float GradeOf(int count)
    {
        float ratio = count / (float)checkpoints.Length;
        return Mathf.InverseLerp(QualityRatioMin, QualityRatioMax, ratio);
    }

    private void EndGame(bool success, string text, float grade)
    {
        if (!isPlaying) return;
        pourStream.gameObject.SetActive(false);
        quality = grade;
        Finish(success, text);
    }
}
