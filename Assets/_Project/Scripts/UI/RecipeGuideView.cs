using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 로비 도감의 커피 상세 패널에 '만드는 법' 버튼을 붙이고, 누르면 전체 화면 오버레이로 단계별 조작법을 보여 준다.
// 씬 구조상 상세 패널의 빈자리를 알 수 없고(배경 그림에 글자가 구워져 있음) 영상 버튼은 3개 패널에만 있어서,
// 기존 레이아웃을 건드리지 않는 '버튼 + 오버레이' 방식을 쓴다.
public class RecipeGuideView : MonoBehaviour
{
    // 도감 칸 번호 -> MinigameGuide.Menus 메뉴 이름. 잠긴 칸(4~7)은 없어서 아무것도 만들지 않는다.
    private static readonly string[] MenuNameByIndex =
    {
        "에스프레소", "아메리카노", "아이스아메리카노", "라떼",
    };

    private static readonly Vector2 ButtonPos = new Vector2(131f, 320f);   // 기존 영상 버튼 자리
    private static readonly Vector2 ButtonSize = new Vector2(170f, 60f);
    private static readonly Color PanelColor = new Color(0.10f, 0.07f, 0.05f, 0.96f);
    private static readonly Color ButtonColor = new Color(0.42f, 0.26f, 0.14f, 1f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.4f);
    private static readonly Color TextColor = new Color(0.96f, 0.92f, 0.85f);

    private int coffeeIndex = -1;
    private GameObject button;
    private GameObject overlay;

    // LobbyManager가 커피 상세 패널을 열 때 부른다
    public static void Show(GameObject coffeePanel, int index)
    {
        if (coffeePanel == null) return;
        if (index < 0 || index >= MenuNameByIndex.Length) return;
        if (FindMenu(MenuNameByIndex[index]) == null) return;   // 매칭 안 되면 아무것도 만들지 않음

        RecipeGuideView view = coffeePanel.GetComponent<RecipeGuideView>();
        if (view == null) view = coffeePanel.AddComponent<RecipeGuideView>();
        view.coffeeIndex = index;
        view.EnsureButton();
        if (view.overlay != null) view.overlay.SetActive(false);   // 다시 열 때는 목록을 닫은 상태로
    }

    private static MinigameGuide.MenuRecipe FindMenu(string name)
    {
        MinigameGuide.MenuRecipe[] menus = MinigameGuide.Menus;
        if (menus == null) return null;
        for (int i = 0; i < menus.Length; i++)
        {
            // 띄어쓰기 차이("아이스 아메리카노")는 무시하고 비교
            if (menus[i] != null && menus[i].menuName != null
                && menus[i].menuName.Replace(" ", "") == name.Replace(" ", "")) return menus[i];
        }
        return null;
    }

    private static TMP_FontAsset FindFont()
    {
        TMP_Text src = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        return src != null ? src.font : null;
    }

    private void EnsureButton()
    {
        if (button != null) { button.SetActive(true); return; }

        // 반응 없는 영상 버튼은 숨기고 그 자리에 '만드는 법' 버튼을 둔다
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Gif_Button")) child.gameObject.SetActive(false);
        }

        button = new GameObject("RecipeGuideButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage), typeof(Button));
        RectTransform rt = (RectTransform)button.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = ButtonPos;
        rt.sizeDelta = ButtonSize;
        UImage bg = button.GetComponent<UImage>();
        bg.color = ButtonColor;
        button.GetComponent<Button>().targetGraphic = bg;
        button.GetComponent<Button>().onClick.AddListener(OpenOverlay);

        TMP_Text label = MakeText(rt, "Label", FindFont(), 28f, TextColor, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, Vector2.zero, Vector2.zero);
        label.text = "만드는 법";
        label.raycastTarget = false;
    }

    private void OpenOverlay()
    {
        if (overlay != null) Destroy(overlay);
        overlay = BuildOverlay();
        overlay.transform.SetAsLastSibling();
    }

    private GameObject BuildOverlay()
    {
        TMP_FontAsset font = FindFont();
        MinigameGuide.MenuRecipe menu = coffeeIndex >= 0 && coffeeIndex < MenuNameByIndex.Length
            ? FindMenu(MenuNameByIndex[coffeeIndex]) : null;

        GameObject root = new GameObject("RecipeGuideOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = (RectTransform)root.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        UImage dim = root.GetComponent<UImage>();
        dim.color = PanelColor;
        dim.raycastTarget = true;   // 뒤의 버튼이 눌리지 않게 막는다

        string menuName = menu != null ? menu.menuName : "";
        TMP_Text title = MakeText(rt, "Title", font, 56f, GoldColor, TextAlignmentOptions.Left);
        title.rectTransform.anchorMin = new Vector2(0f, 1f);
        title.rectTransform.anchorMax = new Vector2(1f, 1f);
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.offsetMin = new Vector2(160f, -170f);
        title.rectTransform.offsetMax = new Vector2(-260f, -70f);
        title.text = menuName + " 만드는 법";

        TMP_Text body = MakeText(rt, "Steps", font, 40f, TextColor, TextAlignmentOptions.TopLeft);
        body.rectTransform.anchorMin = Vector2.zero;
        body.rectTransform.anchorMax = Vector2.one;
        body.rectTransform.offsetMin = new Vector2(160f, 90f);
        body.rectTransform.offsetMax = new Vector2(-160f, -190f);
        body.richText = true;
        body.enableAutoSizing = true;
        body.fontSizeMin = 16f;
        body.fontSizeMax = 40f;
        body.text = BuildSteps(menu);

        // 닫기 버튼
        GameObject close = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage), typeof(Button));
        RectTransform crt = (RectTransform)close.transform;
        crt.SetParent(rt, false);
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-100f, -70f);
        crt.sizeDelta = new Vector2(120f, 70f);
        UImage cbg = close.GetComponent<UImage>();
        cbg.color = ButtonColor;
        close.GetComponent<Button>().targetGraphic = cbg;
        GameObject captured = root;
        close.GetComponent<Button>().onClick.AddListener(() => captured.SetActive(false));
        TMP_Text cl = MakeText(crt, "Label", font, 30f, TextColor, TextAlignmentOptions.Center);
        Stretch(cl.rectTransform, Vector2.zero, Vector2.zero);
        cl.text = "닫기";
        cl.raycastTarget = false;
        return root;
    }

    private static string BuildSteps(MinigameGuide.MenuRecipe menu)
    {
        if (menu == null || menu.steps == null) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < menu.steps.Length; i++)
        {
            MinigameGuide.Entry e = MinigameGuide.Get(menu.steps[i]);
            if (e == null) continue;
            sb.Append(i + 1).Append(". ").Append(e.title);
            string how = JoinCaptions(e.controls);
            if (how.Length > 0) sb.Append(" — ").Append(how);
            sb.Append('\n');
            if (!string.IsNullOrEmpty(e.success))
                sb.Append("<size=70%><color=#C8B8A0>    ").Append(e.success).Append("</color></size>\n");
        }
        return sb.ToString();
    }

    private static string JoinCaptions(MinigameGuide.Control[] controls)
    {
        if (controls == null || controls.Length == 0) return "";
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < controls.Length; i++)
        {
            if (controls[i] == null || string.IsNullOrEmpty(controls[i].caption)) continue;
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(controls[i].caption);
        }
        return sb.ToString();
    }

    private static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = min;
        rt.offsetMax = max;
    }

    private static TMP_Text MakeText(RectTransform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.Normal;
        return t;
    }
}
