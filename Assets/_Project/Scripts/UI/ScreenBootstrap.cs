using UnityEngine;

// 빌드를 켰을 때 화면을 16:9 창(1600x900)으로 고정해서 시작한다. 이후 설정 화면(VideoOption)에서 바꿀 수 있다
public static class ScreenBootstrap
{
    private const int StartWidth = 1600;
    private const int StartHeight = 900;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
#if !UNITY_EDITOR
        Screen.SetResolution(StartWidth, StartHeight, FullScreenMode.Windowed);
#endif
    }
}
