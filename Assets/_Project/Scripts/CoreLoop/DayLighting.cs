using UnityEngine;

// 하루 조명 단계 하나 (아침·오전·정오·오후·해질녘·저녁)
[System.Serializable]
public class LightPhase
{
    public string name;
    [Tooltip("해 높이(°). 0보다 작으면 해가 진 것")]
    public float sunPitch;
    [Tooltip("해 방위(°). 0 = 창 정면, 양수 = 오른쪽(+x)")]
    public float sunYaw;
    public Color sunColor = Color.white;
    public float sunIntensity = 1.5f;
    [Header("앰비언트 (Trilight)")]
    public Color ambientSky;
    public Color ambientEquator;
    public Color ambientGround;
    public Color fogColor;
    [Header("하늘 (BariSky)")]
    public Color zenith;
    public Color sky;
    public Color horizon;
    [Tooltip("실내 조명 세기 배율 (씬에 맞춘 값 = 1)")]
    public float lampScale = 1f;
}

// 하루 시계에 맞춰 조명을 6단계로 바꾼다. 단계 끝 무렵에만 다음 단계로 부드럽게 넘어간다
public class DayLighting : MonoBehaviour
{
    public CafeLoop cafeLoop;
    public Light sun;
    [Tooltip("실내 조명들의 부모. 시작할 때의 세기를 기준(배율 1)으로 쓴다")]
    public Transform lampsRoot;
    [Tooltip("단계 끝에서 다음 단계로 넘어가는 구간 비율 (0.3 = 단계의 마지막 30%)")]
    [Range(0f, 1f)]
    public float blend = 0.3f;
    public LightPhase[] phases;

    // 해 최소 고도(°). 이보다 낮게는 회전시키지 않는다
    private const float MinSunPitch = 1f;
    // 해 세기 배율 = clamp01(pitch / SunsetFadeDegrees + SunsetFadeOffset)
    private const float SunsetFadeDegrees = 4f;
    private const float SunsetFadeOffset = 0.25f;
    // 해 세기가 이 값보다 작으면 그림자를 끈다
    private const float ShadowOffIntensity = 0.02f;

    // 하늘 셰이더 속성 ID (매 프레임 문자열 해시를 피한다)
    private static readonly int ZenithId = Shader.PropertyToID("_ZenithColor");
    private static readonly int SkyId = Shader.PropertyToID("_SkyColor");
    private static readonly int HorizonId = Shader.PropertyToID("_HorizonColor");
    private static readonly int SunColorId = Shader.PropertyToID("_SunColor");

    private Light[] lamps;
    private float[] lampBase;
    private Material skyMaterial;

    private void Start()
    {
        lamps = lampsRoot.GetComponentsInChildren<Light>(true);
        lampBase = new float[lamps.Length];
        for (int i = 0; i < lamps.Length; i++)
        {
            lampBase[i] = lamps[i].intensity;
        }
        // 하늘 재질은 복사본을 바꾼다 (에셋이 바뀌지 않게)
        skyMaterial = new Material(RenderSettings.skybox);
        RenderSettings.skybox = skyMaterial;
    }

    private void Update()
    {
        // 하루 진행률 x 단계 수. 정수부가 단계 번호, 소수부가 단계 안 진행률
        float scaledProgress = Mathf.Clamp01(cafeLoop.DayProgress()) * phases.Length;
        int index = Mathf.Min((int)scaledProgress, phases.Length - 1);
        int next = Mathf.Min(index + 1, phases.Length - 1);
        float phaseProgress = scaledProgress - index;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - blend, 1f, phaseProgress));
        Apply(phases[index], phases[next], t);
    }

    // 해 -> 앰비언트 -> 하늘 -> 실내 조명 순. 하늘이 해의 색·세기를 읽으므로 ApplySun이 먼저여야 한다
    private void Apply(LightPhase a, LightPhase b, float t)
    {
        ApplySun(a, b, t);
        ApplyAmbient(a, b, t);
        ApplySky(a, b, t);
        ApplyLamps(a, b, t);
    }

    private void ApplySun(LightPhase a, LightPhase b, float t)
    {
        float pitch = Mathf.Lerp(a.sunPitch, b.sunPitch, t);
        sun.transform.rotation = Quaternion.Euler(Mathf.Max(pitch, MinSunPitch), Mathf.Lerp(a.sunYaw, b.sunYaw, t), 0f);
        sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
        // 해가 지평선 아래면 끄고 그림자도 그리지 않는다
        sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t) * Mathf.Clamp01(pitch / SunsetFadeDegrees + SunsetFadeOffset);
        sun.shadows = sun.intensity < ShadowOffIntensity ? LightShadows.None : LightShadows.Soft;
    }

    private void ApplyAmbient(LightPhase a, LightPhase b, float t)
    {
        RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
        RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);
        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
    }

    private void ApplySky(LightPhase a, LightPhase b, float t)
    {
        skyMaterial.SetColor(ZenithId, Color.Lerp(a.zenith, b.zenith, t));
        skyMaterial.SetColor(SkyId, Color.Lerp(a.sky, b.sky, t));
        skyMaterial.SetColor(HorizonId, Color.Lerp(a.horizon, b.horizon, t));
        skyMaterial.SetColor(SunColorId, sun.color * Mathf.Clamp01(sun.intensity));
    }

    private void ApplyLamps(LightPhase a, LightPhase b, float t)
    {
        float scale = Mathf.Lerp(a.lampScale, b.lampScale, t);
        for (int i = 0; i < lamps.Length; i++)
        {
            lamps[i].intensity = lampBase[i] * scale;
        }
    }
}
