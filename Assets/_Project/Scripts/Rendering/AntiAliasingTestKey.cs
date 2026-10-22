using UnityEngine;
using UnityEngine.InputSystem;

// 임시 테스트용: F1을 누르면 안티에일리어싱 모드를 차례로 바꾸고 왼쪽 위에 이름을 잠깐 보여 준다.
// 설정 화면에 드롭다운이 생기면 이 파일은 지운다
public class AntiAliasingTestKey : MonoBehaviour
{
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
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            int next = (AntiAliasingSettings.Current + 1) % AntiAliasingSettings.Names.Length;
            AntiAliasingSettings.Set(next);
            showTimer = 2f;
        }
        if (showTimer > 0f) showTimer -= Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        if (showTimer <= 0f) return;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(16f, 16f, 500f, 40f), "AA: " + AntiAliasingSettings.Names[AntiAliasingSettings.Current] + "  (F1)", style);
    }
}
