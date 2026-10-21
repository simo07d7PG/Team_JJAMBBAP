using System.Collections.Generic;
using UnityEngine;

// 기계. 카페 화면에서 클릭하면 같은 stationId의 미니게임이 열린다. 클릭 판정에는 Collider가 필요하다
public class Station : MonoBehaviour
{
    [Tooltip("기계 id (espresso / ice / milk 등). 미니게임의 stationId와 같게")]
    public string stationId;
    [Tooltip("끄면 마우스를 올려도 하이라이트·클릭이 안 된다")]
    public bool clickable = true;

    [Tooltip("하이라이트할 Renderer들. 비우면 자식 Renderer 전부")]
    public Renderer[] targets;
    [Tooltip("하이라이트 때 흰색 쪽으로 섞는 정도 (0~1)")]
    [Range(0f, 1f)]
    public float brightness = 0.45f;

    private List<Material> materials = new List<Material>();
    private List<Color> originalColors = new List<Color>();
    private bool highlighted;

    private void Awake()
    {
        if (targets == null || targets.Length == 0)
        {
            targets = GetComponentsInChildren<Renderer>();
        }
        for (int i = 0; i < targets.Length; i++)
        {
            // .materials는 이 Renderer 전용 머티리얼 인스턴스를 만든다
            Material[] mats = targets[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                materials.Add(mats[j]);
                originalColors.Add(GetColor(mats[j]));
            }
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        for (int i = 0; i < materials.Count; i++)
        {
            Color color = originalColors[i];
            if (on) color = Color.Lerp(originalColors[i], Color.white, brightness);
            SetColor(materials[i], color);
        }
    }

    // URP는 _BaseColor, 기본 셰이더는 _Color
    private Color GetColor(Material mat)
    {
        if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
        if (mat.HasProperty("_Color")) return mat.GetColor("_Color");
        return Color.white;
    }

    private void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
    }
}
