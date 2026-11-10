using UnityEngine;

// "원두 갈아라!" — 클릭 없이 마우스를 손잡이 축 둘레로 시계 방향으로 돌린다. 누적 바퀴 수를 채우면 바로 성공.
// 반시계 방향 움직임은 무시하고, 축에 너무 가까운 포인터는 각도가 튀므로 무시한다. 속도 제한은 없다.
public class ManualGrindMicrogame : Minigame
{
    [Header("참조")]
    [Tooltip("손잡이 회전축. 이 위치가 화면에서 포인터 각도의 기준이 되고, 누적 각도만큼 돈다")]
    [SerializeField] private Transform crankPivot;
    [Tooltip("손잡이 외형(막대 + 구). 한 바퀴마다 색이 깜빡인다")]
    [SerializeField] private Renderer[] crankRenderers;
    [Tooltip("통 안 가루 피벗(바닥에 둔다). localScale.y = 진행 비율에 따른 높이")]
    [SerializeField] private Transform powderPivot;
    [SerializeField] private ManualGrindHud hud;

    [Header("수치")]
    [Tooltip("이만큼 시계 방향으로 돌리면 성공(바퀴)")]
    [SerializeField] private int neededTurns = 6;
    [Tooltip("축에서 이 거리(px) 안쪽의 포인터는 무시한다")]
    [SerializeField] private float deadZonePixels = 40f;
    [Tooltip("등급 = 남은 시간 / 제한 시간 × 이 값 (0~1로 자름)")]
    [SerializeField] private float qualityTimeScale = 2.5f;
    [Tooltip("시간 초과 때 이 비율 이상 갈았으면 \"덜 갈렸어!\"")]
    [SerializeField] private float halfDoneRatio = 0.5f;

    [Header("연출")]
    [Tooltip("손잡이가 도는 로컬 축")]
    [SerializeField] private Vector3 crankLocalAxis = Vector3.forward;
    [Tooltip("화면에서 시계 방향으로 돌게 하는 부호(1 또는 -1). 축이 카메라 시선과 같은 방향이면 -1")]
    [SerializeField] private float spinSign = 1f;
    [Tooltip("가루 높이(localScale.y): 시작 / 다 갈았을 때")]
    [SerializeField] private float powderMinHeight = 0.02f;
    [SerializeField] private float powderMaxHeight = 1f;
    [Tooltip("한 바퀴마다 손잡이가 깜빡이는 색과 시간(초)")]
    [SerializeField] private Color flashColor = new Color(1f, 0.85f, 0.35f);
    [SerializeField] private float flashSeconds = 0.2f;
    [Tooltip("시작 후 \"시계 방향으로 돌려!\" 안내를 보여 주는 시간(초)")]
    [SerializeField] private float hintSeconds = 1.5f;

    private const float DegreesPerTurn = 360f;
    private const string TextSuccess = "성공!";
    private const string TextHalfDone = "덜 갈렸어!";
    private const string TextTimeOver = "시간 초과!";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Quaternion crankRestRot;
    private Vector3 powderRestScale;
    private Color[] crankRestColors;
    private MaterialPropertyBlock block;

    private float accumulated;
    private float elapsed;
    private int turnsDone;
    private float flashTimer;
    // 직전 프레임 포인터 각도. 데드존·일시정지·시작 직후에는 없음(첫 프레임은 기록만 한다)
    private bool hasLastAngle;
    private float lastAngle;

    private void Awake()
    {
        crankRestRot = crankPivot.localRotation;
        powderRestScale = powderPivot.localScale;
        block = new MaterialPropertyBlock();
        crankRestColors = new Color[crankRenderers.Length];
        for (int i = 0; i < crankRenderers.Length; i++)
        {
            crankRestColors[i] = ReadBaseColor(crankRenderers[i].sharedMaterial);
        }
    }

    private Color ReadBaseColor(Material mat)
    {
        if (mat == null) return Color.white;
        if (mat.HasProperty(BaseColorId)) return mat.GetColor(BaseColorId);
        if (mat.HasProperty(ColorId)) return mat.GetColor(ColorId);
        return Color.white;
    }

    protected override void ResetGame()
    {
        accumulated = 0f;
        elapsed = 0f;
        turnsDone = 0;
        flashTimer = 0f;
        hasLastAngle = false;
        ClearFlash();
        UpdateVisuals();
        hud.Configure(neededTurns);
        hud.SetHint(true);
    }

    private void Update()
    {
        UpdateFlash();
        if (!isPlaying) return;

        // 일시정지(timeScale 0) 중에는 돌린 양을 받지 않는다. 풀린 순간 튀지 않게 직전 각도도 버린다
        if (Time.timeScale <= 0f)
        {
            hasLastAngle = false;
            return;
        }

        elapsed += Time.deltaTime;
        hud.SetHint(elapsed < hintSeconds);

        ReadRotation();
        UpdateVisuals();

        if (accumulated >= neededTurns * DegreesPerTurn)
        {
            quality = timeLimit > 0f ? Mathf.Clamp01((timeLimit - elapsed) / timeLimit * qualityTimeScale) : 1f;
            EndGame(true, TextSuccess);
        }
    }

    // 축 기준 포인터 각도의 시계 방향 변화량만 누적한다
    private void ReadRotation()
    {
        Vector3 axis = viewCamera.WorldToScreenPoint(crankPivot.position);
        Vector2 offset = PointerPosition() - new Vector2(axis.x, axis.y);
        if (axis.z <= 0f || offset.magnitude < deadZonePixels)
        {
            hasLastAngle = false;
            return;
        }

        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        if (hasLastAngle)
        {
            // 화면 y가 위로 커지므로 시계 방향은 각도가 줄어드는 쪽. DeltaAngle이 ±180°로 접어 준다
            float delta = -Mathf.DeltaAngle(lastAngle, angle);
            if (delta > 0f) accumulated += delta;
        }
        lastAngle = angle;
        hasLastAngle = true;

        int turns = Mathf.FloorToInt(accumulated / DegreesPerTurn);
        if (turns > turnsDone)
        {
            turnsDone = Mathf.Min(turns, neededTurns);
            flashTimer = flashSeconds;
            hud.PulseTurn();
        }
    }

    private float Progress()
    {
        return Mathf.Clamp01(accumulated / (neededTurns * DegreesPerTurn));
    }

    // 손잡이 회전, 가루 높이, HUD 진행
    private void UpdateVisuals()
    {
        crankPivot.localRotation = crankRestRot * Quaternion.AngleAxis(accumulated * spinSign, crankLocalAxis);
        float p = Progress();
        Vector3 s = powderRestScale;
        s.y = Mathf.Lerp(powderMinHeight, powderMaxHeight, p);
        powderPivot.localScale = s;
        hud.SetProgress(p, turnsDone);
    }

    // 깜빡임은 게임이 끝난 뒤에도 마저 사라지게 한다
    private void UpdateFlash()
    {
        if (flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
        {
            ClearFlash();
            return;
        }
        float k = flashTimer / flashSeconds;
        for (int i = 0; i < crankRenderers.Length; i++)
        {
            Color c = Color.Lerp(crankRestColors[i], flashColor, k);
            crankRenderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            crankRenderers[i].SetPropertyBlock(block);
        }
    }

    private void ClearFlash()
    {
        flashTimer = 0f;
        for (int i = 0; i < crankRenderers.Length; i++)
        {
            crankRenderers[i].SetPropertyBlock(null);
        }
    }

    // 시간 초과. 반 이상 갈았으면 "덜 갈렸어!"
    public override void TimeOver()
    {
        if (accumulated >= neededTurns * DegreesPerTurn * halfDoneRatio) EndGame(false, TextHalfDone);
        else EndGame(false, TextTimeOver);
    }

    private void EndGame(bool success, string text)
    {
        if (!isPlaying) return;
        hud.SetHint(false);
        Finish(success, text);
    }
}
