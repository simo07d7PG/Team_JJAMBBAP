using TMPro;
using UnityEngine;

// 코어 루프 관리. 기계를 클릭하면 같은 stationId의 미니게임을 열고, 끝나면 카페 화면으로 돌아온다
public class CafeLoop : MonoBehaviour
{
    [Tooltip("씬에 꺼 둔 미니게임들 (카페 밖 먼 곳)")]
    public Minigame[] minigames;
    public CafeView view;
    public Camera cafeCamera;

    [Header("글자 (코어 루프 Canvas)")]
    public TMP_Text instructionText;
    public TMP_Text timeText;
    public TMP_Text resultText;
    [Tooltip("지시어를 보여 주는 시간(초). 이 동안 제한 시간은 세지 않는다")]
    public float instructionSeconds = 1.5f;
    [Tooltip("실패 결과를 보여 주는 시간(초)")]
    public float failSeconds = 0.9f;
    [Tooltip("성공 결과를 보여 주는 시간(초)")]
    public float successSeconds = 1.4f;

    // 0 = 카페, 1 = 지시어, 2 = 미니게임 플레이, 3 = 결과
    private int state;
    private Minigame current;
    private float timer;
    private int returnFrame = -10;

    private void Start()
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            minigames[i].gameObject.SetActive(false);
        }
        ShowTexts();
        state = 0;
    }

    private void Update()
    {
        switch (state)
        {
            case 0:
                if (view.clickedStation != null)
                {
                    Minigame game = FindMinigame(view.clickedStation.stationId);
                    view.clickedStation = null;
                    // 결과를 건너뛴 클릭이 바로 기계 클릭으로 이어지지 않게
                    if (game != null && Time.frameCount > returnFrame + 1) OpenMinigame(game);
                }
                break;
            case 1:
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    current.StartGame();
                    timer = current.timeLimit;
                    state = 2;
                    ShowTexts();
                    Debug.Log("[CafeLoop] 미니게임 시작: " + current.stationId);
                }
                break;
            case 2:
                timer -= Time.deltaTime;
                if (timer <= 0f && current.isPlaying) current.TimeOver();
                timeText.text = "남은 시간 " + Mathf.Max(0f, timer).ToString("0.0") + "초";
                if (current.isFinished)
                {
                    Debug.Log("[CafeLoop] " + current.stationId + " 결과: " + (current.isSuccess ? "성공" : "실패") + " / " + current.resultText);
                    timer = failSeconds;
                    if (current.isSuccess) timer = successSeconds;
                    state = 3;
                    ShowTexts();
                }
                break;
            case 3:
                timer -= Time.deltaTime;
                // 클릭하면 결과 표시를 건너뛴다
                if (timer <= 0f || current.clickAction.action.WasPressedThisFrame())
                {
                    CloseMinigame();
                }
                break;
        }
    }

    private Minigame FindMinigame(string stationId)
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            if (minigames[i].stationId == stationId) return minigames[i];
        }
        Debug.LogWarning("[CafeLoop] 미니게임 없음: " + stationId);
        return null;
    }

    private void OpenMinigame(Minigame game)
    {
        current = game;
        view.viewEnabled = false;
        game.gameObject.SetActive(true);
        // 카메라 컴포넌트만 끈다 (AudioListener는 카페 카메라에 남김)
        cafeCamera.enabled = false;
        game.viewCamera.enabled = true;
        game.cup = new Cup();
        timer = instructionSeconds;
        state = 1;
        ShowTexts();
    }

    private void CloseMinigame()
    {
        current.StopGame();
        current.gameObject.SetActive(false);
        current = null;
        cafeCamera.enabled = true;
        view.viewEnabled = true;
        returnFrame = Time.frameCount;
        state = 0;
        ShowTexts();
        Debug.Log("[CafeLoop] 카페 복귀");
    }

    // 상태에 맞는 글자만 켠다
    private void ShowTexts()
    {
        instructionText.gameObject.SetActive(state == 1);
        timeText.gameObject.SetActive(state == 2);
        resultText.gameObject.SetActive(state == 3);
        if (state == 1) instructionText.text = current.instruction;
        if (state == 2) timeText.text = "남은 시간 " + current.timeLimit.ToString("0.0") + "초";
        if (state == 3)
        {
            resultText.text = current.resultText;
            if (current.isSuccess) resultText.text = "성공!";
        }
    }
}
