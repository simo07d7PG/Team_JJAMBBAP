using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 미니게임 조작 상시 표시: 플레이 중 화면 오른쪽 아래에 '조작: …' 한 줄을 반투명 갈색 패널로 보여 준다.
// 런타임에 만들며, 문구는 CafeLoop.CurrentControlLabel을 그대로 쓴다
public class MinigameHint : MonoBehaviour
{
    private const float Margin = 40f;
    private const float PanelHeight = 56f;
    private const float PadX = 24f;
    private const float FontSize = 28f;

    private static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.72f);
    private static readonly Color BorderColor = new Color(0.85f, 0.62f, 0.32f, 0.85f);

    private CafeLoop loop;
    private RectTransform panel;
    private TMP_Text label;
    private string lastLabel = null;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
        if (loop.instructionText == null) return;
        RectTransform parent = loop.instructionText.rectTransform.parent as RectTransform;
        if (parent == null) return;

        GameObject go = new GameObject("MinigameHintPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        panel = go.GetComponent<RectTransform>();
        panel.SetParent(parent, false);
        panel.anchorMin = new Vector2(1f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(1f, 0f);
        panel.anchoredPosition = new Vector2(-Margin, Margin);
        panel.sizeDelta = new Vector2(400f, PanelHeight);

        UImage img = go.GetComponent<UImage>();
        if (loop.receiptBox != null)
        {
            UImage src = loop.receiptBox.GetComponent<UImage>();
            if (src != null && src.sprite != null)
            {
                img.sprite = src.sprite;
                img.type = src.sprite.border.sqrMagnitude > 0f ? UImage.Type.Sliced : UImage.Type.Simple;
            }
        }
        img.color = PanelColor;
        img.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform tr = textGo.GetComponent<RectTransform>();
        tr.SetParent(panel, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(PadX, 0f);
        tr.offsetMax = new Vector2(-PadX, 0f);
        label = textGo.GetComponent<TextMeshProUGUI>();
        label.font = loop.instructionText.font;
        label.fontSharedMaterial = loop.instructionText.fontSharedMaterial;
        label.fontSize = FontSize;
        label.color = new Color(1f, 0.95f, 0.85f, 1f);
        label.alignment = TextAlignmentOptions.Midline;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (panel == null || loop == null) return;
        string text = loop.IsMinigamePlaying ? loop.CurrentControlLabel : "";
        bool show = text != "";
        if (panel.gameObject.activeSelf != show) panel.gameObject.SetActive(show);
        if (!show || text == lastLabel) return;
        lastLabel = text;
        label.text = "조작: " + text;
        // 글자 길이에 맞춰 패널 너비를 맞춘다
        float width = label.GetPreferredValues(label.text).x + PadX * 2f;
        panel.sizeDelta = new Vector2(width, PanelHeight);
    }
}
