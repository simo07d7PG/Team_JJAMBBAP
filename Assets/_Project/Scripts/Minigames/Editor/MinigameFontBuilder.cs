using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace BariBarista.Minigames.EditorTools
{
    /// <summary>
    /// Tools/BariBarista/Create Galmuri Dynamic Font — 미니게임 UI 전용 동적 TMP 폰트를 만든다.
    /// 기존 Galmuri SDF 에셋과 TMP Settings는 건드리지 않고 새 에셋만 만든다. 이미 있으면 다시 만들지 않는다(GUID 유지).
    /// 미리 넣을 글자는 PresentationRules.PreloadCharacters에서 가져온다.
    /// </summary>
    public static class MinigameFontBuilder
    {
        public const string FontDir = "Assets/_Project/UI/Fonts";
        public const string SdfPath = FontDir + "/Galmuri11-Bold_Dynamic SDF.asset";
        public const string OutlineMaterialPath = FontDir + "/Galmuri11-Bold_Dynamic SDF Outline.mat";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/Galmuri11-Bold.ttf";
        private const int SamplingPointSize = 48;
        private const int AtlasPadding = 6;
        private const int AtlasSize = 2048;

        [MenuItem("Tools/BariBarista/Create Galmuri Dynamic Font")]
        public static void CreateMenu() => Ensure();

        /// <summary>폰트 에셋과 외곽선 재질을 돌려준다. 없으면 만든다.</summary>
        public static TMP_FontAsset Ensure()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SdfPath);
            if (font == null) font = Create();
            if (font != null) EnsureOutlineMaterial(font);
            return font;
        }

        public static Material LoadOutlineMaterial() => AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);

        private static TMP_FontAsset Create()
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
            {
                Debug.LogError($"[MinigameFont] 원본 폰트를 찾지 못했습니다: {SourceFontPath}");
                return null;
            }

            EnsureFolder(FontDir);
            var asset = TMP_FontAsset.CreateFontAsset(source, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogError("[MinigameFont] 동적 폰트 에셋을 만들지 못했습니다.");
                return null;
            }

            asset.name = "Galmuri11-Bold_Dynamic SDF";
            AssetDatabase.CreateAsset(asset, SdfPath);
            // 아틀라스 텍스처와 재질은 하위 에셋으로 저장
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                asset.atlasTextures[i].name = i == 0 ? asset.name + " Atlas" : asset.name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
            }
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            PreloadAndReport(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[MinigameFont] 생성 완료: {SdfPath}");
            return asset;
        }

        /// <summary>미리 넣을 글자를 아틀라스에 넣고 빠진 글자를 로그로 남긴다.</summary>
        private static void PreloadAndReport(TMP_FontAsset asset)
        {
            bool ok = asset.TryAddCharacters(PresentationRules.PreloadCharacters, out string missing);
            if (!ok && !string.IsNullOrEmpty(missing))
                Debug.LogError($"[MinigameFont] 원본 폰트에 없는 글자: \"{missing}\"");
        }

        private static void EnsureOutlineMaterial(TMP_FontAsset font)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath) != null) return;
            var mat = new Material(font.material) { name = "Galmuri11-Bold_Dynamic SDF Outline" };
            mat.EnableKeyword("OUTLINE_ON");
            mat.SetFloat("_OutlineWidth", 0.22f);
            mat.SetColor("_OutlineColor", MinigameUiPalette.InkDeep);
            AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
            AssetDatabase.SaveAssets();
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
