using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 첫 판매를 끝내기 전까지 화면 위쪽에 지금 할 일을 한 줄로 알려 주는 배너. 런타임에 만든다
public class CafeTutorial : MonoBehaviour
{
    public const string DoneKey = "bari_tutorial_done";
    private const float AnimSeconds = 0.2f;
    private static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.78f);
    private static readonly Color BorderColor = new Color(0.85f, 0.62f, 0.32f, 0.85f);

    private CafeLoop loop;
    private RectTransform banner;
    private CanvasGroup group;
    private TMP_Text label;
    private int shownPhase = -1;
    private float animTimer = AnimSeconds;
    private bool done;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
        done = PlayerPrefs.GetInt(DoneKey, 0) != 0;
        if (done || loop.instructionText == null) return;

        Transform parent = loop.dayText.canvas.rootCanvas.transform;
        GameObject go = new GameObject("TutorialBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage), typeof(CanvasGroup));
        banner = go.GetComponent<RectTransform>();
        banner.SetParent(parent, false);
        banner.anchorMin = new Vector2(0.5f, 1f);
        banner.anchorMax = new Vector2(0.5f, 1f);
        banner.pivot = new Vector2(0.5f, 1f);
        banner.anchoredPosition = new Vector2(0f, -150f);
        banner.sizeDelta = new Vector2(1000f, 68f);
        UImage img = go.GetComponent<UImage>();
        if (loop.receiptBox != null)
        {
            UImage src = loop.receiptBox.GetComponent<UImage>();
            if (src != null)
            {
                img.sprite = src.sprite;
                img.type = src.sprite != null && src.sprite.border.sqrMagnitude > 0f ? UImage.Type.Sliced : UImage.Type.Simple;
            }
        }
        img.color = PanelColor;
        img.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform tr = tg.GetComponent<RectTransform>();
        tr.SetParent(banner, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(24f, 4f);
        tr.offsetMax = new Vector2(-24f, -4f);
        label = tg.GetComponent<TextMeshProUGUI>();
        label.font = loop.instructionText.font;
        label.fontSize = 30f;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.color = Color.white;
        label.raycastTarget = false;
        go.SetActive(false);
    }

    private void Update()
    {
        if (done || banner == null) return;
        int phase = loop.TutorialPhase();
        if (phase != shownPhase)
        {
            shownPhase = phase;
            if (phase == 0)
            {
                banner.gameObject.SetActive(false);
            }
            else
            {
                label.text = TextOf(phase);
                banner.gameObject.SetActive(true);
                banner.SetAsLastSibling();
                animTimer = 0f;
            }
        }
        if (phase == 0) return;
        // 등장: 0.2초 동안 페이드 + 살짝 튕김
        animTimer += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(animTimer / AnimSeconds);
        group.alpha = t;
        float pop = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;
        banner.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, t) * pop;
    }

    private static string TextOf(int phase)
    {
        switch (phase)
        {
            case 1: return "손님을 클릭해서 주문을 받으세요";
            case 2: return "화살표가 가리키는 기계를 클릭하세요 · 순서대로 만들어요";
            default: return "완성! 손님을 클릭해서 건네세요";
        }
    }

    // 첫 판매가 성공했다. 다시는 띄우지 않는다
    public void OnServed()
    {
        if (done) return;
        done = true;
        PlayerPrefs.SetInt(DoneKey, 1);
        PlayerPrefs.Save();
        if (banner != null) banner.gameObject.SetActive(false);
    }
}
