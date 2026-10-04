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

        [Header("액체 색")]
        [SerializeField] private Color espressoColor = new Color(0.25f, 0.13f, 0.06f);
        [SerializeField] private Color milkColor = new Color(0.96f, 0.94f, 0.88f);
        [SerializeField] private Color waterColor = new Color(0.6f, 0.75f, 0.9f);
        [SerializeField] private Color syrupColor = new Color(0.8f, 0.5f, 0.15f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private MaterialPropertyBlock block;
        private float shownFill = -1f;

        public void SetFill(float fill01)
        {
            if (liquid == null) return;
            fill01 = Mathf.Clamp01(fill01);
            if (Mathf.Approximately(fill01, shownFill)) return;
            shownFill = fill01;
            Vector3 s = liquid.localScale;
            liquid.localScale = new Vector3(s.x, Mathf.Max(fill01, 0.0001f), s.z);
            liquid.gameObject.SetActive(fill01 > 0.001f);
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
