using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 카페 HUD 정리: 날짜·시간, 수입, 미니게임 남은 시간 글자 뒤에 둥근 반투명 패널을 깔고
// 하루 진행 바와 수입 튕김을 더한다. 씬은 그대로 두고 런타임에 만든다
public class CafeHud : MonoBehaviour
{
    private const float PadX = 28f;
    private const float PanelHeight = 72f;
    private const float BarHeight = 10f;
    private const float BounceSeconds = 0.25f;
    private const float BounceScale = 1.12f;
    private const string IncomeMark = "<color=#FFD166>●</color> 수입 ";

    private static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.72f);
    private static readonly Color BorderColor = new Color(0.85f, 0.62f, 0.32f, 0.85f);
    private static readonly Color BarBack = new Color(0f, 0f, 0f, 0.45f);
    private static readonly Color BarStart = new Color(1f, 0.82f, 0.4f, 1f);
    private static readonly Color BarEnd = new Color(1f, 0.5f, 0.15f, 1f);

    private CafeLoop loop;
    private Sprite panelSprite;
    private UImage.Type panelType = UImage.Type.Simple;

    private TMP_Text dayText;
    private RectTransform dayPanel;
    private RectTransform dayFill;
    private UImage dayFillImage;

    private TMP_Text incomeText;
    private RectTransform incomePanel;
    private int lastIncome = -1;
    private float bounceTimer = BounceSeconds;

    private TMP_Text timeText;
    private RectTransform timePanel;
    // 팀원 ESC 메뉴 로고 버튼(왼쪽 위). 미니게임 중에는 안내 종이와 겹쳐서 숨긴다 (ESC 키는 그대로 동작)
    private CanvasGroup escButtonGroup;
    private const float TimePanelHeight = 40f;

    // 지시어·결과 글자 뒤 시네마틱 띠 (글자가 켜질 때만 보이고, 세로로 펼쳐지며 등장)
    private const float BandPadY = 56f;
    private const float BandPadX = 220f;
    private const float BandMinWidth = 900f;
    private const float BandLineThickness = 4f;
    private const float BandOpenSeconds = 0.15f;
    private static readonly Color BandColor = new Color(0.10f, 0.06f, 0.04f, 0.82f);
    private static readonly Color BandLineColor = new Color(1f, 0.82f, 0.35f, 0.9f);
    private static Sprite bandSprite;

    private class Band
    {
        public TMP_Text text;
        public RectTransform root;
        public Vector2 basePos; // 글자의 원래 위치 (Shake로 움직여도 띠는 고정)
        public bool wasOn;
        public float timer;
    }

    private Band instructionBand;
    private Band resultBand;
    private RectTransform bandCanvasRect;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
        if (loop.dayText != null && loop.dayText.canvas != null) bandCanvasRect = loop.dayText.canvas.rootCanvas.transform as RectTransform;
        instructionBand = MakeBand("InstructionBand", loop.instructionText);
        resultBand = MakeBand("ResultBand", loop.resultText);
        if (loop.receiptBox != null)
        {
            UImage src = loop.receiptBox.GetComponent<UImage>();
            if (src != null)
            {
                panelSprite = src.sprite;
                panelType = src.sprite != null && src.sprite.border.sqrMagnitude > 0f ? UImage.Type.Sliced : UImage.Type.Simple;
            }
        }
        dayText = loop.dayText;
        incomeText = loop.incomeText;
        timeText = loop.timeText;

        if (dayText != null)
        {
            // 씬 값과 상관없이 왼쪽 위 모서리에 고정 (패널은 가장자리에서 가로 14, 세로 8 안팎)
            RectTransform dr = dayText.rectTransform;
            dr.anchorMin = new Vector2(0f, 1f);
            dr.anchorMax = new Vector2(0f, 1f);
            dr.pivot = new Vector2(0f, 1f);
            dr.anchoredPosition = new Vector2(PadX, -14f);
            dayText.alignment = TextAlignmentOptions.MidlineLeft;
            dayText.enableWordWrapping = false;
            dayPanel = MakePanel("DayPanel", dayText);
            MakeBar(dayPanel);
        }
        if (incomeText != null)
        {
            // 오른쪽 위 모서리에 고정
            RectTransform ir = incomeText.rectTransform;
            ir.anchorMin = new Vector2(1f, 1f);
            ir.anchorMax = new Vector2(1f, 1f);
            ir.pivot = new Vector2(1f, 1f);
            ir.anchoredPosition = new Vector2(-PadX, -14f);
            incomeText.alignment = TextAlignmentOptions.MidlineRight;
            incomeText.enableWordWrapping = false;
            incomePanel = MakePanel("IncomePanel", incomeText);
        }
        if (timeText != null)
        {
            // 미니게임 HUD는 화면 위쪽 3/4까지 쓴다. 맨 위 가는 띠(인내도 막대 + 남은 시간)에만 둬서 겹치지 않게 한다
            RectTransform tr = timeText.rectTransform;
            tr.anchorMin = new Vector2(0.5f, 1f);
            tr.anchorMax = new Vector2(0.5f, 1f);
            tr.pivot = new Vector2(0f, 1f);
            tr.sizeDelta = new Vector2(420f, 36f);
            tr.anchoredPosition = new Vector2(310f, -1f);
            timeText.fontSize = 28f;
            timeText.alignment = TextAlignmentOptions.MidlineLeft;
            timeText.enableWordWrapping = false;
            timePanel = MakePanel("TimePanel", timeText);
        }
        if (loop.minigamePatienceBar != null)
        {
            RectTransform bar = loop.minigamePatienceBar.GetComponent<RectTransform>();
            bar.anchorMin = new Vector2(0.5f, 1f);
            bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = new Vector2(0f, -2f);
        }
    }

    // 글자와 같은 sibling 자리에 패널을 끼워 글자가 한 칸 뒤로 밀리게 한다 (글자 뒤에 그려지고, 덮이는 패널보다는 아래)
    private RectTransform MakePanel(string name, TMP_Text text)
    {
        RectTransform tr = text.rectTransform;
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(tr.parent, false);
        rt.SetSiblingIndex(tr.GetSiblingIndex());
        rt.anchorMin = tr.anchorMin;
        rt.anchorMax = tr.anchorMax;
        rt.pivot = tr.pivot;
        UImage img = go.GetComponent<UImage>();
        img.sprite = panelSprite;
        img.type = panelType;
        img.color = PanelColor;
        img.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        return rt;
    }

    // 가운데 불투명, 양끝 알파 0인 256x1 가로 그라데이션 (한 번만 만든다)
    private static Sprite GetBandSprite()
    {
        if (bandSprite != null) return bandSprite;
        const int w = 256;
        Texture2D tex = new Texture2D(w, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w;
            float edge = Mathf.Min(u, 1f - u) / 0.2f; // 양끝 20% 구간에서 서서히 투명
            tex.SetPixel(x, 0, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(edge))));
        }
        tex.Apply();
        bandSprite = Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
        return bandSprite;
    }

    private UImage MakeBandImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        UImage img = go.GetComponent<UImage>();
        img.sprite = GetBandSprite();
        img.type = UImage.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Band MakeBand(string name, TMP_Text text)
    {
        if (text == null) return null;
        RectTransform tr = text.rectTransform;
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform root = go.GetComponent<RectTransform>();
        root.SetParent(tr.parent, false);
        root.SetSiblingIndex(tr.GetSiblingIndex());
        root.anchorMin = tr.anchorMin;
        root.anchorMax = tr.anchorMax;
        root.pivot = new Vector2(0.5f, 0.5f);

        UImage body = MakeBandImage("Body", root, BandColor);
        RectTransform br = body.rectTransform;
        br.anchorMin = Vector2.zero;
        br.anchorMax = Vector2.one;
        br.offsetMin = Vector2.zero;
        br.offsetMax = Vector2.zero;

        UImage top = MakeBandImage("LineTop", root, BandLineColor);
        RectTransform tt = top.rectTransform;
        tt.anchorMin = new Vector2(0f, 1f);
        tt.anchorMax = new Vector2(1f, 1f);
        tt.pivot = new Vector2(0.5f, 1f);
        tt.anchoredPosition = Vector2.zero;
        tt.sizeDelta = new Vector2(0f, BandLineThickness);

        UImage bottom = MakeBandImage("LineBottom", root, BandLineColor);
        RectTransform bt = bottom.rectTransform;
        bt.anchorMin = new Vector2(0f, 0f);
        bt.anchorMax = new Vector2(1f, 0f);
        bt.pivot = new Vector2(0.5f, 0f);
        bt.anchoredPosition = Vector2.zero;
        bt.sizeDelta = new Vector2(0f, BandLineThickness);

        go.SetActive(false);
        return new Band { text = text, root = root, basePos = tr.anchoredPosition };
    }

    private void UpdateBand(Band band)
    {
        if (band == null) return;
        bool on = band.text.gameObject.activeInHierarchy;
        if (band.root.gameObject.activeSelf != on) band.root.gameObject.SetActive(on);
        if (!on)
        {
            band.wasOn = false;
            return;
        }
        if (!band.wasOn)
        {
            band.wasOn = true;
            band.timer = 0f;
        }

        RectTransform tr = band.text.rectTransform;
        float maxWidth = bandCanvasRect != null ? bandCanvasRect.rect.width : 1920f;
        float width = Mathf.Clamp(band.text.preferredWidth + BandPadX * 2f, BandMinWidth, maxWidth);
        float height = band.text.preferredHeight + BandPadY;
        band.root.sizeDelta = new Vector2(width, height);
        // 글자 영역의 중심에 맞춘다 (글자의 피벗이 가운데가 아닌 경우 보정)
        Vector2 size = tr.rect.size;
        band.root.anchoredPosition = band.basePos + new Vector2((0.5f - tr.pivot.x) * size.x, (0.5f - tr.pivot.y) * size.y);

        float scaleY = 1f;
        if (band.timer < BandOpenSeconds)
        {
            band.timer += Time.unscaledDeltaTime;
            scaleY = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(band.timer / BandOpenSeconds));
        }
        band.root.localScale = new Vector3(1f, scaleY, 1f);
    }

    private void MakeBar(RectTransform panel)
    {
        GameObject back = new GameObject("DayBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform br = back.GetComponent<RectTransform>();
        br.SetParent(panel, false);
        br.anchorMin = new Vector2(0f, 0f);
        br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(0.5f, 1f);
        br.anchoredPosition = new Vector2(0f, -6f);
        br.sizeDelta = new Vector2(0f, BarHeight);
        UImage bi = back.GetComponent<UImage>();
        bi.sprite = panelSprite;
        bi.type = panelType;
        bi.color = BarBack;
        bi.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        dayFill = fill.GetComponent<RectTransform>();
        dayFill.SetParent(br, false);
        dayFill.anchorMin = Vector2.zero;
        dayFill.anchorMax = new Vector2(0f, 1f);
        dayFill.pivot = new Vector2(0f, 0.5f);
        dayFill.offsetMin = Vector2.zero;
        dayFill.offsetMax = Vector2.zero;
        dayFillImage = fill.GetComponent<UImage>();
        dayFillImage.sprite = panelSprite;
        dayFillImage.type = panelType;
        dayFillImage.color = BarStart;
        dayFillImage.raycastTarget = false;
    }

    // 패널을 글자 크기에 맞춘다. 글자의 피벗 쪽 기준으로 좌우 여백만큼 바깥으로 둔다
    private void Fit(RectTransform panel, TMP_Text text, float height = PanelHeight)
    {
        RectTransform tr = text.rectTransform;
        float width = text.preferredWidth + PadX;
        panel.sizeDelta = new Vector2(width, height);
        float extraY = (height - tr.rect.height) * 0.5f;
        float ox = Mathf.Lerp(-PadX * 0.5f, PadX * 0.5f, tr.pivot.x);
        float oy = Mathf.Lerp(-extraY, extraY, tr.pivot.y);
        panel.anchoredPosition = tr.anchoredPosition + new Vector2(ox, oy);
    }

    private void FindEscButton()
    {
        foreach (Button b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.onClick.GetPersistentEventCount() > 0 && b.onClick.GetPersistentTarget(0) is EscManager)
            {
                escButtonGroup = b.GetComponent<CanvasGroup>();
                if (escButtonGroup == null) escButtonGroup = b.gameObject.AddComponent<CanvasGroup>();
                return;
            }
        }
    }

    private void UpdateEscButton()
    {
        if (escButtonGroup == null)
        {
            FindEscButton();
            if (escButtonGroup == null) return;
        }
        bool hide = loop.Mood == 1;
        escButtonGroup.alpha = hide ? 0f : 1f;
        escButtonGroup.blocksRaycasts = !hide;
    }

    private void LateUpdate()
    {
        if (loop == null) return;
        UpdateDay();
        UpdateIncome();
        UpdateTime();
        UpdateEscButton();
        UpdateBand(instructionBand);
        UpdateBand(resultBand);
    }

    private void UpdateDay()
    {
        if (dayPanel == null) return;
        // 미니게임 남은 시간 패널이 같은 자리에 뜨는 동안은 날짜 패널과 글자를 숨긴다
        bool timeOn = timeText != null && timeText.gameObject.activeInHierarchy;
        bool on = dayText.gameObject.activeInHierarchy && !timeOn;
        dayText.enabled = !timeOn;
        if (dayPanel.gameObject.activeSelf != on) dayPanel.gameObject.SetActive(on);
        if (!on) return;
        Fit(dayPanel, dayText);
        float p = Mathf.Clamp01(loop.DayProgress());
        dayFill.anchorMax = new Vector2(p, 1f);
        dayFillImage.color = Color.Lerp(BarStart, BarEnd, p);
    }

    private void UpdateIncome()
    {
        if (incomePanel == null) return;
        bool on = incomeText.gameObject.activeInHierarchy;
        if (incomePanel.gameObject.activeSelf != on) incomePanel.gameObject.SetActive(on);
        if (!on) return;

        // CafeLoop·CafeEffects는 "수입 N원"을 쓴다. 숫자만 뽑아 쉼표·동전 표시로 다시 꾸민다
        string raw = incomeText.text;
        if (!raw.StartsWith("<"))
        {
            int value = 0;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] >= '0' && raw[i] <= '9') value = value * 10 + (raw[i] - '0');
            }
            incomeText.text = IncomeMark + value.ToString("N0") + "원";
            if (lastIncome >= 0 && value > lastIncome && bounceTimer >= BounceSeconds) bounceTimer = 0f;
            lastIncome = value;
        }
        Fit(incomePanel, incomeText);

        float scale = 1f;
        if (bounceTimer < BounceSeconds)
        {
            bounceTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(bounceTimer / BounceSeconds);
            scale = 1f + (BounceScale - 1f) * Mathf.Sin(t * Mathf.PI);
        }
        incomePanel.localScale = new Vector3(scale, scale, 1f);
        incomeText.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    private void UpdateTime()
    {
        if (timePanel == null) return;
        bool on = timeText.gameObject.activeInHierarchy;
        if (timePanel.gameObject.activeSelf != on) timePanel.gameObject.SetActive(on);
        if (on) Fit(timePanel, timeText, TimePanelHeight);
    }
}
