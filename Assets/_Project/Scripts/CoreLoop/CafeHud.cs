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

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
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
            dayText.alignment = TextAlignmentOptions.MidlineLeft;
            dayText.enableWordWrapping = false;
            dayPanel = MakePanel("DayPanel", dayText);
            MakeBar(dayPanel);
        }
        if (incomeText != null)
        {
            incomeText.alignment = TextAlignmentOptions.MidlineRight;
            incomeText.enableWordWrapping = false;
            incomePanel = MakePanel("IncomePanel", incomeText);
        }
        if (timeText != null)
        {
            timeText.alignment = TextAlignmentOptions.MidlineLeft;
            timeText.enableWordWrapping = false;
            timePanel = MakePanel("TimePanel", timeText);
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
    private void Fit(RectTransform panel, TMP_Text text)
    {
        RectTransform tr = text.rectTransform;
        float width = text.preferredWidth + PadX;
        panel.sizeDelta = new Vector2(width, PanelHeight);
        float extraY = (PanelHeight - tr.rect.height) * 0.5f;
        float ox = Mathf.Lerp(-PadX * 0.5f, PadX * 0.5f, tr.pivot.x);
        float oy = Mathf.Lerp(-extraY, extraY, tr.pivot.y);
        panel.anchoredPosition = tr.anchoredPosition + new Vector2(ox, oy);
    }

    private void LateUpdate()
    {
        if (loop == null) return;
        UpdateDay();
        UpdateIncome();
        UpdateTime();
    }

    private void UpdateDay()
    {
        if (dayPanel == null) return;
        bool on = dayText.gameObject.activeInHierarchy;
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
        if (on) Fit(timePanel, timeText);
    }
}
