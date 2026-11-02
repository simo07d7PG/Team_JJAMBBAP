using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 코어 루프 연출 모음. CafeLoop와 같은 오브젝트에 붙인다.
// 페이드, 글자 튕김·흔들림, 떠오르는 글자, 숫자 굴리기, 아래에서 올라오기, 효과음 자리(비우면 무음)
public class CafeEffects : MonoBehaviour
{
    [Header("페이드 (화면 전체 검정 Image)")]
    public Image fadeImage;
    [Tooltip("검정으로 가는 시간. 왕복이면 두 배")]
    public float fadeSeconds = 0.12f;

    [Header("효과음 자리 (비우면 무음)")]
    public AudioSource audioSource;
    [Tooltip("손님 등장")]
    public AudioClip bellClip;
    [Tooltip("기계·손님 클릭")]
    public AudioClip clickClip;
    public AudioClip successClip;
    public AudioClip failClip;
    [Tooltip("결제")]
    public AudioClip coinClip;

    [Header("배경 음악 (비우면 무음)")]
    [Tooltip("loop로 재생할 AudioSource (효과음과 따로)")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)]
    public float bgmVolume = 0.6f;

    // 튕김 연출: 걸리는 시간(초), 1배를 넘치는 정도
    private const float PopSeconds = 0.25f;
    private const float PopOvershoot = 0.08f;

    // 흔들림 연출: 걸리는 시간(초), 사인파 반주기 수(p * PI * 값)
    private const float ShakeSeconds = 0.3f;
    private const float ShakeHalfWaves = 6f;

    // 떠오르는 글자 연출: 지속 시간, 시작 높이 오프셋, 상승 거리(캔버스 단위)
    private const float FloatSeconds = 1f;
    private const float FloatStartOffset = 40f;
    private const float FloatRiseDistance = 90f;
    // 처음 커지는 구간 비율, 시작 크기, 최대 크기, 사라지기 시작하는 진행률
    private const float FloatPopPortion = 0.15f;
    private const float FloatStartScale = 0.6f;
    private const float FloatPeakScale = 1.1f;
    private const float FloatFadeStart = 0.6f;

    // 처음엔 빠르고 끝에서 느려지는 곡선 (2차)
    private static float EaseOutQuad(float p)
    {
        return 1f - (1f - p) * (1f - p);
    }

    // 처음엔 빠르고 끝에서 느려지는 곡선 (3차, 2차보다 더 크게 느려짐)
    private static float EaseOutCubic(float p)
    {
        float q = 1f - p;
        return 1f - q * q * q;
    }

    public void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    // 배경 음악을 seconds 동안 서서히 키우며 시작한다
    public IEnumerator StartBgm(float seconds)
    {
        if (bgmSource == null || bgmClip == null) yield break;
        bgmSource.clip = bgmClip;
        bgmSource.loop = true;
        bgmSource.volume = 0f;
        bgmSource.Play();
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            bgmSource.volume = bgmVolume * Mathf.Clamp01(t / seconds);
            yield return null;
        }
        bgmSource.volume = bgmVolume;
    }

    // 화면을 바로 검게 (씬 시작 때 첫 프레임부터 가리기)
    public void SetBlack()
    {
        fadeImage.gameObject.SetActive(true);
        SetFadeAlpha(1f);
    }

    // 페이드 Image의 알파만 바꾼다
    private void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    // 검정 정도를 target(0~1)까지. 0이 되면 Image를 끈다
    public IEnumerator FadeTo(float target)
    {
        yield return FadeTo(target, fadeSeconds);
    }

    public IEnumerator FadeTo(float target, float seconds)
    {
        fadeImage.gameObject.SetActive(true);
        // 시작 시점의 색(RGB)을 잡아 두고 알파만 바꿔 매 프레임 덮어쓴다
        Color color = fadeImage.color;
        float start = color.a;
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(start, target, t / seconds);
            fadeImage.color = color;
            yield return null;
        }
        color.a = target;
        fadeImage.color = color;
        if (target <= 0f) fadeImage.gameObject.SetActive(false);
    }

    // from배 크기에서 살짝 넘쳤다가 1배로 (짧은 시간)
    public IEnumerator Pop(RectTransform rect, float from)
    {
        float t = 0f;
        while (t < PopSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / PopSeconds);
            // 1배를 조금 지나쳤다가 돌아오는 곡선
            float s = Mathf.Lerp(from, 1f, p) + Mathf.Sin(p * Mathf.PI) * PopOvershoot;
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    // 좌우로 흔들었다가 제자리로 (짧은 시간)
    public IEnumerator Shake(RectTransform rect, float amount)
    {
        Vector2 origin = rect.anchoredPosition;
        float t = 0f;
        while (t < ShakeSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / ShakeSeconds);
            rect.anchoredPosition = origin + new Vector2(Mathf.Sin(p * Mathf.PI * ShakeHalfWaves) * amount * (1f - p), 0f);
            yield return null;
        }
        rect.anchoredPosition = origin;
    }

    // 월드 좌표를 캔버스 로컬 좌표로 바꾼다 (Overlay 캔버스 기준)
    private static Vector2 WorldToCanvasLocal(RectTransform canvasRect, Vector3 worldPos, Camera cam)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, cam.WorldToScreenPoint(worldPos), null, out local);
        return local;
    }

    // 월드 위치 위에서 글자가 떠오르며 사라진다 (FloatSeconds초). text는 캔버스 바로 아래, 가운데 앵커
    public IEnumerator FloatUp(TMP_Text text, string message, Vector3 worldPos, Camera cam)
    {
        RectTransform rect = text.rectTransform;
        RectTransform canvasRect = (RectTransform)rect.parent;
        Vector2 local = WorldToCanvasLocal(canvasRect, worldPos, cam);
        text.text = message;
        text.gameObject.SetActive(true);
        Color color = text.color;
        float t = 0f;
        while (t < FloatSeconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / FloatSeconds);
            rect.anchoredPosition = local + new Vector2(0f, FloatStartOffset + FloatRiseDistance * p);
            float s = p < FloatPopPortion
                ? Mathf.Lerp(FloatStartScale, FloatPeakScale, p / FloatPopPortion)
                : Mathf.Lerp(FloatPeakScale, 1f, (p - FloatPopPortion) / 0.85f);
            rect.localScale = new Vector3(s, s, 1f);
            color.a = 1f - Mathf.Clamp01((p - FloatFadeStart) / 0.4f);
            text.color = color;
            yield return null;
        }
        color.a = 1f;
        text.color = color;
        text.gameObject.SetActive(false);
    }

    // 숫자가 from에서 to까지 굴러 올라간다
    public IEnumerator CountUp(TMP_Text text, string prefix, int from, int to, string suffix, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / seconds);
            // 처음엔 빠르고 끝에서 느려지게
            p = EaseOutQuad(p);
            text.text = prefix + Mathf.RoundToInt(Mathf.Lerp(from, to, p)) + suffix;
            yield return null;
        }
        text.text = prefix + to + suffix;
    }

    // 아래쪽 offset만큼에서 제자리로 올라온다 (끝에서 느려짐)
    public IEnumerator SlideUp(RectTransform rect, float offset, float seconds)
    {
        Vector2 end = rect.anchoredPosition;
        Vector2 start = end - new Vector2(0f, offset);
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / seconds);
            p = EaseOutCubic(p);
            rect.anchoredPosition = Vector2.Lerp(start, end, p);
            yield return null;
        }
        rect.anchoredPosition = end;
    }
}
