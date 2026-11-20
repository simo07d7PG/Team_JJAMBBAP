using TMPro;
using UnityEngine;
using UImage = UnityEngine.UI.Image;

// 로비의 업적 패널(questPanel) 안에 업적 목록을 2열 격자로 채운다. 열 때마다 Show로 갱신
// 패널 배경은 417x249 책 펼침 그림(Guide_Penal)을 늘린 것이라, 위치는 그림 픽셀 좌표 기준으로 잡는다
// 왼쪽 쪽 / 오른쪽 쪽에 한 열씩, 도감(MenuPanel)의 스크롤 영역(왼쪽 쪽 안쪽)과 같은 세로 범위를 쓴다
public class AchievementList : MonoBehaviour
{
    // 배경 그림 크기와 내용 영역(그림 픽셀, 왼쪽 위 원점)
    private const float ArtW = 417f;
    private const float ArtH = 249f;
    private const float LeftColX0 = 43.5f;    // 왼쪽 쪽 열: 안쪽 테두리 안, 책등 앞
    private const float LeftColX1 = 191f;
    private const float RightColX0 = 226f;    // 오른쪽 쪽 열: 오른쪽 위 닫기 버튼(패널 x 672~772)과 겹치지 않는 선까지
    private const float RightColX1 = 373.5f;
    private const float AreaY0 = 36.25f;      // 위쪽 탭 버튼 아래
    private const float AreaY1 = 216.25f;

    private const int Columns = 2;
    private const int Rows = 6;               // 업적 11칸 + 달성 요약 1칸
    private const float Gap = 10f;

    private static readonly Color CellColor = new Color(0.12f, 0.08f, 0.06f, 0.78f);
    private static readonly Color DoneColor = new Color(0.30f, 0.20f, 0.08f, 0.9f);
    private static readonly Color SummaryColor = new Color(0.20f, 0.13f, 0.07f, 0.85f);
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

    // 패널이 닫히거나 다른 탭으로 바뀌면 격자를 지워 다른 팝업 위에 남지 않게 한다 (열 때 Show가 다시 만든다)
    private void OnDisable()
    {
        if (grid != null) Destroy(grid.gameObject);
        grid = null;
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
        grid.localScale = Vector3.one;
        // 패널 전체를 덮는 좌표계(왼쪽 위가 원점)로 쓰고, 칸은 모두 왼쪽 위 기준으로 배치한다
        grid.anchorMin = Vector2.zero;
        grid.anchorMax = Vector2.one;
        grid.offsetMin = Vector2.zero;
        grid.offsetMax = Vector2.zero;

        // 패널 크기: 오브젝트가 막 켜져 rect가 0이면 sizeDelta로 대신한다 (중앙 고정 앵커라 sizeDelta가 곧 크기)
        float pw = panel.rect.width > 1f ? panel.rect.width : panel.sizeDelta.x;
        float ph = panel.rect.height > 1f ? panel.rect.height : panel.sizeDelta.y;
        if (pw < 1f || ph < 1f) { pw = 1668f; ph = 996f; }
        float sx = pw / ArtW;
        float sy = ph / ArtH;

        float areaTop = AreaY0 * sy;
        float areaH = (AreaY1 - AreaY0) * sy;
        float cellH = (areaH - Gap * (Rows - 1)) / Rows;
        float[] colX = { LeftColX0 * sx, RightColX0 * sx };
        float[] colW = { (LeftColX1 - LeftColX0) * sx, (RightColX1 - RightColX0) * sx };

        int count = Achievements.All.Length;
        for (int i = 0; i < Columns * Rows; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            // 패널 왼쪽 위 기준 좌표를 패널 중앙 기준으로 바꾼 뒤, 왼쪽 위 앵커 위치로 놓는다
            Vector2 pos = new Vector2(colX[col], -(areaTop + row * (cellH + Gap)));
            Vector2 size = new Vector2(colW[col], cellH);
            if (i < count) MakeCell(Achievements.All[i], font, pos, size);
            else MakeSummary(count, font, pos, size);
        }
        grid.SetAsLastSibling();
    }

    // 왼쪽 위 앵커·피벗 칸 만들기 (pos는 패널 왼쪽 위에서 오른쪽/아래 방향 오프셋)
    private RectTransform MakeBox(string name, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(grid, false);
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        UImage img = go.GetComponent<UImage>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private void MakeSummary(int count, TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        RectTransform rt = MakeBox("Cell_Summary", pos, size, SummaryColor);
        TMP_Text total = MakeText(rt, "Total", font, GoldColor, TextAlignmentOptions.Center, false, 20f, Mathf.Floor(size.y * 0.4f));
        Stretch(total.rectTransform, 12f, 8f, 12f, 8f);
        total.text = "달성 " + Achievements.UnlockedCount() + " / " + count;
    }

    private void MakeCell(Achievements.Def def, TMP_FontAsset font, Vector2 pos, Vector2 size)
    {
        bool done = Achievements.IsUnlocked(def.id);
        RectTransform rt = MakeBox("Cell_" + def.id, pos, size, done ? DoneColor : CellColor);

        const float pad = 14f;
        float starW = Mathf.Min(64f, size.y * 0.5f);
        float titleH = Mathf.Floor(size.y * 0.38f);          // 제목 줄
        float progW = Mathf.Min(120f, size.x * 0.22f);       // 진행도(오른쪽 아래)
        float progH = Mathf.Floor(size.y * 0.30f);

        // 제목: 위쪽 줄, 오른쪽은 별 자리만큼 비운다
        TMP_Text title = MakeText(rt, "Title", font, done ? GoldColor : Color.white, TextAlignmentOptions.MidlineLeft, false, 16f, Mathf.Floor(titleH * 0.85f));
        Stretch(title.rectTransform, pad, 6f, starW + pad, size.y - 6f - titleH);
        title.text = def.title;

        // 별: 오른쪽 위
        TMP_Text star = MakeText(rt, "Star", font, done ? GoldColor : GrayColor, TextAlignmentOptions.Center, false, 14f, Mathf.Floor(titleH * 0.9f));
        Stretch(star.rectTransform, size.x - pad - starW, 6f, pad, size.y - 6f - titleH);
        star.text = done ? "★" : "☆";

        // 설명: 제목 아래부터 칸 바닥까지, 진행도 자리를 오른쪽에서 비우고 줄바꿈
        float descTop = 6f + titleH + 2f;
        TMP_Text desc = MakeText(rt, "Desc", font, GrayColor, TextAlignmentOptions.TopLeft, true, 14f, 22f);
        Stretch(desc.rectTransform, pad, descTop, progW + pad * 2f, 6f);
        desc.text = def.description;

        // 진행도는 여러 번 모아야 하는 업적에만 보여 준다 (목표 1은 달성 표시로 충분)
        TMP_Text prog = MakeText(rt, "Progress", font, done ? GoldColor : Color.white, TextAlignmentOptions.BottomRight, false, 14f, Mathf.Floor(progH * 0.8f));
        Stretch(prog.rectTransform, size.x - pad - progW, size.y - 6f - progH, pad, 6f);
        prog.text = def.target > 1 || !done ? Achievements.Progress(def.id) + "/" + def.target : "달성";
    }

    // 부모 칸의 왼쪽/위/오른쪽/아래에서 각각 떨어진 거리로 늘리는 사각형
    private static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, Color color, TextAlignmentOptions align, bool wrap, float minSize, float maxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.GetComponent<RectTransform>().SetParent(parent, false);
        TMP_Text t = go.GetComponent<TMP_Text>();
        if (font != null) t.font = font;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = wrap;
        t.enableAutoSizing = true;
        t.fontSizeMin = minSize;
        t.fontSizeMax = Mathf.Max(minSize, maxSize);
        t.fontSize = t.fontSizeMax;
        t.overflowMode = TextOverflowModes.Truncate;
        t.raycastTarget = false;
        return t;
    }
}
