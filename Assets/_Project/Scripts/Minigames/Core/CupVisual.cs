using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 컵 모델 상태 표시(액체 높이, 얼음 유무). 컵 모델과 분리된 피벗만 다루므로 모델을 바꿔 끼워도 된다.
    /// </summary>
    public class CupVisual : MonoBehaviour
    {
        [Tooltip("액체 피벗. 아래쪽이 원점, localScale.y = 높이 비율(0~1)")]
        [SerializeField] private Transform liquid;
        [SerializeField] private Renderer liquidRenderer;
        [Tooltip("기존 얼음이 있을 때 켜는 표시 오브젝트")]
        [SerializeField] private GameObject iceIndicator;

        [Header("맨 위 층 (크레마·거품)")]
        [Tooltip("액체 루트 아래에 두는 얇은 원판. 액체 높이를 따라 올라온다")]
        [SerializeField] private Transform topLayer;
        [SerializeField] private Renderer topLayerRenderer;
        [SerializeField] private Material cremaMaterial;
        [SerializeField] private Material foamMaterial;
        [Tooltip("원판 두께(월드 m)")]
        [SerializeField] private float cremaThickness = 0.008f;
        [SerializeField] private float foamThickness = 0.022f;

        [Header("액체 색")]
        [SerializeField] private Color espressoColor = new Color(0.25f, 0.13f, 0.06f);
        [SerializeField] private Color milkColor = new Color(0.96f, 0.94f, 0.88f);
        [SerializeField] private Color waterColor = new Color(0.6f, 0.75f, 0.9f);
        [SerializeField] private Color syrupColor = new Color(0.8f, 0.5f, 0.15f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock block;
        private float shownFill = -1f;
        private TopLayer shownLayer = TopLayer.None;

        public TopLayer CurrentTopLayer => shownLayer;

        public void SetFill(float fill01)
        {
            if (liquid == null) return;
            fill01 = Mathf.Clamp01(fill01);
            if (Mathf.Approximately(fill01, shownFill)) return;
            shownFill = fill01;
            Vector3 s = liquid.localScale;
            liquid.localScale = new Vector3(s.x, Mathf.Max(fill01, 0.0001f), s.z);
            liquid.gameObject.SetActive(fill01 > 0.001f);
            RefreshTopLayer();
        }

        /// <summary>맨 위 층을 바꾼다. 액체가 없으면 보이지 않는다.</summary>
        public void SetTopLayer(TopLayer layer)
        {
            if (layer == shownLayer) return;
            shownLayer = layer;
            RefreshTopLayer();
        }

        private void RefreshTopLayer()
        {
            if (topLayer == null) return;
            bool show = shownLayer != TopLayer.None && shownFill > 0.001f;
            if (topLayer.gameObject.activeSelf != show) topLayer.gameObject.SetActive(show);
            if (!show) return;

            if (topLayerRenderer != null)
            {
                Material m = shownLayer == TopLayer.Foam ? foamMaterial : cremaMaterial;
                if (m != null && topLayerRenderer.sharedMaterial != m) topLayerRenderer.sharedMaterial = m;
            }
            // 액체 루트의 y 스케일이 높이(m)이므로 월드 두께로 되돌려 넣는다
            float rootScaleY = Mathf.Max(topLayer.parent != null ? topLayer.parent.localScale.y : 1f, 1e-4f);
            float thickness = shownLayer == TopLayer.Foam ? foamThickness : cremaThickness;
            Vector3 s = topLayer.localScale;
            topLayer.localScale = new Vector3(s.x, thickness * 0.5f / rootScaleY, s.z);
            topLayer.localPosition = new Vector3(0f, shownFill, 0f);
        }

        public void SetIceVisible(bool visible)
        {
            if (iceIndicator != null && iceIndicator.activeSelf != visible) iceIndicator.SetActive(visible);
        }

        /// <summary>컵 내용물 비율에 따라 액체 색을 섞는다. extraEspresso 등은 아직 Cup에 반영 전인 이번 판의 양.</summary>
        public void SetColorFrom(CupContents cup, float extraEspresso = 0f, float extraMilk = 0f)
        {
            if (liquidRenderer == null) return;
            float e = extraEspresso, m = extraMilk, w = 0f, s = 0f;
            if (cup != null)
            {
                e += cup.Get(Ingredient.Espresso);
                m += cup.Get(Ingredient.Milk);
                w = cup.Get(Ingredient.Water);
                s = cup.Get(Ingredient.Syrup);
            }
            float total = e + m + w + s;
            if (total <= 0f) return;
            SetTopLayer(LiquidLook.TopLayerFor(e, m, w, s));
            Color c = (espressoColor * e + milkColor * m + waterColor * w + syrupColor * s) / total;
            // 에스프레소는 조금만 들어가도 색이 확 바뀐다
            if (e > 0f && m > 0f) c = Color.Lerp(c, espressoColor, Mathf.Clamp01(e / total) * 0.6f);

            block ??= new MaterialPropertyBlock();
            liquidRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            liquidRenderer.SetPropertyBlock(block);
        }
    }
}
