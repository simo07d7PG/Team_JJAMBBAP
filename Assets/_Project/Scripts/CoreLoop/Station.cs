using System.Collections;
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

    [Header("하이라이트 테두리 (BariToon 머티리얼만)")]
    public Color outlineColor = new Color(1f, 0.82f, 0.35f, 1f);
    [Tooltip("테두리 두께 (화면 비율)")]
    [Range(0f, 0.02f)]
    public float outlineWidth = 0.006f;

    private const string OutlineWidthProp = "_OutlineWidth";
    private const string OutlineColorProp = "_OutlineColor";
    private const string OutlineUsePositionProp = "_OutlineUsePosition";
    private const string OutlinePass = "SRPDefaultUnlit";
    private const string BaseColorProp = "_BaseColor";
    private const string ColorProp = "_Color";

    // 깜빡임: 시작 대기, 켜짐·꺼짐 간격(초), 횟수
    private const float BlinkStartDelay = 0.3f;
    private const float BlinkInterval = 0.15f;
    private const int BlinkCount = 2;

    // 머티리얼 하나의 원래 상태 (하이라이트를 끄면 이 값으로 되돌린다)
    private struct MaterialState
    {
        public Material mat;
        public bool hasOutline;
        public Color color;
        public Color outlineColor;
        public float outlineWidth;
        public float outlineUsePosition;
        public bool outlinePass;
    }

    private readonly List<MaterialState> states = new List<MaterialState>();
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
                Material mat = mats[j];
                MaterialState state = new MaterialState();
                state.mat = mat;
                state.color = GetColor(mat);
                state.hasOutline = mat.HasProperty(OutlineWidthProp);
                state.outlinePass = mat.GetShaderPassEnabled(OutlinePass);
                if (state.hasOutline)
                {
                    state.outlineColor = mat.GetColor(OutlineColorProp);
                    state.outlineWidth = mat.GetFloat(OutlineWidthProp);
                    state.outlineUsePosition = mat.GetFloat(OutlineUsePositionProp);
                }
                else
                {
                    state.outlineColor = Color.black;
                }
                states.Add(state);
            }
        }
    }

    public void SetHighlight(bool on)
    {
        if (highlighted == on) return;
        highlighted = on;
        Apply(on);
    }

    // 다음 차례가 됐을 때 두 번 깜빡여 "여기"를 알린다. 끝나면 원래 하이라이트 상태로
    public void Blink()
    {
        StopAllCoroutines();
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        yield return new WaitForSeconds(BlinkStartDelay);
        WaitForSeconds wait = new WaitForSeconds(BlinkInterval);
        for (int i = 0; i < BlinkCount; i++)
        {
            Apply(true);
            yield return wait;
            Apply(false);
            yield return wait;
        }
        Apply(highlighted);
    }

    private void Apply(bool on)
    {
        for (int i = 0; i < states.Count; i++)
        {
            MaterialState state = states[i];
            SetColor(state.mat, on ? Color.Lerp(state.color, Color.white, brightness) : state.color);
            if (state.hasOutline) ApplyOutline(state, on);
        }
    }

    // 테두리를 켜거나(하이라이트 값) 원래 값으로 되돌린다
    private void ApplyOutline(MaterialState state, bool on)
    {
        Material mat = state.mat;
        if (on)
        {
            // 기본 도형(큐브·원기둥)은 위치 방향으로 부풀려야 모서리에 틈이 안 생긴다
            mat.SetColor(OutlineColorProp, outlineColor);
            mat.SetFloat(OutlineWidthProp, outlineWidth);
            mat.SetFloat(OutlineUsePositionProp, 1f);
            mat.SetShaderPassEnabled(OutlinePass, true);
        }
        else
        {
            mat.SetColor(OutlineColorProp, state.outlineColor);
            mat.SetFloat(OutlineWidthProp, state.outlineWidth);
            mat.SetFloat(OutlineUsePositionProp, state.outlineUsePosition);
            mat.SetShaderPassEnabled(OutlinePass, state.outlinePass);
        }
    }

    // URP는 _BaseColor, 기본 셰이더는 _Color
    private static Color GetColor(Material mat)
    {
        if (mat.HasProperty(BaseColorProp)) return mat.GetColor(BaseColorProp);
        if (mat.HasProperty(ColorProp)) return mat.GetColor(ColorProp);
        return Color.white;
    }

    private static void SetColor(Material mat, Color color)
    {
        if (mat.HasProperty(BaseColorProp)) mat.SetColor(BaseColorProp, color);
        if (mat.HasProperty(ColorProp)) mat.SetColor(ColorProp, color);
    }
}
