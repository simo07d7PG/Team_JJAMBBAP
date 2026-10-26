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
        float p = Mathf.Clamp01(cafeLoop.DayProgress()) * phases.Length;
        int index = Mathf.Min((int)p, phases.Length - 1);
        int next = Mathf.Min(index + 1, phases.Length - 1);
        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f - blend, 1f, p - index));
        Apply(phases[index], phases[next], t);
    }

    private void Apply(LightPhase a, LightPhase b, float t)
    {
        float pitch = Mathf.Lerp(a.sunPitch, b.sunPitch, t);
        sun.transform.rotation = Quaternion.Euler(Mathf.Max(pitch, 1f), Mathf.Lerp(a.sunYaw, b.sunYaw, t), 0f);
        sun.color = Color.Lerp(a.sunColor, b.sunColor, t);
        // 해가 지평선 아래면 끄고 그림자도 그리지 않는다
        sun.intensity = Mathf.Lerp(a.sunIntensity, b.sunIntensity, t) * Mathf.Clamp01(pitch / 4f + 0.25f);
        sun.shadows = LightShadows.Soft;
        if (sun.intensity < 0.02f) sun.shadows = LightShadows.None;

        RenderSettings.ambientSkyColor = Color.Lerp(a.ambientSky, b.ambientSky, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(a.ambientEquator, b.ambientEquator, t);
        RenderSettings.ambientGroundColor = Color.Lerp(a.ambientGround, b.ambientGround, t);
        RenderSettings.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);

        skyMaterial.SetColor("_ZenithColor", Color.Lerp(a.zenith, b.zenith, t));
        skyMaterial.SetColor("_SkyColor", Color.Lerp(a.sky, b.sky, t));
        skyMaterial.SetColor("_HorizonColor", Color.Lerp(a.horizon, b.horizon, t));
        skyMaterial.SetColor("_SunColor", sun.color * Mathf.Clamp01(sun.intensity));

        float scale = Mathf.Lerp(a.lampScale, b.lampScale, t);
        for (int i = 0; i < lamps.Length; i++)
        {
            lamps[i].intensity = lampBase[i] * scale;
        }
    }
}
