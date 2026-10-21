using UnityEngine;
using UnityEngine.InputSystem;

// 코어 루프 없이 미니게임을 혼자 돌려 보는 테스트 도구.
// 지시어 → 플레이(제한 시간) → 결과 → 잠시 뒤 다시. R 재시작, Tab 다음 미니게임
public class StandaloneMicrogameRunner : MonoBehaviour
{
    [Tooltip("돌려 볼 미니게임들 (씬에 둔 프리팹 인스턴스)")]
    public Minigame[] games;
    [Tooltip("지시어를 보여 주는 시간(초). 제한 시간은 세지 않는다")]
    public float instructionSeconds = 1.5f;
    [Tooltip("켜면 다시 시작해도 컵을 비우지 않는다(컵 이어 쓰기 테스트)")]
    public bool keepCup;

    // 결과 표시 시간(초)과 다시 시작까지 기다리는 시간(초)
    private const float FailSeconds = 0.9f;
    private const float SuccessSeconds = 1.4f;
    private const float RestartDelay = 1f;

    // 0 = 지시어, 1 = 플레이, 2 = 결과, 3 = 다시 시작 대기
    private int state;
    private int index;
    private float timer;
    private Cup cup = new Cup();
    private GUIStyle bigStyle;
    private GUIStyle smallStyle;

    private void Start()
    {
        for (int i = 0; i < games.Length; i++)
        {
            games[i].gameObject.SetActive(false);
        }
        Restart();
    }

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
            return;
        }
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            games[index].StopGame();
            games[index].gameObject.SetActive(false);
            index = (index + 1) % games.Length;
            Restart();
            return;
        }

        Minigame game = games[index];
        switch (state)
        {
            case 0:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    game.StartGame();
                    timer = game.timeLimit;
                    state = 1;
                }
                break;
            case 1:
                timer -= Time.deltaTime;
                if (timer <= 0f && game.isPlaying) game.TimeOver();
                if (game.isFinished)
                {
                    Debug.Log("[Runner] " + game.name + " 결과: " + (game.isSuccess ? "성공" : "실패") + " / " + game.resultText + " / 컵 에스프레소 " + cup.espresso + ", 우유 " + cup.milk + ", 얼음 " + cup.ice);
                    timer = FailSeconds;
                    if (game.isSuccess) timer = SuccessSeconds;
                    state = 2;
                }
                break;
            case 2:
                timer -= Time.deltaTime;
                // 클릭하면 결과 표시를 건너뛴다
                if (timer <= 0f || game.clickAction.action.WasPressedThisFrame())
                {
                    game.gameObject.SetActive(false);
                    timer = RestartDelay;
                    state = 3;
                }
                break;
            case 3:
                timer -= Time.deltaTime;
                if (timer <= 0f) Restart();
                break;
        }
    }

    private void Restart()
    {
        Minigame game = games[index];
        game.StopGame();
        if (!keepCup) cup = new Cup();
        game.cup = cup;
        for (int i = 0; i < games.Length; i++)
        {
            games[i].gameObject.SetActive(i == index);
        }
        timer = instructionSeconds;
        state = 0;
    }

    private void OnGUI()
    {
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label);
            bigStyle.fontSize = 64;
            bigStyle.fontStyle = FontStyle.Bold;
            bigStyle.alignment = TextAnchor.MiddleCenter;
            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 20;
        }

        Minigame game = games[index];
        Rect center = new Rect(0f, Screen.height * 0.3f, Screen.width, 120f);
        if (state == 0)
        {
            GUI.Label(center, game.instruction, bigStyle);
        }
        else if (state == 1)
        {
            GUI.Label(new Rect(20f, 14f, 600f, 30f), "남은 시간 " + Mathf.Max(0f, timer).ToString("0.0") + "초", smallStyle);
        }
        else if (state == 2)
        {
            string text = game.resultText;
            if (game.isSuccess) text = "성공!";
            GUI.Label(center, text, bigStyle);
        }

        string cupText = "컵: 에스프레소 " + cup.espresso.ToString("0") + "ml, 우유 " + cup.milk.ToString("0") + "ml, 얼음 " + cup.ice;
        if (keepCup) cupText += " (컵 이어 쓰기)";
        GUI.Label(new Rect(20f, 44f, 800f, 30f), "[" + (index + 1) + "/" + games.Length + "] " + game.stationId + "   " + cupText, smallStyle);
        GUI.Label(new Rect(20f, 74f, 800f, 30f), "R 재시작   Tab 다음 미니게임", smallStyle);
    }
}
