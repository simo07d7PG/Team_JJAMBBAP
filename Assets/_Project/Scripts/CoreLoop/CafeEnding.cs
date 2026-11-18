using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 엔딩일(EndingDay)의 영수증이 뜬 뒤 한 번만 나오는 축하 화면. 전체 화면 오버레이가 클릭을 가로챈다
public class CafeEnding : MonoBehaviour
{
    public const int EndingDay = 5;
    private const string SeenKey = "bari_ending_seen";
    private const string TotalKey = "bari_total_sales";
    private const float DelaySeconds = 1.5f;
    private const float ClickLockSeconds = 0.6f;

    private CafeLoop loop;
    private GameObject overlay;
    private float shownAt;
    private bool running;

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
    }

    // 판매 성공 한 번마다 누적 판매를 올린다
    public static void AddSale()
    {
        PlayerPrefs.SetInt(TotalKey, PlayerPrefs.GetInt(TotalKey, 0) + 1);
    }

    // 영수증이 뜬 직후 호출. 조건이 맞으면 1.5초 뒤 엔딩을 띄운다
    public void TryShow(int day)
    {
        if (running || overlay != null) return;
        if (day < EndingDay || PlayerPrefs.GetInt(SeenKey, 0) != 0) return;
        if (loop.instructionText == null) return;
        running = true;
        StartCoroutine(Run(day));
    }

    private IEnumerator Run(int day)
    {
        yield return new WaitForSecondsRealtime(DelaySeconds);
        if (loop.receiptPanel == null || !loop.receiptPanel.activeSelf)
        {
            running = false;
            yield break;
        }
        Build(day);
        shownAt = Time.unscaledTime;
    }

    private void Build(int day)
    {
        TMP_FontAsset font = loop.instructionText.font;
        Transform parent = loop.dayText.canvas.rootCanvas.transform;
        overlay = new GameObject("EndingOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
        UImage bg = overlay.GetComponent<UImage>();
        bg.color = new Color(0.04f, 0.02f, 0.02f, 0.88f);
        bg.raycastTarget = true;

        int total = PlayerPrefs.GetInt(TotalKey, 0);
        string sub = day + "일 동안 바리바리스타를 운영했어요";
        if (total > 0) sub += " · 누적 판매 " + total + "잔";
        MakeText(rt, "Title", "축하합니다!", font, 120f, new Color(1f, 0.82f, 0.4f, 1f), new Vector2(0f, 120f), 200f);
        MakeText(rt, "Sub", sub, font, 40f, Color.white, new Vector2(0f, -30f), 80f);
        MakeText(rt, "Hint", "계속하기 (클릭)", font, 32f, new Color(1f, 1f, 1f, 0.7f), new Vector2(0f, -200f), 60f);
    }

    private static void MakeText(RectTransform parent, string name, string text, TMP_FontAsset font, float size, Color color, Vector2 pos, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform tr = go.GetComponent<RectTransform>();
        tr.SetParent(parent, false);
        tr.anchorMin = new Vector2(0.5f, 0.5f);
        tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.pivot = new Vector2(0.5f, 0.5f);
        tr.anchoredPosition = pos;
        tr.sizeDelta = new Vector2(1600f, height);
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = font;
        t.fontSize = size;
        t.text = text;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
    }

    private void Update()
    {
        if (overlay == null) return;
        if (Time.unscaledTime - shownAt < ClickLockSeconds) return;
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Destroy(overlay);
            overlay = null;
            running = false;
        }
    }
}
