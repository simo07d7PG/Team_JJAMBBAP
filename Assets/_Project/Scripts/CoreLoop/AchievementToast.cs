using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 업적 달성 알림. 화면 오른쪽 위에서 미끄러져 들어와 잠깐 보이고 나간다. 여러 개면 차례로
public class AchievementToast : MonoBehaviour
{
    private const float SlideSeconds = 0.35f;
    private const float ShowSeconds = 2.5f;
    private const float Width = 380f;
    private const float Height = 96f;
    private const float RestX = -24f;
    private const float PosY = -110f;
    private static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.85f);
    private static readonly Color BorderColor = new Color(1f, 0.82f, 0.4f, 0.9f);
    private static readonly Color SmallColor = new Color(1f, 0.82f, 0.4f);

    private readonly Queue<Achievements.Def> queue = new Queue<Achievements.Def>();
    private RectTransform panel;
    private TMP_Text smallText;
    private TMP_Text titleText;
    private bool running;

    public void Init(CafeLoop loop)
    {
        if (loop.dayText == null) return;
        // 비활성 오브젝트의 canvas는 null일 수 있어, 항상 켜져 있는 dayText의 루트 캔버스를 쓴다
        Transform parent = loop.dayText.canvas.rootCanvas.transform;
        TMP_FontAsset font = loop.dayText.font;

        GameObject go = new GameObject("AchievementToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        panel = go.GetComponent<RectTransform>();
        panel.SetParent(parent, false);
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.sizeDelta = new Vector2(Width, Height);
        panel.anchoredPosition = new Vector2(RestX, PosY);
        UImage img = go.GetComponent<UImage>();
        img.color = PanelColor;
        img.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        smallText = MakeText("Small", font, 24f, SmallColor, new Vector2(0f, -10f), 30f);
        titleText = MakeText("Title", font, 38f, Color.white, new Vector2(0f, -42f), 46f);
        group = go.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        this.loop = loop;
        go.SetActive(false);
        Achievements.OnUnlocked -= Enqueue; // 중복 구독 방지
        Achievements.OnUnlocked += Enqueue;
    }

    private CafeLoop loop;
    private CanvasGroup group;

    // 일시정지(ESC 메뉴·설명 카드)나 영수증이 떠 있으면 알림을 숨기고 표시 시간도 멈춘다
    private bool Suppressed()
    {
        return Time.timeScale <= 0f || (loop != null && loop.receiptPanel != null && loop.receiptPanel.activeInHierarchy);
    }

    private void Update()
    {
        if (group != null) group.alpha = Suppressed() ? 0f : 1f;
    }

    private TMP_Text MakeText(string name, TMP_FontAsset font, float size, Color color, Vector2 pos, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(panel, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(-30f, height);
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Left;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }

    private void OnDestroy()
    {
        Achievements.OnUnlocked -= Enqueue;
    }

    private void Enqueue(Achievements.Def def)
    {
        queue.Enqueue(def);
        if (!running && panel != null) StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        panel.gameObject.SetActive(true);
        while (queue.Count > 0)
        {
            Achievements.Def def = queue.Dequeue();
            smallText.text = "★ 업적 달성";
            titleText.text = def.title;
            MinigameSfx.Play("sfx_greet", 0.5f);
            yield return Slide(Width + 40f, RestX);
            float t = 0f;
            while (t < ShowSeconds)
            {
                if (!Suppressed()) t += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return Slide(RestX, Width + 40f);
        }
        panel.gameObject.SetActive(false);
        running = false;
    }

    private void OnDisable()
    {
        // 비활성화되면 코루틴이 멈추므로 상태를 정리한다
        running = false;
        queue.Clear();
        if (panel != null) panel.gameObject.SetActive(false);
    }

    // 오른쪽 가장자리 기준 x 위치를 부드럽게 옮긴다 (오른쪽 밖에서 들어오고 나감)
    private IEnumerator Slide(float fromX, float toX)
    {
        float t = 0f;
        while (t < SlideSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / SlideSeconds);
            k = 1f - (1f - k) * (1f - k);
            panel.anchoredPosition = new Vector2(Mathf.Lerp(fromX, toX, k), PosY);
            yield return null;
        }
        panel.anchoredPosition = new Vector2(toX, PosY);
    }
}
