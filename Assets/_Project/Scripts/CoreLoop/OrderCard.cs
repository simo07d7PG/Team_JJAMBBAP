using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 주문서 카드: 주문을 받은 뒤(제조 중·제공 대기) 카페 화면 왼쪽에 메뉴·변형·단계 진행을 고정 표시한다.
// 미니게임·영수증·상점·페이드·로딩 중에는 숨기고, 나타날 때 왼쪽에서 미끄러져 들어온다
public class OrderCard : MonoBehaviour
{
    private const float CardWidth = 300f;
    private const float ShownX = 14f; // 날짜 패널 왼쪽 가장자리와 맞춤
    private const float ShownY = -108f; // 날짜 패널(높이 72)+진행 바 아래 12px
    private const float SlideSeconds = 0.2f;
    private const float Pad = 18f;

    private static readonly Color PaperColor = new Color(0.96f, 0.92f, 0.82f, 0.95f);
    private const string InkHex = "#4A3320";
    private const string DimHex = "#A39378";
    private const string GoldHex = "#C98A00";

    private CafeLoop loop;
    private RectTransform root;
    private CanvasGroup group;
    private TMP_Text label;
    private string lastKey = "";
    private bool shown;
    private float slideT = 1f;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
        if (loop.dayText == null || loop.dayText.canvas == null) return;
        Transform canvas = loop.dayText.canvas.rootCanvas.transform;

        GameObject go = new GameObject("OrderCard", typeof(RectTransform), typeof(CanvasGroup), typeof(UImage));
        root = go.GetComponent<RectTransform>();
        root.SetParent(canvas, false);
        root.anchorMin = new Vector2(0f, 1f);
        root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(CardWidth, 200f);
        group = go.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        UImage bg = go.GetComponent<UImage>();
        bg.color = PaperColor;
        bg.raycastTarget = false;
        if (loop.receiptBox != null)
        {
            UImage src = loop.receiptBox.GetComponent<UImage>();
            if (src != null && src.sprite != null)
            {
                bg.sprite = src.sprite;
                bg.type = src.sprite.border.sqrMagnitude > 0f ? UImage.Type.Sliced : UImage.Type.Simple;
            }
        }

        GameObject tgo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform trt = tgo.GetComponent<RectTransform>();
        trt.SetParent(root, false);
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(Pad, 0f);
        trt.offsetMax = new Vector2(-Pad, -Pad);
        label = tgo.GetComponent<TextMeshProUGUI>();
        label.font = loop.dayText.font;
        label.fontSharedMaterial = loop.dayText.fontSharedMaterial;
        label.fontSize = 24f;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = true;
        label.richText = true;
        label.raycastTarget = false;

        // 날짜 패널 등 다른 HUD 위로 올라오지 않게 맨 앞이 아닌 맨 뒤쪽 순서 유지
        root.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (root == null || loop == null) return;
        bool want = loop.HasActiveOrder && loop.IsCafeState && loop.Mood == 0;
        if (want && !shown)
        {
            shown = true;
            slideT = 0f;
            lastKey = "";
            root.gameObject.SetActive(true);
        }
        else if (!want && shown)
        {
            shown = false;
            root.gameObject.SetActive(false);
            return;
        }
        if (!shown) return;

        Rebuild();
        if (slideT < 1f) slideT = Mathf.Min(1f, slideT + Time.unscaledDeltaTime / SlideSeconds);
        float e = 1f - (1f - slideT) * (1f - slideT);
        root.anchoredPosition = new Vector2(Mathf.Lerp(-CardWidth - 40f, ShownX, e), ShownY);
        group.alpha = e;
    }

    private void Rebuild()
    {
        int count = loop.OrderStepCount;
        StringBuilder sb = new StringBuilder();
        sb.Append("<size=20><color=").Append(DimHex).Append(">주문서</color></size>\n");
        sb.Append("<size=34><b><color=").Append(InkHex).Append(">").Append(loop.OrderMenu != null ? loop.OrderMenu.menuName : "").Append("</color></b></size>\n");
        string tag = CustomerLines.PersonalityLabel(loop.OrderPersonality);
        if (tag != "") sb.Append("<size=18><color=").Append(DimHex).Append(">").Append(tag).Append("</color></size>\n");
        string request = loop.OrderRequest;
        if (!string.IsNullOrEmpty(request))
        {
            sb.Append("<color=").Append(GoldHex).Append(">").Append(request).Append("</color>\n");
        }
        sb.Append("\n");
        int cur = loop.OrderStepIndex;
        for (int i = 0; i < count; i++)
        {
            string name = loop.OrderStepName(i);
            int r = loop.OrderStepResult(i);
            if (r >= 0)
            {
                string mark = r == 1 ? "●" : "x";
                sb.Append("<color=").Append(DimHex).Append("><s>").Append(mark).Append(" ").Append(name).Append("</s></color>\n");
            }
            else if (i == cur)
            {
                sb.Append("<b><color=").Append(GoldHex).Append(">→ ").Append(name).Append("</color></b>\n");
            }
            else
            {
                sb.Append("<color=").Append(DimHex).Append(">   ").Append(name).Append("</color>\n");
            }
        }
        string key = sb.ToString();
        if (key == lastKey) return;
        lastKey = key;
        label.text = key;
        label.ForceMeshUpdate();
        root.sizeDelta = new Vector2(CardWidth, label.preferredHeight + Pad * 2f);
    }
}
