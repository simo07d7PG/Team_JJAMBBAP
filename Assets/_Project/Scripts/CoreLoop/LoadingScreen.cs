using System.Collections;
using TMPro;
using UnityEngine;

// 카페 씬을 시작할 때 덮는 로딩 화면 (가짜 진행 막대). 다 차면 서서히 사라진다
public class LoadingScreen : MonoBehaviour
{
    public CanvasGroup group;
    [Tooltip("진행 막대. 왼쪽 기준(pivot x = 0)으로 가로 크기를 키운다")]
    public RectTransform barFill;
    public TMP_Text dayText;
    [Tooltip("막대 아래 단계 글자 (원두 가는 중… 등)")]
    public TMP_Text stepText;
    public TMP_Text tipText;
    [Tooltip("빙글빙글 도는 고리")]
    public RectTransform spinner;

    [Tooltip("막대가 다 차는 시간(초)")]
    public float seconds = 2.2f;
    [Tooltip("다 찬 뒤 사라지는 시간(초)")]
    public float fadeSeconds = 0.45f;
    [Tooltip("진행에 따라 차례로 보여 줄 단계 글자")]
    public string[] steps;
    [Tooltip("무작위로 하나 보여 줄 팁")]
    public string[] tips;

    // 다 찬 뒤 사라지기 전에 잠깐 멈추는 시간(초)
    private const float HoldSeconds = 0.15f;
    // 고리 회전 속도(도/초)
    private const float SpinSpeed = -300f;
    // 진행 구간 경계(시간 비율)와 그 지점의 막대 진행값
    private const float PhaseOneEnd = 0.35f;
    private const float PhaseTwoEnd = 0.55f;
    // 두 번째 구간 길이(PhaseTwoEnd - PhaseOneEnd). 부동소수 오차를 피하려고 리터럴로 둔다
    private const float PhaseTwoSpan = 0.2f;
    // 세 번째 구간 길이(1 - PhaseTwoEnd)
    private const float PhaseThreeSpan = 0.45f;
    private const float PhaseOneProgress = 0.45f;
    private const float PhaseTwoProgress = 0.5f;

    public IEnumerator Run(int day)
    {
        // 화면을 덮고 날짜·팁 표시
        gameObject.SetActive(true);
        group.alpha = 1f;
        dayText.text = day + "일차";
        tipText.text = tips.Length > 0 ? "TIP  " + tips[Random.Range(0, tips.Length)] : "";

        // 가짜 진행 막대 채우기
        float fillT = 0f;
        while (fillT < 1f)
        {
            fillT = Mathf.Min(1f, fillT + Time.deltaTime / seconds);
            SetProgress(FakeProgress(fillT));
            yield return null;
        }
        yield return new WaitForSeconds(HoldSeconds);

        // 서서히 사라지기
        float fadeT = 0f;
        while (fadeT < 1f)
        {
            fadeT = Mathf.Min(1f, fadeT + Time.deltaTime / fadeSeconds);
            group.alpha = 1f - fadeT;
            Spin();
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // 처음엔 빠르게, 중간에 잠깐 멈칫, 끝에서 마저 찬다
    private float FakeProgress(float t)
    {
        // 빠른 구간
        if (t < PhaseOneEnd) return Mathf.Lerp(0f, PhaseOneProgress, Mathf.SmoothStep(0f, 1f, t / PhaseOneEnd));
        // 멈칫 구간
        if (t < PhaseTwoEnd) return Mathf.Lerp(PhaseOneProgress, PhaseTwoProgress, (t - PhaseOneEnd) / PhaseTwoSpan);
        // 마무리 구간
        return Mathf.Lerp(PhaseTwoProgress, 1f, Mathf.SmoothStep(0f, 1f, (t - PhaseTwoEnd) / PhaseThreeSpan));
    }

    private void SetProgress(float p)
    {
        barFill.localScale = new Vector3(p, 1f, 1f);
        if (p >= 1f) stepText.text = "준비 완료!";
        else if (steps.Length > 0) stepText.text = steps[Mathf.Min(steps.Length - 1, (int)(p * steps.Length))];
        Spin();
    }

    private void Spin()
    {
        spinner.Rotate(0f, 0f, SpinSpeed * Time.deltaTime);
    }
}
