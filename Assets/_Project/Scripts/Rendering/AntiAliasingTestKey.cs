using UnityEngine;
using UnityEngine.InputSystem;

// 임시 테스트용: F1을 누르면 안티에일리어싱 모드를 차례로 바꾸고 왼쪽 위에 이름을 잠깐 보여 준다.
// 설정 화면에 드롭다운이 생기면 이 파일은 지운다
public class AntiAliasingTestKey : MonoBehaviour
{
    // 이름을 보여 주는 시간(초)
    private const float ShowSeconds = 2f;
    // 라벨 글자 크기
    private const int FontSize = 24;
    // 왼쪽 위 라벨 위치와 크기
    private static readonly Rect LabelRect = new Rect(16f, 16f, 500f, 40f);

    private float showTimer;
    private GUIStyle style;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("AntiAliasingTestKey");
        go.AddComponent<AntiAliasingTestKey>();
        DontDestroyOnLoad(go);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) CycleMode();
        if (showTimer > 0f) showTimer -= Time.unscaledDeltaTime;
    }

    // 다음 모드로 바꾸고 이름 표시 타이머를 시작
    private void CycleMode()
    {
        int next = (AntiAliasingSettings.Current + 1) % AntiAliasingSettings.Names.Length;
        AntiAliasingSettings.Set(next);
        showTimer = ShowSeconds;
    }

    private void OnGUI()
    {
        if (showTimer <= 0f) return;
        EnsureStyle();
        GUI.Label(LabelRect, "AA: " + AntiAliasingSettings.Names[AntiAliasingSettings.Current] + "  (F1)", style);
    }

    // 라벨 스타일을 처음 한 번만 만든다
    private void EnsureStyle()
    {
        if (style != null) return;
        style = new GUIStyle(GUI.skin.label);
        style.fontSize = FontSize;
        style.normal.textColor = Color.white;
    }
}
