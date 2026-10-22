using UnityEngine;

// 코어 루프 관리. 기계를 클릭하면 같은 stationId의 미니게임을 열고, 끝나면 카페 화면으로 돌아온다
public class CafeLoop : MonoBehaviour
{
    [Tooltip("씬에 꺼 둔 미니게임들 (카페 밖 먼 곳)")]
    public Minigame[] minigames;
    public CafeView view;
    public Camera cafeCamera;

    // 0 = 카페, 1 = 미니게임 플레이
    private int state;
    private Minigame current;
    private float timer;

    private void Start()
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            minigames[i].gameObject.SetActive(false);
        }
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
                    if (game != null) OpenMinigame(game);
                }
                break;
            case 1:
                timer -= Time.deltaTime;
                if (timer <= 0f && current.isPlaying) current.TimeOver();
                if (current.isFinished)
                {
                    Debug.Log("[CafeLoop] " + current.stationId + " 결과: " + (current.isSuccess ? "성공" : "실패") + " / " + current.resultText);
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
        game.StartGame();
        timer = game.timeLimit;
        state = 1;
        Debug.Log("[CafeLoop] 미니게임 시작: " + game.stationId);
    }

    private void CloseMinigame()
    {
        current.StopGame();
        current.gameObject.SetActive(false);
        current = null;
        cafeCamera.enabled = true;
        view.viewEnabled = true;
        state = 0;
    }
}
