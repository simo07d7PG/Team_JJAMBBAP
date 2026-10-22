using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// 안티에일리어싱 설정. 고른 값은 PlayerPrefs에 저장하고, 씬이 열릴 때마다 모든 카메라에 적용한다.
// 설정 화면에서는 Names로 목록을 만들고 Set(번호)만 부르면 된다
public static class AntiAliasingSettings
{
    public const int Off = 0;
    public const int FXAA = 1;
    public const int SMAA = 2;
    public const int TAA = 3;
    public const int MSAA2 = 4;
    public const int MSAA4 = 5;
    public const int MSAA8 = 6;

    // 설정 화면 드롭다운에 쓰는 이름 (번호 순서와 같게)
    public static readonly string[] Names =
    {
        "끄기",
        "FXAA (가벼움)",
        "SMAA (권장)",
        "TAA (부드러움)",
        "MSAA 2x",
        "MSAA 4x",
        "MSAA 8x",
    };

    private const string SaveKey = "graphics.antiAliasing";
    private const int DefaultMode = SMAA;

    public static int Current;

#if UNITY_EDITOR
    // 에디터에서는 MSAA를 바꾸면 RP 에셋 파일이 바뀌므로 Play가 끝나면 원래 값으로 돌린다
    private static int editorMsaa = -1;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Current = PlayerPrefs.GetInt(SaveKey, DefaultMode);
        if (Current < 0 || Current >= Names.Length) Current = DefaultMode;
        SceneManager.sceneLoaded += OnSceneLoaded;
#if UNITY_EDITOR
        UniversalRenderPipelineAsset asset = GetAsset();
        if (asset != null) editorMsaa = asset.msaaSampleCount;
        Application.quitting += RestoreEditorMsaa;
#endif
    }

    public static void Set(int mode)
    {
        if (mode < 0 || mode >= Names.Length) return;
        Current = mode;
        PlayerPrefs.SetInt(SaveKey, mode);
        PlayerPrefs.Save();
        Apply();
    }

    // 지금 씬의 모든 카메라(꺼진 미니게임 카메라 포함)에 적용
    public static void Apply()
    {
        AntialiasingMode cameraMode = AntialiasingMode.None;
        int msaa = 1;
        if (Current == FXAA) cameraMode = AntialiasingMode.FastApproximateAntialiasing;
        if (Current == SMAA) cameraMode = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        if (Current == TAA) cameraMode = AntialiasingMode.TemporalAntiAliasing;
        if (Current == MSAA2) msaa = 2;
        if (Current == MSAA4) msaa = 4;
        if (Current == MSAA8) msaa = 8;

        UniversalRenderPipelineAsset asset = GetAsset();
        if (asset != null) asset.msaaSampleCount = msaa;

        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            UniversalAdditionalCameraData data = cameras[i].GetUniversalAdditionalCameraData();
            data.antialiasing = cameraMode;
            data.antialiasingQuality = AntialiasingQuality.High;
            data.taaSettings.quality = TemporalAAQuality.High;
            cameras[i].allowMSAA = msaa > 1;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
    }

    private static UniversalRenderPipelineAsset GetAsset()
    {
        return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    }

#if UNITY_EDITOR
    private static void RestoreEditorMsaa()
    {
        UniversalRenderPipelineAsset asset = GetAsset();
        if (asset != null && editorMsaa > 0) asset.msaaSampleCount = editorMsaa;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.quitting -= RestoreEditorMsaa;
    }
#endif
}
