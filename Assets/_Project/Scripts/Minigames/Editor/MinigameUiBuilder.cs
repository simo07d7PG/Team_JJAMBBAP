using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BariBarista.Minigames.EditorTools
{
    /// <summary>
    /// 미니게임별 Canvas UI를 코드로 만든다(MinigameSandboxBuilder가 부른다).
    /// 미니게임마다 레이아웃이 다르다: 에스프레소는 오른쪽 세로 게이지, 얼음은 아래 칸 줄, 우유는 왼쪽 컵 단면.
    /// Canvas는 Screen Space - Overlay, sortingOrder -10, 표시 전용(GraphicRaycaster 없음).
    /// </summary>
    internal static class MinigameUiBuilder
    {
        private const string UiDir = "Assets/_Project/UI/Minigames";
        private const string WhitePath = UiDir + "/Pixel_White.png";
        private const string MouseLeftPath = UiDir + "/Mouse_Left.png";
        private const string MouseRightPath = UiDir + "/Mouse_Right.png";
        private const string MouseMovePath = UiDir + "/Mouse_Move.png";
        public const int CanvasSortingOrder = -10;

        private static Sprite white;
        private static Sprite mouseLeft;
        private static Sprite mouseRight;
        private static Sprite mouseMove;
        private static TMP_FontAsset font;
        private static Material outlineMaterial;

        // ───────────────────────── 에셋 준비 ─────────────────────────

        /// <summary>폰트와 스프라이트를 준비한다. 빌드 한 번에 한 번만 부른다.</summary>
        public static void EnsureAssets()
        {
            font = MinigameFontBuilder.Ensure();
            outlineMaterial = MinigameFontBuilder.LoadOutlineMaterial();
            if (font == null) Debug.LogError("[MinigameUi] 동적 폰트가 없어 글자가 □로 보일 수 있습니다. Tools/BariBarista/Create Galmuri Dynamic Font를 확인하세요.");

            EnsureFolder(UiDir);
            WritePng(WhitePath, SolidPixels(4, 4));
            WritePng(MouseLeftPath, MousePixels(left: true, right: false, arrows: false), 16);
            WritePng(MouseRightPath, MousePixels(left: false, right: true, arrows: false), 16);
            WritePng(MouseMovePath, MousePixels(left: false, right: false, arrows: true), 16);
            AssetDatabase.Refresh();
            white = ImportSprite(WhitePath);
            mouseLeft = ImportSprite(MouseLeftPath);
            mouseRight = ImportSprite(MouseRightPath);
            mouseMove = ImportSprite(MouseMovePath);
        }

        private static Color32[] SolidPixels(int w, int h)
        {
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            return px;
        }

        /// <summary>16x16 마우스 그림. 눌린 버튼은 버건디, 움직이기는 양옆 화살표.</summary>
        private static Color32[] MousePixels(bool left, bool right, bool arrows)
        {
            const int n = 16;
            var px = new Color32[n * n];
            var clear = new Color32(0, 0, 0, 0);
            var ink = MinigameUiPalette.Ink;
            var body = new Color32(250, 236, 205, 255);
            var press = MinigameUiPalette.Burgundy;
            for (int i = 0; i < px.Length; i++) px[i] = clear;

            int x0 = 4, x1 = 11, y0 = 1, y1 = 14;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    bool corner = (x == x0 || x == x1) && (y == y0 || y == y1);
                    if (corner) continue;
                    bool edge = x == x0 || x == x1 || y == y0 || y == y1;
                    px[y * n + x] = edge ? ink : body;
                }
            }
            // 버튼 경계선: 가로 y=9, 세로 x=7..8
            for (int x = x0; x <= x1; x++) px[9 * n + x] = ink;
            for (int y = 9; y <= y1; y++) { px[y * n + 7] = ink; px[y * n + 8] = ink; }
            // 눌린 버튼
            if (left) FillRect(px, n, x0 + 1, 10, 6, y1 - 1, press);
            if (right) FillRect(px, n, 9, 10, x1 - 1, y1 - 1, press);
            if (arrows)
            {
                var a = MinigameUiPalette.Burgundy;
                // 왼쪽 화살표
                px[6 * n + 0] = a; px[6 * n + 1] = a; px[6 * n + 2] = a;
                px[7 * n + 1] = a; px[5 * n + 1] = a;
                // 오른쪽 화살표
                px[6 * n + 13] = a; px[6 * n + 14] = a; px[6 * n + 15] = a;
                px[7 * n + 14] = a; px[5 * n + 14] = a;
            }
            return px;
        }

        private static void FillRect(Color32[] px, int n, int xa, int ya, int xb, int yb, Color32 c)
        {
            for (int y = ya; y <= yb; y++)
                for (int x = xa; x <= xb; x++)
                    px[y * n + x] = c;
        }

        private static void WritePng(string assetPath, Color32[] pixels, int size = 4)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            string full = Path.GetFullPath(assetPath);
            // 내용이 같으면 쓰지 않는다(수정 시각·GUID 유지)
            if (File.Exists(full) && File.ReadAllBytes(full).Length == bytes.Length && AreEqual(File.ReadAllBytes(full), bytes)) return;
            File.WriteAllBytes(full, bytes);
        }

        private static bool AreEqual(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static Sprite ImportSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ───────────────────────── 에스프레소 샷: 오른쪽 세로 게이지 ─────────────────────────

        public static EspressoShotHud BuildEspressoUi(Transform parent)
        {
            var canvas = CreateCanvas(parent);
            var c = canvas.transform;

            // 오른쪽 세로 "샷 잔" 게이지
            var gauge = Panel("GaugePanel", c, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, 10f), new Vector2(400f, 780f));
            Label("Title", gauge, V(0f, 1f), V(1f, 1f), V(0f, -88f), V(0f, -14f), ShotRules.Title, 60f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);
            var inside = BarFrame("Bar", gauge, V(0.62f, 0.14f), V(0.88f, 0.80f), MinigameUiPalette.PaperDark);
            var fill = Stretch("Fill", inside, V(0f, 0f), V(1f, 0f));
            Fill(fill, MinigameUiPalette.Espresso);
            fill.gameObject.SetActive(false);
            var band = Stretch("Band", inside, V(-0.12f, 0.4f), V(1.12f, 0.6f));
            BandVisual(band);
            var bandLabel = Stretch("BandLabel", inside, V(-2.3f, 0.5f), V(-0.2f, 0.5f));
            Text(bandLabel, ShotRules.Release, 38f, MinigameUiPalette.Burgundy, TextAlignmentOptions.MidlineRight);
            var capacity = Stretch("Capacity", inside, V(-0.6f, 1f), V(1.6f, 1f), V(0f, 4f), V(0f, 48f));
            var capacityText = Text(capacity, "0", 36f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);
            var value = Stretch("Value", gauge, V(0f, 0f), V(1f, 0.13f));
            var valueText = Text(value, "0 ml", 68f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);

            // 구석 힌트
            var hint = Panel("Hint", c, V(0f, 0f), V(0f, 0f), new Vector2(40f, 40f), new Vector2(880f, 90f));
            var hintText = Label("Text", hint, V(0f, 0f), V(1f, 1f), V(10f, 0f), V(-10f, 0f), ShotRules.Hint, 40f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);

            // 큰 안내 (지시어 + 조작 줄)
            var guide = Panel("GuideBig", c, V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(-200f, 20f), new Vector2(1000f, 580f));
            var instruction = Label("Instruction", guide, V(0f, 1f), V(1f, 1f), V(20f, -170f), V(-20f, -24f), PresentationRules.InstructionShot, 110f, MinigameUiPalette.Burgundy, TextAlignmentOptions.Center);
            GuideRow("RowHold", guide, 0, mouseLeft, ShotRules.GuideHold);
            GuideRow("RowRelease", guide, 1, mouseLeft, ShotRules.GuideRelease);
            var cupRow = GuideRow("RowCup", guide, 2, mouseLeft, ShotRules.GuideCup);

            var banner = CreateBanner(c, new Vector2(0f, 330f));

            var hud = canvas.gameObject.AddComponent<EspressoShotHud>();
            MinigameSandboxBuilder.Configure(hud,
                ("guideBig", guide.parent.gameObject),
                ("instructionText", instruction),
                ("guideCupRow", cupRow.gameObject),
                ("hintRoot", hint.parent.gameObject),
                ("hintText", hintText),
                ("fill", fill),
                ("band", band),
                ("bandLabel", bandLabel),
                ("capacityText", capacityText),
                ("valueText", valueText),
                ("banner", banner));
            return hud;
        }

        // ───────────────────────── 얼음 퍼기: 아래 얼음 칸 줄 + 유지 파이 ─────────────────────────

        public static IceScoopHud BuildIceUi(Transform parent)
        {
            const int maxSlots = 10;
            var canvas = CreateCanvas(parent);
            var c = canvas.transform;

            // 아래 얼음 칸 줄
            var row = Panel("SlotPanel", c, V(0.5f, 0f), V(0.5f, 0f), new Vector2(-190f, 40f), new Vector2(1400f, 300f));
            Label("Title", row, V(0f, 1f), V(0f, 1f), V(30f, -92f), V(330f, -14f), IceRules.Title, 60f, MinigameUiPalette.Ink, TextAlignmentOptions.MidlineLeft);
            var countRect = Stretch("Count", row, V(1f, 1f), V(1f, 1f), V(-360f, -96f), V(-30f, -14f));
            var countText = Text(countRect, "0개", 68f, MinigameUiPalette.Ink, TextAlignmentOptions.MidlineRight);

            var slotRoots = new GameObject[maxSlots];
            var slotFills = new Image[maxSlots];
            var slotBorders = new GameObject[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                var slot = Fixed("Slot" + (i + 1), row, V(0.5f, 0.38f), V(0.5f, 0.5f), new Vector2((i - (maxSlots - 1) * 0.5f) * 128f, 0f), new Vector2(104f, 150f));
                var border = Stretch("TargetBorder", slot, V(0f, 0f), V(1f, 1f), V(-10f, -10f), V(10f, 10f));
                Fill(border, MinigameUiPalette.Burgundy);
                var frame = Stretch("Frame", slot, V(0f, 0f), V(1f, 1f));
                Fill(frame, MinigameUiPalette.Ink);
                var inner = Stretch("Fill", slot, V(0f, 0f), V(1f, 1f), V(6f, 6f), V(-6f, -6f));
                slotFills[i] = Fill(inner, MinigameUiPalette.PaperDark);
                slotRoots[i] = slot.gameObject;
                slotBorders[i] = border.gameObject;
            }

            // 오른쪽 유지 파이
            var hold = Panel("HoldPanel", c, V(1f, 0.5f), V(1f, 0.5f), new Vector2(-50f, 40f), new Vector2(320f, 380f));
            Label("Title", hold, V(0f, 1f), V(1f, 1f), V(0f, -100f), V(0f, -14f), IceRules.Hold, 60f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);
            var pieBack = Fixed("PieBack", hold, V(0.5f, 0.42f), V(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f));
            Fill(pieBack, MinigameUiPalette.Ink);
            var pieInner = Stretch("PieInner", pieBack, V(0f, 0f), V(1f, 1f), V(8f, 8f), V(-8f, -8f));
            Fill(pieInner, MinigameUiPalette.PaperDark);
            var pie = Stretch("PieFill", pieInner, V(0f, 0f), V(1f, 1f));
            var holdFill = Fill(pie, MinigameUiPalette.Gold);
            holdFill.type = Image.Type.Filled;
            holdFill.fillMethod = Image.FillMethod.Radial360;
            holdFill.fillOrigin = (int)Image.Origin360.Top;
            holdFill.fillClockwise = true;
            holdFill.fillAmount = 0f;

            // 구석 힌트 (왼쪽 위)
            var hint = Panel("Hint", c, V(0f, 1f), V(0f, 1f), new Vector2(40f, -40f), new Vector2(800f, 90f));
            Label("Text", hint, V(0f, 0f), V(1f, 1f), V(10f, 0f), V(-10f, 0f), IceRules.Hint, 40f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);

            // 큰 안내
            var guide = Panel("GuideBig", c, V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(1240f, 580f));
            var instruction = Label("Instruction", guide, V(0f, 1f), V(1f, 1f), V(20f, -170f), V(-20f, -24f), PresentationRules.InstructionIce, 110f, MinigameUiPalette.Burgundy, TextAlignmentOptions.Center);
            GuideRow("RowScoop", guide, 0, mouseLeft, IceRules.GuideScoop);
            GuideRow("RowMove", guide, 1, mouseMove, IceRules.GuideMove);
            GuideRow("RowTilt", guide, 2, mouseRight, IceRules.GuideTilt);

            var banner = CreateBanner(c, new Vector2(0f, 330f));

            var hud = canvas.gameObject.AddComponent<IceScoopHud>();
            MinigameSandboxBuilder.Configure(hud,
                ("guideBig", guide.parent.gameObject),
                ("instructionText", instruction),
                ("hintRoot", hint.parent.gameObject),
                ("slotRoots", slotRoots),
                ("slotFills", slotFills),
                ("slotTargetBorders", slotBorders),
                ("countText", countText),
                ("holdFill", holdFill),
                ("banner", banner));
            return hud;
        }

        // ───────────────────────── 우유 붓기: 왼쪽 컵 단면 ─────────────────────────

        public static MilkPourHud BuildMilkUi(Transform parent)
        {
            var canvas = CreateCanvas(parent);
            var c = canvas.transform;

            // 왼쪽 컵 단면
            var cup = Panel("CupPanel", c, V(0f, 0.5f), V(0f, 0.5f), new Vector2(50f, 0f), new Vector2(420f, 820f));
            Label("Title", cup, V(0f, 1f), V(1f, 1f), V(0f, -88f), V(0f, -14f), PourRules.Title, 60f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);
            var inside = BarFrame("CupFrame", cup, V(0.2f, 0.24f), V(0.62f, 0.84f), MinigameUiPalette.PaperDark);
            var fill = Stretch("Fill", inside, V(0f, 0f), V(1f, 0f));
            Fill(fill, MinigameUiPalette.Milk);
            fill.gameObject.SetActive(false);
            var band = Stretch("Band", inside, V(-0.1f, 0.6f), V(1.1f, 0.8f));
            BandVisual(band);
            var bandLabel = Stretch("BandLabel", inside, V(1.25f, 0.5f), V(3.4f, 0.5f));
            Text(bandLabel, PresentationRules.Target, 40f, MinigameUiPalette.Burgundy, TextAlignmentOptions.MidlineLeft);
            var percent = Stretch("Percent", cup, V(0f, 0.12f), V(1f, 0.22f));
            var percentText = Text(percent, "0%", 64f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);

            // 흘림 막대
            Label("SpillLabel", cup, V(0.03f, 0.015f), V(0.33f, 0.1f), Vector2.zero, Vector2.zero, PourRules.Spill, 40f, MinigameUiPalette.Burgundy, TextAlignmentOptions.MidlineLeft);
            var spillInside = BarFrame("SpillBar", cup, V(0.35f, 0.03f), V(0.95f, 0.09f), MinigameUiPalette.PaperDark);
            var spillFill = Stretch("SpillFill", spillInside, V(0f, 0f), V(0f, 1f));
            Fill(spillFill, MinigameUiPalette.Burgundy);

            // 구석 힌트 (오른쪽 아래)
            var hint = Panel("Hint", c, V(1f, 0f), V(1f, 0f), new Vector2(-40f, 40f), new Vector2(1180f, 90f));
            Label("Text", hint, V(0f, 0f), V(1f, 1f), V(10f, 0f), V(-10f, 0f), PourRules.Hint, 40f, MinigameUiPalette.Ink, TextAlignmentOptions.Center);

            // 큰 안내
            var guide = Panel("GuideBig", c, V(0.5f, 0.5f), V(0.5f, 0.5f), new Vector2(180f, 30f), new Vector2(1100f, 580f));
            var instruction = Label("Instruction", guide, V(0f, 1f), V(1f, 1f), V(20f, -170f), V(-20f, -24f), PresentationRules.InstructionPour, 110f, MinigameUiPalette.Burgundy, TextAlignmentOptions.Center);
            GuideRow("RowGrab", guide, 0, mouseLeft, PourRules.GuideGrab);
            GuideRow("RowTilt", guide, 1, mouseRight, PourRules.GuideTilt);
            GuideRow("RowStop", guide, 2, null, PourRules.GuideStop);

            var banner = CreateBanner(c, new Vector2(180f, 330f));

            var hud = canvas.gameObject.AddComponent<MilkPourHud>();
            MinigameSandboxBuilder.Configure(hud,
                ("guideBig", guide.parent.gameObject),
                ("instructionText", instruction),
                ("hintRoot", hint.parent.gameObject),
                ("fill", fill),
                ("band", band),
                ("bandLabel", bandLabel),
                ("percentText", percentText),
                ("spillFill", spillFill),
                ("banner", banner));
            return hud;
        }

        // ───────────────────────── 부품 ─────────────────────────

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static Canvas CreateCanvas(Transform parent)
        {
            var go = new GameObject("HudCanvas", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            // 표시 전용: GraphicRaycaster를 붙이지 않는다
            return canvas;
        }

        private static RectTransform Stretch(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
            => Stretch(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        private static RectTransform Stretch(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        private static RectTransform Fixed(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Image Fill(RectTransform rt, Color32 color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = white;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TextMeshProUGUI Text(RectTransform rt, string s, float size, Color32 color, TextAlignmentOptions align, bool outline = false)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            if (outline && outlineMaterial != null) t.fontSharedMaterial = outlineMaterial;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            t.text = s;
            return t;
        }

        private static TextMeshProUGUI Label(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, string s, float size, Color32 color, TextAlignmentOptions align)
            => Text(Stretch(name, parent, aMin, aMax, oMin, oMax), s, size, color, align);

        /// <summary>팀 UI처럼 버건디 테두리 + 베이지 종이 판. 안쪽 종이 영역을 돌려준다.</summary>
        private static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var outer = Fixed(name, parent, anchor, pivot, pos, size);
            Fill(outer, MinigameUiPalette.Burgundy);
            var inner = Stretch("Paper", outer, V(0f, 0f), V(1f, 1f), V(8f, 8f), V(-8f, -8f));
            Fill(inner, MinigameUiPalette.Paper);
            // 판 이름의 오브젝트(켜고 끄는 대상)는 바깥 테두리이고, 내용은 종이 안에 넣는다
            return inner;
        }

        /// <summary>진갈색 틀 + 안쪽 빈 영역. 안쪽 영역을 돌려준다.</summary>
        private static RectTransform BarFrame(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color32 insideColor)
        {
            var frame = Stretch(name, parent, aMin, aMax);
            Fill(frame, MinigameUiPalette.Ink);
            var inside = Stretch("Inside", frame, V(0f, 0f), V(1f, 1f), V(6f, 6f), V(-6f, -6f));
            Fill(inside, insideColor);
            return inside;
        }

        /// <summary>목표 띠: 밝은 갈색 반투명 채움 + 위아래 버건디 선.</summary>
        private static void BandVisual(RectTransform band)
        {
            var fillRect = Stretch("BandFill", band, V(0f, 0f), V(1f, 1f));
            var img = Fill(fillRect, MinigameUiPalette.BrownLight);
            var col = img.color;
            col.a = 0.6f;
            img.color = col;
            Fill(Stretch("TopLine", band, V(0f, 1f), V(1f, 1f), V(0f, -4f), V(0f, 4f)), MinigameUiPalette.Burgundy);
            Fill(Stretch("BottomLine", band, V(0f, 0f), V(1f, 0f), V(0f, -4f), V(0f, 4f)), MinigameUiPalette.Burgundy);
        }

        /// <summary>큰 안내의 한 줄: 마우스 그림(없을 수 있음) + 짧은 한글.</summary>
        private static RectTransform GuideRow(string name, Transform guide, int index, Sprite icon, string text)
        {
            float top = 190f + index * 120f;
            var row = Stretch(name, guide, V(0f, 1f), V(1f, 1f), V(36f, -(top + 110f)), V(-36f, -top));
            if (icon != null)
            {
                var iconRect = Fixed("Icon", row, V(0f, 0.5f), V(0f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
                var img = iconRect.gameObject.AddComponent<Image>();
                img.sprite = icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            var label = Stretch("Text", row, V(0f, 0f), V(1f, 1f), V(icon != null ? 130f : 10f, 0f), V(0f, 0f));
            Text(label, text, 54f, MinigameUiPalette.Ink, TextAlignmentOptions.MidlineLeft);
            return row;
        }

        /// <summary>결과 큰 글씨 자리. 외곽선 재질로 어떤 배경에서도 읽힌다. 처음엔 꺼 둔다.</summary>
        private static ResultBanner CreateBanner(Transform canvas, Vector2 pos)
        {
            var root = Fixed("ResultBanner", canvas, V(0.5f, 0.5f), V(0.5f, 0.5f), pos, new Vector2(1500f, 280f));
            var label = Text(Stretch("Label", root, V(0f, 0f), V(1f, 1f)), "0", 170f, MinigameUiPalette.ResultSuccess, TextAlignmentOptions.Center, true);
            var banner = root.gameObject.AddComponent<ResultBanner>();
            MinigameSandboxBuilder.Configure(banner, ("label", label));
            root.gameObject.SetActive(false);
            return banner;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
