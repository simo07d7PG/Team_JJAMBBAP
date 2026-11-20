using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

// 미니게임 첫 진입 설명 카드. 화면 가운데 카드 + 뒤의 반투명 가림막, 조작 아이콘은 코드로 그린다.
// 닫는 입력은 CafeLoop가 ClosePressed로 확인한다 (이 클래스는 그리기와 애니메이션만 맡는다)
public class MinigameGuideCard : MonoBehaviour
{
    private const float CardWidth = 1040f;
    private const float CardHeight = 720f;
    private const float PopSeconds = 0.15f;
    private const float CellWidth = 300f;
    // 순서 목록 아래 요약 줄의 아이콘 배율 / 순서 목록이 없을 때(기존 방식)의 배율과 위치
    private const float SummaryScale = 0.8f;
    private const float SummaryY = -100f;
    private const float FallbackScale = 1.2f;
    private const float FallbackY = 40f;

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color PanelColor = new Color(0.12f, 0.08f, 0.06f, 0.94f);
    private static readonly Color BorderColor = new Color(0.85f, 0.62f, 0.32f, 1f);
    private static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 1f);
    private static readonly Color TextColor = new Color(1f, 0.95f, 0.85f, 1f);
    private static readonly Color SubColor = new Color(0.8f, 0.72f, 0.62f, 1f);
    private static readonly Color BodyColor = new Color(0.22f, 0.17f, 0.14f, 1f);
    private static readonly Color IdleButton = new Color(0.5f, 0.45f, 0.42f, 1f);

    private static Sprite roundedSprite;

    private CafeLoop loop;
    private TMP_Text fontSource;
    private RectTransform parent;
    private RectTransform root;
    private RectTransform card;
    private RectTransform controlsRoot;
    private TMP_Text titleText;
    private TMP_Text goalText;
    private TMP_Text stepsHeaderText;
    private TMP_Text stepsText;
    private TMP_Text successText;
    private TMP_Text failText;
    private TMP_Text tipText;
    private TMP_Text footerText;
    private RectTransform hint;
    private float popTimer = PopSeconds;

    public bool Ready { get { return root != null; } }
    public bool IsOpen { get { return root != null && root.gameObject.activeSelf; } }

    public void Init(CafeLoop cafeLoop)
    {
        loop = cafeLoop;
        fontSource = loop.instructionText;
        if (loop.effects != null && loop.effects.fadeImage != null) parent = loop.effects.fadeImage.transform.parent as RectTransform;
        else if (loop.dayText != null && loop.dayText.canvas != null) parent = loop.dayText.canvas.rootCanvas.transform as RectTransform;
        if (parent == null || fontSource == null) return;
        Sprite panelSprite = null;
        UImage.Type panelType = UImage.Type.Simple;
        if (loop.receiptBox != null)
        {
            UImage src = loop.receiptBox.GetComponent<UImage>();
            if (src != null && src.sprite != null)
            {
                panelSprite = src.sprite;
                panelType = src.sprite.border.sqrMagnitude > 0f ? UImage.Type.Sliced : UImage.Type.Simple;
            }
        }

        // 가림막 (클릭을 막는 것은 이것뿐)
        GameObject rootGo = new GameObject("GuideCardRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        root = rootGo.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        UImage dim = rootGo.GetComponent<UImage>();
        dim.color = DimColor;
        dim.raycastTarget = true;

        UImage cardImg = NewImage(root, "Card", Vector2.zero, new Vector2(CardWidth, CardHeight), PanelColor, false);
        card = cardImg.rectTransform;
        if (panelSprite != null)
        {
            cardImg.sprite = panelSprite;
            cardImg.type = panelType;
        }
        Outline outline = cardImg.gameObject.AddComponent<Outline>();
        outline.effectColor = BorderColor;
        outline.effectDistance = new Vector2(3f, -3f);

        // 위에서부터: 제목 / 목표 / '이렇게 해요' 머리말 / 순서 목록 / 조작 아이콘 줄 / 성공 / 실패 / 팁 / 시작 안내 (카드 1040x720, 가운데 0)
        titleText = NewText(card, "Title", new Vector2(0f, 295f), new Vector2(960f, 70f), 52f, Gold, TextAlignmentOptions.Center);
        goalText = NewText(card, "Goal", new Vector2(0f, 232f), new Vector2(960f, 48f), 34f, TextColor, TextAlignmentOptions.Center);
        stepsHeaderText = NewText(card, "StepsHeader", new Vector2(-480f, 190f), new Vector2(300f, 28f), 24f, Gold, TextAlignmentOptions.Left);
        stepsHeaderText.rectTransform.pivot = new Vector2(0f, 0.5f);
        stepsHeaderText.text = "이렇게 해요";
        stepsText = NewText(card, "Steps", new Vector2(0f, 77f), new Vector2(960f, 190f), 28f, TextColor, TextAlignmentOptions.TopLeft);
        stepsText.lineSpacing = 20f;
        controlsRoot = NewImage(card, "Controls", new Vector2(0f, -100f), new Vector2(960f, 136f), Color.clear, false).rectTransform;
        successText = NewText(card, "Success", new Vector2(0f, -205f), new Vector2(960f, 36f), 26f, TextColor, TextAlignmentOptions.Center);
        failText = NewText(card, "Fail", new Vector2(0f, -243f), new Vector2(960f, 36f), 26f, TextColor, TextAlignmentOptions.Center);
        tipText = NewText(card, "Tip", new Vector2(0f, -287f), new Vector2(960f, 34f), 22f, SubColor, TextAlignmentOptions.Center);
        footerText = NewText(card, "Footer", new Vector2(0f, -327f), new Vector2(960f, 36f), 26f, Gold, TextAlignmentOptions.Center);
        // 줄이 길면 줄바꿈 대신 글자를 줄여 칸 안에 맞춘다
        FitText(titleText, 32f, 52f);
        FitText(goalText, 22f, 34f);
        FitText(stepsText, 20f, 28f);
        FitText(successText, 18f, 26f);
        FitText(failText, 18f, 26f);
        FitText(tipText, 16f, 22f);

        // 플레이 중 구석 힌트
        TMP_Text hintText = NewText(parent, "GuideTabHint", Vector2.zero, new Vector2(420f, 30f), 22f, new Color(1f, 0.95f, 0.85f, 0.85f), TextAlignmentOptions.BottomRight);
        hint = hintText.rectTransform;
        hint.anchorMin = new Vector2(1f, 0f);
        hint.anchorMax = new Vector2(1f, 0f);
        hint.pivot = new Vector2(1f, 0f);
        hint.anchoredPosition = new Vector2(-24f, 16f);
        hintText.text = "Tab 설명 다시 보기";
        hint.gameObject.SetActive(false);

        root.gameObject.SetActive(false);
    }

    // 카드를 연다. canStart=true면 시작 전 안내, false면 플레이 중 다시 보기
    public void Show(MinigameGuide.Entry entry, bool canStart)
    {
        if (root == null || entry == null) return;
        titleText.text = entry.title;
        goalText.text = entry.goal;
        successText.text = "<color=#6FE06F>●</color> 성공  " + entry.success;
        failText.text = "<color=#FF6B6B>●</color> 실패  " + entry.fail;
        tipText.text = string.IsNullOrEmpty(entry.tip) ? "" : "<color=#FFD25A>TIP</color>  " + entry.tip;
        footerText.text = canStart ? "클릭하면 시작" : "클릭하거나 Tab을 누르면 닫기";
        // 순서 목록이 있으면 목록 아래에 아이콘을 작게, 없으면 기존처럼 아이콘을 크게 보여 준다
        bool hasSteps = entry.steps != null && entry.steps.Length > 0;
        stepsHeaderText.gameObject.SetActive(hasSteps);
        stepsText.gameObject.SetActive(hasSteps);
        if (hasSteps) stepsText.text = BuildStepsText(entry.steps);
        BuildControls(entry.controls, hasSteps ? SummaryScale : FallbackScale, hasSteps ? SummaryY : FallbackY);
        root.gameObject.SetActive(true);
        PlaceBelowFade(root);
        popTimer = 0f;
        card.localScale = Vector3.one * 0.85f;
    }

    public void Hide()
    {
        if (root != null) root.gameObject.SetActive(false);
    }

    // 플레이 중 'Tab 설명 다시 보기' 힌트
    public void SetPlayHint(bool on)
    {
        if (hint == null || hint.gameObject.activeSelf == on) return;
        hint.gameObject.SetActive(on);
        if (on) PlaceBelowFade(hint);
    }

    // 카드를 닫는 입력: 좌클릭·Space·Enter (allowTab이면 Tab도)
    public bool ClosePressed(bool allowTab)
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;
        if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) return true;
        return allowTab && kb.tabKey.wasPressedThisFrame;
    }

    private void Update()
    {
        if (!IsOpen) return;
        // 일시정지(timeScale 0) 중에도 돌도록 unscaled 시간을 쓴다
        if (popTimer < PopSeconds)
        {
            popTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popTimer / PopSeconds);
            card.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - (1f - t) * (1f - t));
        }
        Color c = footerText.color;
        c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
        footerText.color = c;
    }

    // 페이드 바로 아래로 (다른 배너·알림 위, 페이드 아래)
    private void PlaceBelowFade(RectTransform rt)
    {
        if (loop == null || loop.effects == null || loop.effects.fadeImage == null) return;
        Transform fade = loop.effects.fadeImage.transform;
        if (fade.parent != rt.parent) return;
        int fi = fade.GetSiblingIndex();
        rt.SetSiblingIndex(rt.GetSiblingIndex() < fi ? fi - 1 : fi);
    }

    // ---- 조작 줄 ----

    // 글자가 칸을 넘으면 줄바꿈하고, 그래도 넘치면 글자 크기를 줄인다
    private static void FitText(TMP_Text t, float minSize, float maxSize)
    {
        t.enableWordWrapping = true;
        t.enableAutoSizing = true;
        t.fontSizeMin = minSize;
        t.fontSizeMax = maxSize;
        t.overflowMode = TextOverflowModes.Truncate;
    }

    // 순서 줄 앞의 '1.' 번호를 금색으로 칠해 눈에 띄게 한다
    private static string BuildStepsText(string[] steps)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < steps.Length; i++)
        {
            string line = steps[i] ?? "";
            int dot = line.IndexOf('.');
            if (dot > 0 && dot <= 2) sb.Append("<color=#FFD25A>").Append(line, 0, dot + 1).Append("</color>").Append(line, dot + 1, line.Length - dot - 1);
            else sb.Append(line);
            if (i < steps.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    private void BuildControls(MinigameGuide.Control[] controls, float scale, float y)
    {
        for (int i = controlsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(controlsRoot.GetChild(i).gameObject);
        }
        controlsRoot.anchoredPosition = new Vector2(0f, y);
        if (controls == null) return;
        int n = Mathf.Min(controls.Length, 3);
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) * 0.5f) * CellWidth * scale;
            RectTransform cell = NewImage(controlsRoot, "Cell" + i, new Vector2(x, 0f), new Vector2(CellWidth - 10f, 170f), Color.clear, false).rectTransform;
            cell.localScale = Vector3.one * scale;
            DrawIcon(cell, controls[i].icon);
            TMP_Text cap = NewText(cell, "Caption", new Vector2(0f, -55f), new Vector2(CellWidth - 10f, 56f), 24f, TextColor, TextAlignmentOptions.Top);
            cap.enableWordWrapping = true;
            cap.text = controls[i].caption;
        }
    }

    private void DrawIcon(RectTransform cell, string token)
    {
        Vector2 pos = new Vector2(0f, 38f);
        switch (token)
        {
            case "LMB": DrawMouse(cell, pos, true, false, false, false); break;
            case "LMB_HOLD": DrawMouse(cell, pos, true, false, false, true); break;
            case "RMB": DrawMouse(cell, pos, false, true, false, false); break;
            case "RMB_HOLD": DrawMouse(cell, pos, false, true, false, true); break;
            case "WHEEL": DrawMouse(cell, pos, false, false, true, false); break;
            case "MOUSE_MOVE": DrawSymbol(cell, pos, "← →"); break;
            case "MOUSE_CIRCLE": DrawSymbol(cell, pos, "→ ↓ ← ↑"); break;
            case "MOUSE_VERT": DrawSymbol(cell, pos, "↑ ↓"); break;
            case "SPACE": DrawKey(cell, pos, "Space", 190f); break;
            case "KEY_Q": DrawKey(cell, pos, "Q", 64f); break;
            case "KEY_W": DrawKey(cell, pos, "W", 64f); break;
            case "KEY_E": DrawKey(cell, pos, "E", 64f); break;
            case "KEY_R": DrawKey(cell, pos, "R", 64f); break;
            case "KEY_QWER":
                string[] keys = { "Q", "W", "E", "R" };
                for (int i = 0; i < 4; i++)
                {
                    DrawKey(cell, pos + new Vector2((i - 1.5f) * 62f, 0f), keys[i], 56f);
                }
                break;
            default:
                // 모르는 토큰은 글자 그대로 키캡에
                string label = token ?? "";
                DrawKey(cell, pos, label, Mathf.Max(64f, label.Length * 24f + 32f));
                break;
        }
    }

    private void DrawSymbol(RectTransform cell, Vector2 pos, string symbol)
    {
        TMP_Text t = NewText(cell, "Symbol", pos, new Vector2(CellWidth - 20f, 90f), 64f, Gold, TextAlignmentOptions.Center);
        t.text = symbol;
        // 긴 기호(시계 방향 화살표 4개)는 칸 폭에 맞게 줄인다
        t.enableAutoSizing = true; t.fontSizeMin = 24f; t.fontSizeMax = 64f; t.enableWordWrapping = false;
    }

    private void DrawMouse(RectTransform cell, Vector2 pos, bool left, bool right, bool wheel, bool hold)
    {
        UImage body = NewImage(cell, "Mouse", pos + new Vector2(0f, 4f), new Vector2(64f, 84f), BodyColor, true);
        Outline ol = body.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.85f, 0.62f, 0.32f, 0.6f);
        ol.effectDistance = new Vector2(2f, -2f);
        NewImage(body.rectTransform, "Left", new Vector2(-14f, 16f), new Vector2(26f, 32f), left ? Gold : IdleButton, false);
        NewImage(body.rectTransform, "Right", new Vector2(14f, 16f), new Vector2(26f, 32f), right ? Gold : IdleButton, false);
        NewImage(body.rectTransform, "Wheel", new Vector2(0f, 18f), new Vector2(9f, 20f), wheel ? Gold : BodyColor, false);
        if (hold)
        {
            TMP_Text t = NewText(cell, "Hold", pos + new Vector2(0f, -56f), new Vector2(120f, 30f), 24f, Gold, TextAlignmentOptions.Center);
            t.text = "꾹";
        }
    }

    private void DrawKey(RectTransform cell, Vector2 pos, string label, float width)
    {
        UImage baseImg = NewImage(cell, "Key", pos, new Vector2(width, 60f), new Color(0.45f, 0.32f, 0.18f, 1f), true);
        UImage face = NewImage(baseImg.rectTransform, "Face", new Vector2(0f, 4f), new Vector2(width - 8f, 50f), new Color(0.96f, 0.9f, 0.76f, 1f), true);
        TMP_Text t = NewText(face.rectTransform, "Label", Vector2.zero, new Vector2(width - 12f, 50f), 30f, new Color(0.2f, 0.12f, 0.08f, 1f), TextAlignmentOptions.Center);
        t.enableWordWrapping = false;
        t.text = label;
    }

    // ---- 생성 도우미 ----

    private UImage NewImage(Transform p, string name, Vector2 pos, Vector2 size, Color color, bool rounded)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UImage));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(p, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        UImage img = go.GetComponent<UImage>();
        img.color = color;
        img.raycastTarget = false;
        if (rounded)
        {
            img.sprite = Rounded();
            img.type = UImage.Type.Sliced;
        }
        return img;
    }

    private TMP_Text NewText(Transform p, string name, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(p, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = fontSource.font;
        t.fontSharedMaterial = fontSource.fontSharedMaterial;
        t.fontSize = fontSize;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        return t;
    }

    // 둥근 사각 스프라이트 (9분할). 키캡·마우스 몸통용
    private static Sprite Rounded()
    {
        if (roundedSprite != null) return roundedSprite;
        const int size = 32;
        const float radius = 9f;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        roundedSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        return roundedSprite;
    }
}
