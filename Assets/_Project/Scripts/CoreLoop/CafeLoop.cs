using TMPro;
using UnityEngine;

// 코어 루프 관리. 손님이 오면 주문을 받고, 메뉴 단계 순서대로 기계(미니게임)를 거쳐 손님에게 제공·결제한다
public class CafeLoop : MonoBehaviour
{
    [Tooltip("씬에 꺼 둔 미니게임들 (카페 밖 먼 곳)")]
    public Minigame[] minigames;
    public CafeView view;
    public Camera cafeCamera;

    [Header("손님·주문")]
    public Customer customer;
    [Tooltip("손님이 고를 메뉴 (무작위)")]
    public MenuData[] menus;
    [Tooltip("카페의 기계들. 다음 단계 기계만 클릭할 수 있게 된다")]
    public Station[] stations;
    [Tooltip("손님이 떠나거나 제공받은 뒤 다음 손님까지(초)")]
    public float nextCustomerSeconds = 3f;
    [Tooltip("인내도(초). 주문을 수락하면 줄기 시작한다")]
    public float patienceSeconds = 90f;
    [Tooltip("미니게임 중 화면 위 인내도 막대 전체")]
    public GameObject minigamePatienceBar;
    [Tooltip("미니게임 중 줄어드는 막대. 왼쪽 기준(pivot x = 0)")]
    public RectTransform minigamePatienceFill;

    [Header("글자 (코어 루프 Canvas)")]
    public TMP_Text instructionText;
    public TMP_Text timeText;
    public TMP_Text resultText;
    [Tooltip("결제·손님 떠남 알림")]
    public TMP_Text noticeText;
    [Tooltip("누적 수입")]
    public TMP_Text incomeText;
    [Tooltip("지시어를 보여 주는 시간(초). 이 동안 제한 시간은 세지 않는다")]
    public float instructionSeconds = 1.5f;
    [Tooltip("실패 결과를 보여 주는 시간(초)")]
    public float failSeconds = 0.9f;
    [Tooltip("성공 결과를 보여 주는 시간(초)")]
    public float successSeconds = 1.4f;
    [Tooltip("결제·떠남 알림을 보여 주는 시간(초)")]
    public float noticeSeconds = 1.5f;

    // 화면 상태: 0 = 카페, 1 = 지시어, 2 = 미니게임 플레이, 3 = 결과
    private int state;
    private Minigame current;
    private float timer;
    private int returnFrame = -10;

    // 주문 상태: 0 = 손님 없음, 1 = 수락 대기, 2 = 제조 중, 3 = 제공 대기
    private int order;
    private MenuData menu;
    private int stepIndex;
    private int successCount;
    private float nextCustomerTimer;
    private float patience;
    private int income;
    private float noticeTimer;
    private Station customerStation;

    private void Start()
    {
        for (int i = 0; i < minigames.Length; i++)
        {
            minigames[i].gameObject.SetActive(false);
        }
        customerStation = customer.GetComponent<Station>();
        customer.Hide();
        order = 0;
        nextCustomerTimer = nextCustomerSeconds;
        noticeText.gameObject.SetActive(false);
        minigamePatienceBar.SetActive(false);
        ShowIncome();
        UpdateTargets();
        ShowTexts();
        state = 0;
    }

    private void Update()
    {
        UpdateCustomer();
        UpdatePatience();
        UpdateNotice();
        if (order == 0) return;

        switch (state)
        {
            case 0:
                if (view.clickedStation != null)
                {
                    Station clicked = view.clickedStation;
                    view.clickedStation = null;
                    // 결과를 건너뛴 클릭이 바로 다음 클릭으로 이어지지 않게
                    if (Time.frameCount > returnFrame + 1) ClickStation(clicked);
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
                    FinishStep();
                }
                break;
        }
    }

    private void ClickStation(Station clicked)
    {
        if (clicked == customerStation)
        {
            if (order == 1) AcceptOrder();
            else if (order == 3) Serve();
            return;
        }
        Minigame game = FindMinigame(clicked.stationId);
        if (game != null) OpenMinigame(game);
    }

    // 손님이 없으면 시간을 세다가 새 손님을 부른다
    private void UpdateCustomer()
    {
        if (order != 0) return;
        nextCustomerTimer -= Time.deltaTime;
        if (nextCustomerTimer > 0f) return;

        menu = menus[Random.Range(0, menus.Length)];
        customer.Show(menu);
        order = 1;
        UpdateTargets();
        Debug.Log("[CafeLoop] 손님 등장: " + menu.menuName);
    }

    private void AcceptOrder()
    {
        order = 2;
        stepIndex = 0;
        successCount = 0;
        patience = patienceSeconds;
        UpdateTargets();
        Debug.Log("[CafeLoop] 주문 수락: " + menu.menuName);
    }

    private void Serve()
    {
        int price = menu.price * successCount / menu.steps.Length;
        income += price;
        ShowIncome();
        ShowNotice("+" + price + "원 (" + successCount + "/" + menu.steps.Length + " 성공)");
        Debug.Log("[CafeLoop] 제공: " + menu.menuName + " " + price + "원");
        CustomerLeave();
    }

    private void CustomerLeave()
    {
        customer.Hide();
        order = 0;
        menu = null;
        nextCustomerTimer = nextCustomerSeconds;
        UpdateTargets();
    }

    // 인내도는 카페 화면과 미니게임 플레이 중에만 준다 (지시어·결과 표시 중에는 정지)
    private void UpdatePatience()
    {
        bool accepted = order == 2 || order == 3;
        if (accepted && (state == 0 || state == 2)) patience -= Time.deltaTime;

        float ratio = patience / patienceSeconds;
        customer.SetPatience(accepted, ratio);
        minigamePatienceBar.SetActive(accepted && state != 0);
        minigamePatienceFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);

        if (accepted && patience <= 0f)
        {
            // 미니게임 중이면 바로 중단하고 카페로 돌아온다
            if (state != 0) CloseMinigame();
            ShowNotice("손님이 떠났어요");
            Debug.Log("[CafeLoop] 인내도 초과, 손님 떠남");
            CustomerLeave();
        }
    }

    // 다음 단계의 대상만 클릭할 수 있게 한다
    private void UpdateTargets()
    {
        string nextStation = "";
        if (order == 2) nextStation = menu.steps[stepIndex];
        for (int i = 0; i < stations.Length; i++)
        {
            stations[i].clickable = stations[i].stationId == nextStation;
        }
        customerStation.clickable = order == 1 || order == 3;
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

    // 미니게임 하나가 끝남 → 다음 단계로 (실패해도 진행)
    private void FinishStep()
    {
        if (current.isSuccess) successCount++;
        stepIndex++;
        if (stepIndex >= menu.steps.Length) order = 3;
        CloseMinigame();
        UpdateTargets();
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

    private void ShowNotice(string text)
    {
        noticeText.text = text;
        noticeText.gameObject.SetActive(true);
        noticeTimer = noticeSeconds;
    }

    private void UpdateNotice()
    {
        if (noticeTimer <= 0f) return;
        noticeTimer -= Time.deltaTime;
        if (noticeTimer <= 0f) noticeText.gameObject.SetActive(false);
    }

    private void ShowIncome()
    {
        incomeText.text = "수입 " + income + "원";
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
