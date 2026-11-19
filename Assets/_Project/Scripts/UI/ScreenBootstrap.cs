using UnityEngine;

// 빌드를 켰을 때 화면을 16:9 창(기본 1600x900)으로 시작한다. 모니터가 작으면 화면의 90% 안에 들어오게 줄인다.
// 이후 설정 화면(VideoOption)에서 바꿀 수 있다
public static class ScreenBootstrap
{
    private const int StartWidth = 1600;
    private const int StartHeight = 900;
    private const float MaxScreenRatio = 0.9f;

    // 마지막으로 적용한 화면 설정(빌드에서만 채워진다). SetResolution은 다음 프레임에야 Screen.*에 반영되므로
    // 로비 Start 시점에는 이 값을 "현재 설정"으로 쓴다. VideoOption이 바꿀 때도 갱신한다
    public static bool HasApplied { get; private set; }
    public static int AppliedWidth { get; private set; }
    public static int AppliedHeight { get; private set; }
    public static FullScreenMode AppliedMode { get; private set; }

    public static void MarkApplied(int width, int height, FullScreenMode mode)
    {
        HasApplied = true;
        AppliedWidth = width;
        AppliedHeight = height;
        AppliedMode = mode;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if !UNITY_EDITOR
        // 빌드는 프레임 무제한으로 돌지 않게 수직 동기화를 켠다
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        GetStartSize(out int w, out int h);
        Screen.SetResolution(w, h, FullScreenMode.Windowed);
        MarkApplied(w, h, FullScreenMode.Windowed);
#endif
    }

    // 기본 1600x900. 모니터(현재 해상도)의 90%를 넘으면 16:9를 유지한 채 줄인다
    private static void GetStartSize(out int w, out int h)
    {
        w = StartWidth;
        h = StartHeight;

        UnityEngine.Resolution cur =Screen.currentResolution;
        if (cur.width <= 0 || cur.height <= 0) return;

        float maxW = cur.width * MaxScreenRatio;
        float maxH = cur.height * MaxScreenRatio;
        if (w <= maxW && h <= maxH) return;

        float height = Mathf.Min(maxH, maxW * 9f / 16f);
        h = Mathf.Max(360, Mathf.FloorToInt(height) & ~1);
        w = (Mathf.RoundToInt(h * 16f / 9f)) & ~1;
    }
}
