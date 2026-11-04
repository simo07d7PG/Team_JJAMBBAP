using UnityEngine;

// 컵 모델 상태 표시(액체 높이, 색, 맨 위 층, 얼음 유무). 컵 모델과 분리된 피벗만 다룬다
public class CupVisual : MonoBehaviour
{
    [Tooltip("액체 피벗. 아래쪽이 원점, localScale.y = 높이 비율(0~1)")]
    [SerializeField] private Transform liquid;
    [SerializeField] private Renderer liquidRenderer;
    [Tooltip("기존 얼음이 있을 때 켜는 표시 오브젝트")]
    [SerializeField] private GameObject iceIndicator;
    [Tooltip("가득 찼을 때 액체 폭 배율(바닥 폭 기준). 위로 벌어진 컵이면 1보다 크게")]
    [SerializeField] private float topWidthScale = 1f;
    [Tooltip("폭이 벌어지는 곡선. 1 = 직선, 0.5 = 아래에서 빨리 벌어짐(둥근 사발)")]
    [SerializeField] private float widthCurve = 1f;

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

    // 위로 벌어지는 액체 모양(원뿔대)의 둘레 칸 수·층 수
    private const int Segments = 24;
    private const int Rings = 6;

    private MaterialPropertyBlock block;
    private float shownFill = -1f;
    // 0 = 없음, 1 = 크레마, 2 = 거품
    private int shownLayer;
    private Mesh liquidMesh;
    private Vector3[] liquidVerts;

    public void SetFill(float fill01)
    {
        fill01 = Mathf.Clamp01(fill01);
        if (Mathf.Approximately(fill01, shownFill)) return;
        shownFill = fill01;
        // 높이만 늘리고, 폭은 층마다 컵 벽을 따라가게 메시를 다시 만든다 (바닥은 늘 좁게)
        liquid.localScale = new Vector3(1f, Mathf.Max(fill01, 0.0001f), 1f);
        if (!Mathf.Approximately(topWidthScale, 1f)) UpdateLiquidMesh(fill01);
        liquid.gameObject.SetActive(fill01 > 0.001f);
        RefreshTopLayer();
    }

    // 액체 표면(원기둥 자리)에 원뿔대 메시를 넣는다. 표면의 로컬 y -1~1 = 액체 바닥~현재 높이, 반지름 0.5 = 바닥 폭
    private void UpdateLiquidMesh(float fill01)
    {
        if (liquidMesh == null)
        {
            liquidMesh = new Mesh();
            liquidMesh.name = "LiquidFrustum";
            liquidVerts = new Vector3[Segments * Rings + Segments + 1];
            int[] tris = new int[Segments * (Rings - 1) * 6 + Segments * 3];
            int n = 0;
            for (int k = 0; k < Rings - 1; k++)
            {
                for (int i = 0; i < Segments; i++)
                {
                    int a = k * Segments + i;
                    int b = k * Segments + (i + 1) % Segments;
                    int c = (k + 1) * Segments + (i + 1) % Segments;
                    int d = (k + 1) * Segments + i;
                    tris[n++] = a; tris[n++] = d; tris[n++] = c;
                    tris[n++] = a; tris[n++] = c; tris[n++] = b;
                }
            }
            // 윗면: 옆면과 따로 둔 테두리 + 가운데
            int capStart = Segments * Rings;
            int center = capStart + Segments;
            for (int i = 0; i < Segments; i++)
            {
                tris[n++] = center;
                tris[n++] = capStart + (i + 1) % Segments;
                tris[n++] = capStart + i;
            }
            liquidMesh.vertices = liquidVerts;
            liquidMesh.triangles = tris;
            liquidRenderer.GetComponent<MeshFilter>().sharedMesh = liquidMesh;
        }

        for (int k = 0; k < Rings; k++)
        {
            float t = k / (float)(Rings - 1);
            float r = 0.5f * WidthAt(fill01 * t);
            float y = t * 2f - 1f;
            for (int i = 0; i < Segments; i++)
            {
                float ang = i * Mathf.PI * 2f / Segments;
                liquidVerts[k * Segments + i] = new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r);
            }
        }
        float topR = 0.5f * WidthAt(fill01);
        for (int i = 0; i < Segments; i++)
        {
            float ang = i * Mathf.PI * 2f / Segments;
            liquidVerts[Segments * Rings + i] = new Vector3(Mathf.Cos(ang) * topR, 1f, Mathf.Sin(ang) * topR);
        }
        liquidVerts[Segments * Rings + Segments] = new Vector3(0f, 1f, 0f);
        liquidMesh.vertices = liquidVerts;
        liquidMesh.RecalculateNormals();
        liquidMesh.RecalculateBounds();
    }

    // 높이 비율에서 액체 폭 배율
    private float WidthAt(float fill01)
    {
        return Mathf.Lerp(1f, topWidthScale, Mathf.Pow(fill01, widthCurve));
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
        float w = WidthAt(shownFill);
        topLayer.localScale = new Vector3(w, thickness * 0.5f / rootScaleY, w);
        topLayer.localPosition = new Vector3(0f, shownFill, 0f);
    }
}
