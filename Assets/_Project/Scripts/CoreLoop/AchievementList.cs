using TMPro;
using UnityEngine;
using UImage = UnityEngine.UI.Image;

// 로비의 업적 패널(questPanel) 안에 업적 목록을 2열 격자로 채운다. 열 때마다 Show로 갱신
public class AchievementList : MonoBehaviour
{
    private const float MarginLeft = 40f;
    private const float MarginRight = 180f;   // 오른쪽 위 닫기 버튼 자리
    private const float MarginTop = 130f;     // 위쪽 탭 버튼 자리
    private const float MarginBottom = 40f;
    private const float Gap = 16f;
    private static readonly Color CellColor = new Color(0.12f, 0.08f, 0.06f, 0.78f);
    private static readonly Color DoneColor = new Color(0.30f, 0.20f, 0.08f, 0.9f);
    private static readonly Color GoldColor = new Color(1f, 0.82f, 0.4f);
    private static readonly Color GrayColor = new Color(0.8f, 0.76f, 0.7f);

    private RectTransform grid;

    // LobbyManager가 업적 패널을 열 때 부른다
    public static void Show(GameObject questPanel)
    {
        if (questPanel == null) return;
        AchievementList list = questPanel.GetComponent<AchievementList>();
        if (list == null) list = questPanel.AddComponent<AchievementList>();
        list.Rebuild();
    }

    private void Rebuild()
    {
        TMP_Text fontSource = FindFirstObjectByType<TMP_Text>(FindObjectsInactive.Include);
        TMP_FontAsset font = fontSource != null ? fontSource.font : null;
        RectTransform panel = (RectTransform)transform;

        // 이전에 만든 격자는 지우고 새로 만든다 (진행도 갱신)
        if (grid != null) Destroy(grid.gameObject);
        GameObject go = new GameObject("AchievementGrid", typeof(RectTransform));
        grid = go.GetComponent<RectTransform>();
        grid.SetParent(panel, false);
        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = new Vector2(MarginLeft, MarginBottom);
        grid.offsetMax = new Vector2(-MarginRight, -MarginTop);

        int count = Achievements.All.Length;
        int rows = (count + 1) / 2;
        // 칸 크기는 패널 크기에 맞춘다 (rect는 이 시점에 레이아웃 전일 수 있어 sizeDelta 기준으로 계산)
        float width = Mathf.Max(200f, panel.rect.width > 1f ? panel.rect.width : panel.sizeDelta.x) - MarginLeft - MarginRight;
        float height = Mathf.Max(200f, panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y) - MarginTop - MarginBottom;
        float cellW = (width - Gap) / 2f;
        float cellH = Mathf.Min(150f, (height - Gap * (rows - 1)) / rows);

        for (int i = 0; i < count; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Vector2 pos = new Vector2(col * (cellW + Gap), -row * (cellH + Gap));
            MakeCell(Achievements.All[i], font, pos, new Vector2(cellW, cellH));
        }

        // 맨 아래 한 줄: 달성 수
        TMP_Text total = MakeText(grid, "Total", font, 28f, GoldColor, TextAlignmentOptions.Right);
        total.rectTransform.anchorMin = new Vector2(0f, 0f);
        total.rectTransform.anchorMax = new Vector2(1f, 0f);
        total.rectTransform.pivot = new Vector2(1f, 0f);
        total.rectTransform.anchoredPosition = Vector2.zero;
        total.rectTransform.sizeDelta = new Vector2(0f, 36f);
        total.text = "달성 " + Achievements.UnlockedCount() + " / " + count;
        grid.SetAsLastSibling();
    }

    private void MakeCell(Achievements.Def def, TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        bool done = Achievements.IsUnlocked(def.id);
        GameObject go = new GameObject("Cell_" + def.id, typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(grid, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        UImage img = go.GetComponent<UImage>();
        img.color = done ? DoneColor : CellColor;
        img.raycastTarget = false;

        TMP_Text title = MakeText(rt, "Title", font, 36f, done ? GoldColor : Color.white, TextAlignmentOptions.TopLeft);
        SetRect(title.rectTransform, new Vector2(20f, -12f), new Vector2(-110f, 46f));
        title.text = def.title;

        TMP_Text star = MakeText(rt, "Star", font, 44f, done ? GoldColor : GrayColor, TextAlignmentOptions.TopRight);
        star.rectTransform.anchorMin = new Vector2(1f, 1f);
        star.rectTransform.anchorMax = new Vector2(1f, 1f);
        star.rectTransform.pivot = new Vector2(1f, 1f);
        star.rectTransform.anchoredPosition = new Vector2(-18f, -10f);
        star.rectTransform.sizeDelta = new Vector2(80f, 50f);
        star.text = done ? "★" : "☆";

        TMP_Text desc = MakeText(rt, "Desc", font, 26f, GrayColor, TextAlignmentOptions.TopLeft);
        SetRect(desc.rectTransform, new Vector2(20f, -62f), new Vector2(-40f, 36f));
        desc.text = def.description;

        // 진행도는 여러 번 모아야 하는 업적에만 보여 준다 (목표 1은 달성 표시로 충분)
        TMP_Text prog = MakeText(rt, "Progress", font, 26f, done ? GoldColor : Color.white, TextAlignmentOptions.BottomRight);
        prog.rectTransform.anchorMin = new Vector2(1f, 0f);
        prog.rectTransform.anchorMax = new Vector2(1f, 0f);
        prog.rectTransform.pivot = new Vector2(1f, 0f);
        prog.rectTransform.anchoredPosition = new Vector2(-18f, 8f);
        prog.rectTransform.sizeDelta = new Vector2(200f, 34f);
        prog.text = def.target > 1 || !done ? Achievements.Progress(def.id) + "/" + def.target : "달성";
    }

    // 위·왼쪽 기준 가로로 늘어나는 칸 (left, top 오프셋 / 가로는 right 여백 음수, 높이)
    private static void SetRect(RectTransform rt, Vector2 topLeft, Vector2 widthMinusAndHeight)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = topLeft;
        rt.sizeDelta = new Vector2(widthMinusAndHeight.x - topLeft.x, widthMinusAndHeight.y);
    }

    private static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        TMP_Text t = go.GetComponent<TMP_Text>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.raycastTarget = false;
        return t;
    }
}
