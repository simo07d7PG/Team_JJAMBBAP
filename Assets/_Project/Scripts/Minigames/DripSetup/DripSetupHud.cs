using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 원두·물 넣기 화면. 드리퍼 옆 가루 게이지, 주전자 옆 물 게이지(세로, 목표 띠 + 퍼센트)와
// 물건·목표를 따라다니는 색 꼬리표, 목표 화살표, 상단 할 일, 경고·안내 문구를 표시한다. 표시만 한다.
// 꼬리표·문구는 프리팹에 없고 런타임에 HUD 캔버스 아래 오버레이로 만든다(캔버스를 못 찾으면 모두 건너뜀).
public class DripSetupHud : MonoBehaviour
{
    // 가루(갈색)·물(파랑) 짝 색
    public static readonly Color GroundsColor = new Color(0.72f, 0.48f, 0.28f, 1f);
    public static readonly Color WaterColor = new Color(0.35f, 0.65f, 0.95f, 1f);

    [Header("가루 게이지 (드리퍼 옆)")]
    [Tooltip("가루 높이. anchorMax.y = 채움 비율")]
    [SerializeField] private RectTransform groundsFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform groundsBand;
    [SerializeField] private TMP_Text groundsPercentText;

    [Header("물 게이지 (주전자 옆)")]
    [Tooltip("물 높이. anchorMax.y = 채움 비율")]
    [SerializeField] private RectTransform waterFill;
    [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
    [SerializeField] private RectTransform waterBand;
    [SerializeField] private TMP_Text waterPercentText;

    private class Tag
    {
        public RectTransform rt;
        public CanvasGroup group;
        public Image bg;
        public TMP_Text text;
    }

    // 꼬리표를 물건·목표 위로 띄우는 높이(캔버스 단위)
    private const float TagLift = 45f;
    private const float ArrowLift = 120f;
    private const float AnnounceSeconds = 1.3f;
    private const float DimAlpha = 0.35f;

    private RectTransform overlay;
    private TMP_FontAsset font;
    private Tag[] itemTags;
    private Tag[] targetTags;
    private TMP_Text[] arrows;
    private Tag taskTag;
    private Tag warningTag;
    private Tag announceTag;
    private float announceTimer;

    public void Configure(float groundsMin01, float groundsMax01, float waterMin01, float waterMax01)
    {
        SetBand(groundsBand, groundsMin01, groundsMax01);
        SetBand(waterBand, waterMin01, waterMax01);
        SetFills(0f, 0f);
        TintGauge(groundsPercentText, GroundsColor);
        TintGauge(waterPercentText, WaterColor);
        ResetOverlay();
    }

    public void SetFills(float grounds01, float water01)
    {
        SetFill(groundsFill, groundsPercentText, grounds01);
        SetFill(waterFill, waterPercentText, water01);
    }

    // 물건 i(0 가루, 1 물) 위 꼬리표. screen = 물건 윗점의 화면 좌표, dim = 차례가 아니라 흐리게
    public void SetItemTag(int i, Vector2 screen, bool dim)
    {
        if (!EnsureOverlay()) return;
        Place(itemTags[i], screen, TagLift, dim);
    }

    // 목표 t(0 드리퍼, 1 주전자) 위 '여기에' 꼬리표
    public void SetTargetTag(int t, Vector2 screen, bool dim)
    {
        if (!EnsureOverlay()) return;
        Place(targetTags[t], screen, TagLift, dim);
    }

    // 잡은 물건의 짝 목표 위 화살표. t < 0 이면 숨김
    public void SetArrow(int t, Vector2 screen)
    {
        if (!EnsureOverlay()) return;
        for (int i = 0; i < arrows.Length; i++)
        {
            bool on = i == t;
            if (arrows[i].gameObject.activeSelf != on) arrows[i].gameObject.SetActive(on);
        }
        if (t < 0) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out local)) return;
        float bounce = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 28f;
        arrows[t].rectTransform.anchoredPosition = local + new Vector2(0f, TagLift + ArrowLift + bounce);
    }

    // 화면 상단 현재 할 일
    public void SetTask(string text, Color color)
    {
        if (!EnsureOverlay()) return;
        taskTag.text.SetText(text);
        taskTag.bg.color = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 0.9f);
        taskTag.rt.gameObject.SetActive(true);
    }

    // 경고 문구. 비우면 숨김
    public void SetWarning(string text)
    {
        if (!EnsureOverlay()) return;
        bool on = !string.IsNullOrEmpty(text);
        if (on) warningTag.text.SetText(text);
        if (warningTag.rt.gameObject.activeSelf != on) warningTag.rt.gameObject.SetActive(on);
    }

    // 잠깐 뜨는 큰 안내('가루 완료!')
    public void Announce(string text)
    {
        if (!EnsureOverlay()) return;
        announceTag.text.SetText(text);
        announceTag.rt.gameObject.SetActive(true);
        announceTimer = AnnounceSeconds;
    }

    private void Update()
    {
        if (announceTimer <= 0f) return;
        announceTimer -= Time.deltaTime;
        if (announceTimer <= 0f && announceTag != null) announceTag.rt.gameObject.SetActive(false);
    }

    // 판이 시작될 때 오버레이를 비운다
    private void ResetOverlay()
    {
        if (!EnsureOverlay()) return;
        for (int i = 0; i < itemTags.Length; i++)
        {
            itemTags[i].rt.gameObject.SetActive(false);
            targetTags[i].rt.gameObject.SetActive(false);
            arrows[i].gameObject.SetActive(false);
        }
        taskTag.rt.gameObject.SetActive(false);
        warningTag.rt.gameObject.SetActive(false);
        announceTag.rt.gameObject.SetActive(false);
        announceTimer = 0f;
    }

    // 게이지 패널(이름이 Panel로 끝나는 부모) 안의 글자를 짝 색으로. 패널을 못 찾으면 퍼센트 글자만
    private void TintGauge(TMP_Text percent, Color color)
    {
        if (percent == null) return;
        Transform panel = percent.transform;
        while (panel != null && !panel.name.EndsWith("Panel")) panel = panel.parent;
        if (panel == null)
        {
            percent.color = color;
            return;
        }
        TMP_Text[] texts = panel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++) texts[i].color = color;
    }

    private bool EnsureOverlay()
    {
        if (overlay != null) return true;
        Canvas canvas = null;
        if (groundsPercentText != null) canvas = groundsPercentText.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return false;
        canvas = canvas.rootCanvas;
        font = groundsPercentText != null ? groundsPercentText.font : null;

        GameObject go = new GameObject("PairOverlay", typeof(RectTransform));
        overlay = go.GetComponent<RectTransform>();
        overlay.SetParent(canvas.transform, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        itemTags = new Tag[]
        {
            MakeTag("ItemTagGrounds", "가루", GroundsColor, Color.white, new Vector2(130f, 56f), 38f),
            MakeTag("ItemTagWater", "물", WaterColor, Color.white, new Vector2(110f, 56f), 38f),
        };
        targetTags = new Tag[]
        {
            MakeTag("TargetTagDripper", "여기에", GroundsColor, Color.white, new Vector2(160f, 50f), 34f),
            MakeTag("TargetTagKettle", "여기에", WaterColor, Color.white, new Vector2(160f, 50f), 34f),
        };
        arrows = new TMP_Text[]
        {
            MakeText("ArrowGrounds", "↓", GroundsColor, 110f, new Vector2(120f, 120f)),
            MakeText("ArrowWater", "↓", WaterColor, 110f, new Vector2(120f, 120f)),
        };
        for (int i = 0; i < arrows.Length; i++) arrows[i].gameObject.SetActive(false);
        taskTag = MakeTag("TaskTag", "", GroundsColor, Color.white, new Vector2(900f, 80f), 48f);
        SetAnchoredTop(taskTag.rt, -70f);
        warningTag = MakeTag("WarningTag", "", new Color(0.85f, 0.2f, 0.2f, 1f), Color.white, new Vector2(800f, 76f), 46f);
        SetAnchoredTop(warningTag.rt, -170f);
        announceTag = MakeTag("AnnounceTag", "", new Color(0.25f, 0.65f, 0.35f, 1f), Color.white, new Vector2(700f, 130f), 72f);
        announceTag.rt.anchorMin = announceTag.rt.anchorMax = new Vector2(0.5f, 0.5f);
        announceTag.rt.anchoredPosition = new Vector2(0f, 200f);
        return true;
    }

    private void SetAnchoredTop(RectTransform rt, float y)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private Tag MakeTag(string name, string label, Color bgColor, Color textColor, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        Tag tag = new Tag();
        tag.rt = go.GetComponent<RectTransform>();
        tag.rt.SetParent(overlay, false);
        tag.rt.anchorMin = tag.rt.anchorMax = new Vector2(0.5f, 0.5f);
        tag.rt.pivot = new Vector2(0.5f, 0.5f);
        tag.rt.sizeDelta = size;
        tag.group = go.GetComponent<CanvasGroup>();
        tag.group.blocksRaycasts = false;
        tag.group.interactable = false;
        tag.bg = go.GetComponent<Image>();
        tag.bg.color = bgColor;
        tag.bg.raycastTarget = false;
        // 둥근 모서리 기본 스프라이트(에디터 내장). 없으면 사각형
#if UNITY_EDITOR
        Sprite round = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (round != null)
        {
            tag.bg.sprite = round;
            tag.bg.type = Image.Type.Sliced;
        }
#endif
        tag.text = MakeText(name + "Text", label, textColor, fontSize, size);
        tag.text.rectTransform.SetParent(tag.rt, false);
        tag.text.rectTransform.anchorMin = Vector2.zero;
        tag.text.rectTransform.anchorMax = Vector2.one;
        tag.text.rectTransform.offsetMin = Vector2.zero;
        tag.text.rectTransform.offsetMax = Vector2.zero;
        go.SetActive(false);
        return tag;
    }

    private TMP_Text MakeText(string name, string label, Color color, float fontSize, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(overlay, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.SetText(label);
        return text;
    }

    private void Place(Tag tag, Vector2 screen, float lift, bool dim)
    {
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out local)) return;
        if (!tag.rt.gameObject.activeSelf) tag.rt.gameObject.SetActive(true);
        tag.rt.anchoredPosition = local + new Vector2(0f, lift);
        tag.group.alpha = dim ? DimAlpha : 1f;
    }

    // 앵커의 x는 그대로 두고 y만 구간으로. 오프셋 y는 0
    private void SetBand(RectTransform band, float min01, float max01)
    {
        band.anchorMin = new Vector2(band.anchorMin.x, min01);
        band.anchorMax = new Vector2(band.anchorMax.x, max01);
        band.offsetMin = new Vector2(band.offsetMin.x, 0f);
        band.offsetMax = new Vector2(band.offsetMax.x, 0f);
    }

    private void SetFill(RectTransform fill, TMP_Text percentText, float v01)
    {
        v01 = Mathf.Clamp01(v01);
        fill.anchorMax = new Vector2(fill.anchorMax.x, v01);
        fill.gameObject.SetActive(v01 > 0f);
        percentText.SetText("{0}%", Mathf.RoundToInt(v01 * 100f));
    }
}
