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

    public IEnumerator Run(int day)
    {
        gameObject.SetActive(true);
        group.alpha = 1f;
        dayText.text = day + "일차";
        tipText.text = "";
        if (tips.Length > 0) tipText.text = "TIP  " + tips[Random.Range(0, tips.Length)];

        float t = 0f;
        while (t < 1f)
        {
            t = Mathf.Min(1f, t + Time.deltaTime / seconds);
            SetProgress(FakeProgress(t));
            yield return null;
        }
        yield return new WaitForSeconds(0.15f);

        float f = 0f;
        while (f < 1f)
        {
            f = Mathf.Min(1f, f + Time.deltaTime / fadeSeconds);
            group.alpha = 1f - f;
            Spin();
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // 처음엔 빠르게, 중간에 잠깐 멈칫, 끝에서 마저 찬다
    private float FakeProgress(float t)
    {
        if (t < 0.35f) return Mathf.Lerp(0f, 0.45f, Mathf.SmoothStep(0f, 1f, t / 0.35f));
        if (t < 0.55f) return Mathf.Lerp(0.45f, 0.5f, (t - 0.35f) / 0.2f);
        return Mathf.Lerp(0.5f, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f));
    }

    private void SetProgress(float p)
    {
        barFill.localScale = new Vector3(p, 1f, 1f);
        if (steps.Length > 0) stepText.text = steps[Mathf.Min(steps.Length - 1, (int)(p * steps.Length))];
        if (p >= 1f) stepText.text = "준비 완료!";
        Spin();
    }

    private void Spin()
    {
        spinner.Rotate(0f, 0f, -300f * Time.deltaTime);
    }
}
