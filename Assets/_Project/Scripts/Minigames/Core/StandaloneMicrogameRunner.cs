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

    // 화면 글자 크기·배치(픽셀)
    private const int BigFontSize = 64;
    private const int SmallFontSize = 20;
    private const float LeftMargin = 20f;
    private const float TopMargin = 14f;
    private const float LineHeight = 30f;
    private const float TimerWidth = 600f;
    private const float InfoWidth = 800f;
    private const float CenterHeight = 120f;
    private const float CenterHeightRatio = 0.3f;

    private enum RunnerState
    {
        Instruction,  // 지시어
        Playing,      // 플레이
        Result,       // 결과
        RestartWait,  // 다시 시작 대기
    }

    private RunnerState state;
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
        timer -= Time.deltaTime;
        switch (state)
        {
            case RunnerState.Instruction:
                UpdateInstruction(game);
                break;
            case RunnerState.Playing:
                UpdatePlaying(game);
                break;
            case RunnerState.Result:
                UpdateResult(game);
                break;
            case RunnerState.RestartWait:
                if (timer <= 0f) Restart();
                break;
        }
    }

    // 지시어 시간이 끝나면 게임을 시작한다
    private void UpdateInstruction(Minigame game)
    {
        if (timer > 0f) return;
        game.StartGame();
        timer = game.timeLimit;
        state = RunnerState.Playing;
    }

    // 제한 시간을 세고, 끝나면 결과를 로그로 남긴다
    private void UpdatePlaying(Minigame game)
    {
        if (timer <= 0f && game.isPlaying) game.TimeOver();
        if (!game.isFinished) return;
        Debug.Log("[Runner] " + game.name + " 결과: " + (game.isSuccess ? "성공" : "실패") + " / " + game.resultText + " / 컵 에스프레소 " + cup.espresso + ", 우유 " + cup.milk + ", 얼음 " + cup.ice);
        timer = game.isSuccess ? SuccessSeconds : FailSeconds;
        state = RunnerState.Result;
    }

    private void UpdateResult(Minigame game)
    {
        // 클릭하면 결과 표시를 건너뛴다
        if (timer <= 0f || game.clickAction.action.WasPressedThisFrame())
        {
            game.gameObject.SetActive(false);
            timer = RestartDelay;
            state = RunnerState.RestartWait;
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
        state = RunnerState.Instruction;
    }

    // 글자 스타일을 처음 한 번만 만든다
    private void EnsureStyles()
    {
        if (bigStyle != null) return;
        bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize = BigFontSize;
        bigStyle.fontStyle = FontStyle.Bold;
        bigStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = SmallFontSize;
    }

    private void OnGUI()
    {
        EnsureStyles();

        Minigame game = games[index];
        Rect center = new Rect(0f, Screen.height * CenterHeightRatio, Screen.width, CenterHeight);
        if (state == RunnerState.Instruction)
        {
            GUI.Label(center, game.instruction, bigStyle);
        }
        else if (state == RunnerState.Playing)
        {
            GUI.Label(new Rect(LeftMargin, TopMargin, TimerWidth, LineHeight), "남은 시간 " + Mathf.Max(0f, timer).ToString("0.0") + "초", smallStyle);
        }
        else if (state == RunnerState.Result)
        {
            string text = game.resultText;
            if (game.isSuccess) text = "성공!";
            GUI.Label(center, text, bigStyle);
        }

        string cupText = "컵: 에스프레소 " + cup.espresso.ToString("0") + "ml, 우유 " + cup.milk.ToString("0") + "ml, 얼음 " + cup.ice;
        if (keepCup) cupText += " (컵 이어 쓰기)";
        GUI.Label(new Rect(LeftMargin, TopMargin + LineHeight, InfoWidth, LineHeight), "[" + (index + 1) + "/" + games.Length + "] " + game.stationId + "   " + cupText, smallStyle);
        GUI.Label(new Rect(LeftMargin, TopMargin + LineHeight * 2f, InfoWidth, LineHeight), "R 재시작   Tab 다음 미니게임", smallStyle);
    }
}
