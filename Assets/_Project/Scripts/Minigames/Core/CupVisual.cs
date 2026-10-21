using UnityEngine;

// 컵 모델 상태 표시(액체 높이, 색, 맨 위 층, 얼음 유무). 컵 모델과 분리된 피벗만 다룬다
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

    // 우유가 액체의 이 비율 이상이면 거품이 맨 위를 덮는다
    private const float FoamShare = 0.2f;

    private MaterialPropertyBlock block;
    private float shownFill = -1f;
    // 0 = 없음, 1 = 크레마, 2 = 거품
    private int shownLayer;

    public void SetFill(float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);
        if (Mathf.Approximately(fill01, shownFill)) return;
        shownFill = fill01;
        Vector3 s = liquid.localScale;
        liquid.localScale = new Vector3(s.x, Mathf.Max(fill01, 0.0001f), s.z);
        liquid.gameObject.SetActive(fill01 > 0.001f);
        RefreshTopLayer();
    }

    public void SetIceVisible(bool visible)
    {
        if (iceIndicator != null) iceIndicator.SetActive(visible);
    }

    // 컵 내용물 비율로 액체 색을 섞는다. extraEspresso·extraMilk는 아직 컵에 넣기 전인 이번 판의 양
    public void SetColorFrom(Cup cup, float extraEspresso, float extraMilk)
    {
        float e = cup.espresso + extraEspresso;
        float m = cup.milk + extraMilk;
        float w = cup.water;
        float total = e + m + w;
        if (total <= 0f) return;

        int layer = 0;
        if (m / total >= FoamShare) layer = 2;
        else if (e > 0f) layer = 1;
        if (layer != shownLayer)
        {
            shownLayer = layer;
            RefreshTopLayer();
        }

        Color c = (espressoColor * e + milkColor * m + waterColor * w) / total;
        // 에스프레소는 조금만 들어가도 색이 확 바뀐다
        if (e > 0f && m > 0f) c = Color.Lerp(c, espressoColor, Mathf.Clamp01(e / total) * 0.6f);

        if (block == null) block = new MaterialPropertyBlock();
        liquidRenderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        liquidRenderer.SetPropertyBlock(block);
    }

    private void RefreshTopLayer()
    {
        if (topLayer == null) return;
        bool show = shownLayer != 0 && shownFill > 0.001f;
        topLayer.gameObject.SetActive(show);
        if (!show) return;

        Material mat = cremaMaterial;
        float thickness = cremaThickness;
        if (shownLayer == 2)
        {
            mat = foamMaterial;
            thickness = foamThickness;
        }
        topLayerRenderer.sharedMaterial = mat;

        // 액체 루트의 y 스케일이 높이(m)라서 월드 두께로 되돌려 넣는다
        float rootScaleY = Mathf.Max(topLayer.parent.localScale.y, 0.0001f);
        Vector3 s = topLayer.localScale;
        topLayer.localScale = new Vector3(s.x, thickness * 0.5f / rootScaleY, s.z);
        topLayer.localPosition = new Vector3(0f, shownFill, 0f);
    }
}
