using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BariBarista.Rendering.Editor
{
    /// <summary>
    /// 전역 카툰 렌더링 설정 도구. 여러 번 실행해도 결과가 같다.
    /// 1) PC_Renderer에 후처리(외곽선·명암 단계) 추가  2) 카페·미니게임 불투명 재질을 BariToon으로 바꾸기
    /// </summary>
    public static class ToonGlobalSetup
    {
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string PostShaderName = "BariBarista/ToonPost";
        const string ToonShaderName = "BariBarista/Toon";
        const string PostMaterialPath = "Assets/_Project/Art/Materials/Toon/ToonPost.mat";
        const string FeatureName = "ToonPost";

        static readonly string[] MaterialFolders =
        {
            "Assets/_Project/Art/Cafe/Materials",
            "Assets/_Project/Prefabs/Minigames/Materials",
        };

        [MenuItem("Tools/BariBarista/Toon/전역 적용 (후처리 + 재질)")]
        public static void ApplyAll()
        {
            AddPostFeature();
            ConvertMaterials();
        }

        [MenuItem("Tools/BariBarista/Toon/후처리만 추가")]
        public static void AddPostFeature()
        {
            var postShader = Shader.Find(PostShaderName);
            if (postShader == null) { Debug.LogError("[Toon] 셰이더를 찾지 못했어요: " + PostShaderName); return; }

            var material = AssetDatabase.LoadAssetAtPath<Material>(PostMaterialPath);
            if (material == null)
            {
                material = new Material(postShader) { name = "ToonPost" };
                AssetDatabase.CreateAsset(material, PostMaterialPath);
            }
            else if (material.shader != postShader)
            {
                material.shader = postShader;
            }

            var data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererPath);
            if (data == null) { Debug.LogError("[Toon] 렌더러를 찾지 못했어요: " + RendererPath); return; }

            foreach (var existing in data.rendererFeatures)
            {
                if (existing is FullScreenPassRendererFeature old && old.name == FeatureName)
                {
                    Configure(old, material);
                    EditorUtility.SetDirty(old);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[Toon] 후처리가 이미 있어서 설정만 갱신했어요.");
                    return;
                }
            }

            var feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            feature.name = FeatureName;
            Configure(feature, material);
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            var so = new SerializedObject(data);
            var features = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            Debug.Log("[Toon] PC_Renderer에 후처리를 추가했어요.");
        }

        static void Configure(FullScreenPassRendererFeature feature, Material material)
        {
            feature.passMaterial = material;
            feature.passIndex = 0;
            feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
            feature.fetchColorBuffer = true;
            feature.requirements = ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            feature.bindDepthStencilAttachment = false;
        }

        [MenuItem("Tools/BariBarista/Toon/재질만 변환")]
        public static void ConvertMaterials()
        {
            var toon = Shader.Find(ToonShaderName);
            if (toon == null) { Debug.LogError("[Toon] 셰이더를 찾지 못했어요: " + ToonShaderName); return; }

            int converted = 0;
            var skipped = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", MaterialFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == toon) continue;

                string reason = SkipReason(mat);
                if (reason != null) { skipped.Add($"{Path.GetFileNameWithoutExtension(path)}({reason})"); continue; }

                Convert(mat, toon);
                EditorUtility.SetDirty(mat);
                converted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Toon] 재질 {converted}개 변환. 건너뜀 {skipped.Count}개: {string.Join(", ", skipped)}");
        }

        // 투명·발광·알파 컷아웃은 불투명 툰 셰이더로 표현할 수 없어서 그대로 둔다
        static string SkipReason(Material mat)
        {
            string shaderName = mat.shader != null ? mat.shader.name : "";
            if (!shaderName.StartsWith("Universal Render Pipeline/")) return "URP 아님";
            if (mat.HasFloat("_Surface") && mat.GetFloat("_Surface") > 0.5f) return "투명";
            if (mat.HasFloat("_AlphaClip") && mat.GetFloat("_AlphaClip") > 0.5f) return "알파 컷아웃";
            if (mat.HasColor("_EmissionColor"))
            {
                Color e = mat.GetColor("_EmissionColor");
                if (e.maxColorComponent > 0.01f && mat.IsKeywordEnabled("_EMISSION")) return "발광";
            }
            return null;
        }

        static void Convert(Material mat, Shader toon)
        {
            Color color = mat.HasColor("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            Texture map = mat.HasTexture("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            Vector2 scale = mat.HasTexture("_BaseMap") ? mat.GetTextureScale("_BaseMap") : Vector2.one;
            Vector2 offset = mat.HasTexture("_BaseMap") ? mat.GetTextureOffset("_BaseMap") : Vector2.zero;
            float smoothness = mat.HasFloat("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.3f;

            mat.shader = toon;
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", map);
            mat.SetTextureScale("_BaseMap", scale);
            mat.SetTextureOffset("_BaseMap", offset);

            // 매끈한 재질에만 하이라이트 띠를 남긴다. 림은 끈다(카페는 배경이라 조용하게).
            mat.SetFloat("_HighlightSize", smoothness >= 0.6f ? 0.1f : 0f);
            mat.SetFloat("_RimAmount", 0f);

            // 외곽선은 후처리가 그린다. 메쉬 외곽선 패스는 꺼서 그리기 비용을 아낀다.
            mat.SetFloat("_OutlineWidth", 0f);
            mat.SetShaderPassEnabled("SRPDefaultUnlit", false);
        }
    }
}
