using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 갈기 타이밍 화면. 링 위 구간 호(좋음·완벽), 바늘, "n/4", 놓침 칸, 판정 글자. 표시만 한다.
// 각도는 12시 = 0도, 시계 방향이 +
public class GrindTimingHud : MonoBehaviour
{
    [Header("링")]
    [Tooltip("좋음 구간 호. 링 스프라이트(Pointer_Ring 등) 필수: 스프라이트가 없으면 채움이 무시되어 사각형으로 보인다. Type Filled, Radial360은 코드가 맞춘다. 링 중심에 같은 크기로 겹쳐 둔다")]
    [SerializeField] private Image goodArc;
    [Tooltip("완벽 구간 호. 위와 같은 설정(링 스프라이트 필수)")]
    [SerializeField] private Image perfectArc;
    [Tooltip("바늘. 피벗이 링 중심(0.5, 0)인 막대, 위쪽(12시)을 향해 서 있게 둔다")]
    [SerializeField] private RectTransform needle;

    [Header("글자")]
    [Tooltip("\"1/4\"")]
    [SerializeField] private TMP_Text countText;
    [Tooltip("판정 글자(링 아래)")]
    [SerializeField] private TMP_Text judgeText;
    [Tooltip("\"Space\" 안내. 처음 잠깐만 보인다")]
    [SerializeField] private GameObject keyHint;
    [SerializeField] private float keyHintSeconds = 1.5f;

    [Header("놓침 칸")]
    [Tooltip("놓침 칸(왼쪽부터). 놓친 만큼 채운다. 최대 놓침 수보다 많은 칸은 숨긴다")]
    [SerializeField] private Image[] missBoxes;
    [SerializeField] private Color missEmptyColor = MinigameUiPalette.InkDeep;
    [SerializeField] private Color missFilledColor = new Color(0.78f, 0.27f, 0.22f);

    [Header("판정 글자 색·시간")]
    [SerializeField] private Color hitColor = MinigameUiPalette.Gold;
    [SerializeField] private Color missColor = new Color(0.78f, 0.27f, 0.22f);
    [SerializeField] private float judgeSeconds = 0.5f;

    [Header("새 구간 깜빡임")]
    [SerializeField] private float blinkSeconds = 0.15f;
    [Tooltip("깜빡임 한 번의 길이(초)")]
    [SerializeField] private float blinkStep = 0.05f;

    private const string CountFormat = "{0}/{1}";
    private const float FullTurn = 360f;

    private bool ready;
    private float hintTimer;
    private float judgeTimer;
    private Color judgeBaseColor;
    private float blinkTimer;
    private Color goodBase;
    private Color perfectBase;

    private void Awake()
    {
        Prepare();
    }

    // 프리팹이 꺼져 있다 켜지는 순서와 상관없이 한 번만 준비한다
    private void Prepare()
    {
        if (ready) return;
        SetupArc(goodArc);
        SetupArc(perfectArc);
        goodBase = goodArc.color;
        perfectBase = perfectArc.color;
        ready = true;
    }

    // 호가 12시에서 시계 방향으로 차오르게 맞춘다
    private static void SetupArc(Image arc)
    {
        arc.type = Image.Type.Filled;
        arc.fillMethod = Image.FillMethod.Radial360;
        arc.fillOrigin = (int)Image.Origin360.Top;
        arc.fillClockwise = true;
    }

    public void Configure(int needed, int maxMisses)
    {
        Prepare();
        hintTimer = keyHintSeconds;
        if (keyHint != null) keyHint.SetActive(true);
        judgeTimer = 0f;
        judgeText.gameObject.SetActive(false);
        blinkTimer = 0f;
        goodArc.color = goodBase;
        perfectArc.color = perfectBase;
        for (int i = 0; i < missBoxes.Length; i++)
        {
            missBoxes[i].gameObject.SetActive(i < maxMisses);
        }
        SetMisses(0);
        SetHits(0, needed);
    }

    // 좋음 호와 완벽 호를 놓는다(시작 각도, 폭). 호는 시작 각도만큼 시계 방향으로 돌려 둔다
    public void ShowZone(float goodStart, float goodWidth, float perfectStart, float perfectWidth)
    {
        Prepare();
        PlaceArc(goodArc, goodStart, goodWidth);
        PlaceArc(perfectArc, perfectStart, perfectWidth);
        blinkTimer = blinkSeconds;
    }

    private static void PlaceArc(Image arc, float startAngle, float width)
    {
        arc.fillAmount = Mathf.Clamp01(width / FullTurn);
        arc.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -startAngle);
    }

    public void SetNeedle(float angle)
    {
        needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    public void SetHits(int hits, int needed)
    {
        countText.SetText(CountFormat, hits, needed);
    }

    public void SetMisses(int misses)
    {
        for (int i = 0; i < missBoxes.Length; i++)
        {
            missBoxes[i].color = i < misses ? missFilledColor : missEmptyColor;
        }
    }

    public void ShowJudge(string text, bool hit)
    {
        judgeBaseColor = hit ? hitColor : missColor;
        judgeText.SetText(text);
        judgeText.color = judgeBaseColor;
        judgeText.gameObject.SetActive(true);
        judgeTimer = judgeSeconds;
    }

    private void Update()
    {
        if (!ready) return;
        float dt = Time.deltaTime;

        if (hintTimer > 0f)
        {
            hintTimer -= dt;
            if (hintTimer <= 0f && keyHint != null) keyHint.SetActive(false);
        }

        if (judgeTimer > 0f)
        {
            judgeTimer -= dt;
            if (judgeTimer <= 0f)
            {
                judgeText.gameObject.SetActive(false);
            }
            else
            {
                // 끝으로 갈수록 흐려진다
                Color c = judgeBaseColor;
                c.a = judgeBaseColor.a * (judgeTimer / judgeSeconds);
                judgeText.color = c;
            }
        }

        if (blinkTimer > 0f)
        {
            blinkTimer -= dt;
            bool on = blinkTimer <= 0f || ((int)(blinkTimer / blinkStep) % 2 == 0);
            goodArc.color = on ? goodBase : Color.clear;
            perfectArc.color = on ? perfectBase : Color.clear;
        }
    }
}
