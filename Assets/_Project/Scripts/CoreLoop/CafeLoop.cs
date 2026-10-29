using System.Collections;
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
    [Tooltip("손님 메뉴 순서 (데모용). 비우면 menus에서 무작위")]
    public MenuData[] fixedMenus;

    [Header("하루")]
    [Tooltip("하루 길이(초). 카페 화면과 미니게임 플레이 중에만 흐른다")]
    public float daySeconds = 360f;
    [Tooltip("날짜와 남은 시간")]
    public TMP_Text dayText;
    [Tooltip("하루가 끝나면 켜는 영수증 패널 (다음날 버튼 → NextDay)")]
    public GameObject receiptPanel;
    public TMP_Text receiptText;

    [Header("주문 변형·등급·콤보")]
    [Tooltip("단계마다 '적게/많이' 변형이 붙을 확률 (0 = 항상 보통)")]
    [Range(0f, 1f)]
    public float variantChance = 0.6f;
    [Tooltip("quality가 이 이상이면 완벽")]
    public float perfectQuality = 0.7f;
    [Tooltip("quality가 이 이상이면 좋아요, 아니면 아슬아슬")]
    public float goodQuality = 0.3f;
    [Tooltip("등급별 가격 배율: 완벽 / 좋아요 / 아슬아슬")]
    public float perfectPrice = 1.2f;
    public float goodPrice = 1f;
    public float closePrice = 0.7f;
    [Tooltip("콤보 1단계당 팁 비율. 좋아요 이상이 연속될 때 쌓이고 실패·아슬아슬에 끊긴다")]
    public float comboTip = 0.1f;
    public int comboTipMax = 5;

    [Header("연출")]
    public CafeEffects effects;
    [Tooltip("결제 때 손님 위로 떠오르는 글자")]
    public TMP_Text floatText;
    [Tooltip("영수증 상자. 아래에서 올라온다")]
    public RectTransform receiptBox;

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
    [Tooltip("기계·손님 앞에 도착한 뒤 상호작용까지 멈추는 시간(초)")]
    public float arriveDelay = 0.15f;
    [Tooltip("실패 결과를 보여 주는 시간(초)")]
    public float failSeconds = 0.9f;
    [Tooltip("성공 결과를 보여 주는 시간(초)")]
    public float successSeconds = 1.4f;
    [Tooltip("결제·떠남 알림을 보여 주는 시간(초)")]
    public float noticeSeconds = 1.5f;

    // 화면 상태: 0 = 카페, 1 = 지시어, 2 = 미니게임 플레이, 3 = 결과, 4 = 페이드 전환 중(시간 정지)
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

    private int day = 1;
    private float dayTimer;
    // 시간이 다 됨. 새 손님은 오지 않고, 진행 중인 주문이 끝나면 영수증
    private bool dayOver;
    private int salesCount;
    private int leftCount;
    private int fixedMenuIndex;

    // 이번 주문의 단계별 변형(-1/0/1), 단계 가격 배율 합, 방금 끝난 단계 배율, 연속 성공
    private int[] stepVariants = new int[0];
    private float priceSum;
    private float stepMultiplier;
    private int combo;

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
        receiptPanel.SetActive(false);
        floatText.gameObject.SetActive(false);
        dayTimer = daySeconds;
        ShowDay();
        ShowIncome();
        UpdateTargets();
        ShowTexts();
        state = 0;
    }

    private void Update()
    {
        UpdateDay();
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
                    if (Time.frameCount > returnFrame + 1) StartCoroutine(WalkThenClick(clicked));
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
                    JudgeStep();
                    Debug.Log("[CafeLoop] " + current.stationId + " 결과: " + (current.isSuccess ? "성공 q=" + current.quality.ToString("0.00") + " x" + stepMultiplier : "실패") + " / " + current.resultText + " 콤보 " + combo);
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

    // 하루 시계는 인내도처럼 카페 화면과 미니게임 플레이 중에만 흐른다
    private void UpdateDay()
    {
        if (dayOver) return;
        if (state == 0 || state == 2) dayTimer -= Time.deltaTime;
        if (dayTimer <= 0f)
        {
            dayTimer = 0f;
            dayOver = true;
            Debug.Log("[CafeLoop] " + day + "일차 마감");
            // 수락한 주문만 끝까지 한다. 수락 전 손님은 돌려보낸다
            if (order == 1)
            {
                ShowNotice("오늘 영업 끝!");
                CustomerLeave("내일 올게요");
            }
        }
        ShowDay();
    }

    // 하루 진행 비율 (0 = 시작, 1 = 마감). 하루 조명이 읽는다
    public float DayProgress()
    {
        return 1f - dayTimer / daySeconds;
    }

    private void ShowDay()
    {
        if (dayOver)
        {
            dayText.text = day + "일차 마감";
            return;
        }
        int seconds = Mathf.CeilToInt(dayTimer);
        dayText.text = day + "일차  " + (seconds / 60) + ":" + (seconds % 60).ToString("00");
    }

    // 손님이 없으면 시간을 세다가 새 손님을 부른다. 하루가 끝났으면 영수증
    private void UpdateCustomer()
    {
        if (order != 0) return;
        if (dayOver)
        {
            if (!receiptPanel.activeSelf) ShowReceipt();
            return;
        }
        nextCustomerTimer -= Time.deltaTime;
        if (nextCustomerTimer > 0f) return;

        menu = menus[Random.Range(0, menus.Length)];
        if (fixedMenus.Length > 0)
        {
            menu = fixedMenus[fixedMenuIndex % fixedMenus.Length];
            fixedMenuIndex++;
        }
        // 단계마다 변형을 정한다 (예: 추출 '진하게', 우유 '적게')
        stepVariants = new int[menu.steps.Length];
        string request = "";
        for (int i = 0; i < menu.steps.Length; i++)
        {
            stepVariants[i] = 0;
            if (Random.value < variantChance) stepVariants[i] = Random.value < 0.5f ? -1 : 1;
            string label = VariantLabel(menu.steps[i], stepVariants[i]);
            if (label == "") continue;
            if (request != "") request += ", ";
            request += label;
        }
        customer.Show(menu, request);
        effects.Play(effects.bellClip);
        order = 1;
        UpdateTargets();
        Debug.Log("[CafeLoop] 손님 등장: " + menu.menuName);
    }

    // 변형 글자. 기계 id마다 '적게/많이'에 해당하는 말
    private string VariantLabel(string stationId, int variant)
    {
        if (variant == 0) return "";
        switch (stationId)
        {
            case "espresso": return variant < 0 ? "연하게" : "진하게";
            case "ice": return variant < 0 ? "얼음 적게" : "얼음 많이";
            case "milk": return variant < 0 ? "우유 적게" : "우유 많이";
        }
        return "";
    }

    // 방금 끝난 단계의 등급 → 가격 배율과 콤보
    private void JudgeStep()
    {
        stepMultiplier = 0f;
        if (current.isSuccess)
        {
            if (current.quality >= perfectQuality) stepMultiplier = perfectPrice;
            else if (current.quality >= goodQuality) stepMultiplier = goodPrice;
            else stepMultiplier = closePrice;
        }
        if (current.isSuccess && current.quality >= goodQuality) combo++;
        else combo = 0;
    }

    private void AcceptOrder()
    {
        effects.Play(effects.clickClip);
        order = 2;
        stepIndex = 0;
        successCount = 0;
        priceSum = 0f;
        patience = patienceSeconds;
        UpdateTargets();
        Debug.Log("[CafeLoop] 주문 수락: " + menu.menuName);
    }

    private void Serve()
    {
        // 가격 = 정가 × 단계 등급 배율 평균 (100원 단위), 콤보 팁은 그 위에
        int price = Mathf.RoundToInt(menu.price * priceSum / menu.steps.Length / 100f) * 100;
        int tip = 0;
        if (combo >= 2) tip = Mathf.RoundToInt(price * comboTip * Mathf.Min(combo - 1, comboTipMax) / 100f) * 100;
        int before = income;
        income += price + tip;
        salesCount++;
        // 수입 숫자가 굴러 올라가고, 손님 머리 위로 +원이 떠오른다
        StartCoroutine(effects.CountUp(incomeText, "수입 ", before, income, "원", 0.6f));
        StartCoroutine(effects.FloatUp(floatText, "+" + (price + tip) + "원", customer.bubble.position, cafeCamera));
        effects.Play(effects.coinClip);
        string notice = successCount + "/" + menu.steps.Length + " 성공";
        if (tip > 0) notice += " · 콤보 ×" + combo + " 팁 +" + tip + "원";
        ShowNotice(notice);
        Debug.Log("[CafeLoop] 제공: " + menu.menuName + " " + price + "원 + 팁 " + tip);
        if (successCount == menu.steps.Length) CustomerLeave("고마워요!");
        else CustomerLeave("음… 다음엔 잘해 줘요");
    }

    // 손님이 한마디 남기고 떠난다
    private void CustomerLeave(string message)
    {
        customer.Leave(message);
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
            leftCount++;
            Debug.Log("[CafeLoop] 인내도 초과, 손님 떠남");
            CustomerLeave("너무 오래 걸려요!");
        }
    }

    // 다음 단계의 대상만 클릭할 수 있게 한다
    private void UpdateTargets()
    {
        string nextStation = "";
        if (order == 2) nextStation = menu.steps[stepIndex];
        for (int i = 0; i < stations.Length; i++)
        {
            bool was = stations[i].clickable;
            stations[i].clickable = stations[i].stationId == nextStation;
            // 새로 차례가 된 기계는 깜빡여서 알린다
            if (!was && stations[i].clickable) stations[i].Blink();
        }
        bool customerWas = customerStation.clickable;
        customerStation.clickable = order == 1 || order == 3;
        if (!customerWas && customerStation.clickable) customerStation.Blink();
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

    // 순서: 클릭(소리만) → 기계·손님 앞까지 걸어감 → 도착 후 잠깐 멈춤 → 상호작용(미니게임·주문·제공)
    // 걷는 동안(state 4) 시계·인내도·입력이 멈춘다
    private IEnumerator WalkThenClick(Station clicked)
    {
        state = 4;
        ShowTexts();
        effects.Play(effects.clickClip);
        Debug.Log("[CafeLoop] 이동 시작: " + clicked.stationId);
        yield return view.WalkTo(clicked);
        yield return new WaitForSeconds(arriveDelay);
        Debug.Log("[CafeLoop] 도착 → 상호작용: " + clicked.stationId);
        state = 0;
        ShowTexts();
        ClickStation(clicked);
    }

    private void OpenMinigame(Minigame game)
    {
        StartCoroutine(SwitchToMinigame(game));
    }

    // 검게 → 카메라 전환 → 밝게. 전환 중(state 4)에는 시계·인내도·입력이 멈춘다
    private IEnumerator SwitchToMinigame(Minigame game)
    {
        state = 4;
        ShowTexts();
        yield return effects.FadeTo(1f);
        current = game;
        game.variant = stepVariants[stepIndex];
        view.viewEnabled = false;
        game.gameObject.SetActive(true);
        // 카메라 컴포넌트만 끈다 (AudioListener는 카페 카메라에 남김)
        cafeCamera.enabled = false;
        game.viewCamera.enabled = true;
        game.cup = new Cup();
        timer = instructionSeconds;
        state = 1;
        ShowTexts();
        yield return effects.FadeTo(0f);
    }

    // 미니게임 하나가 끝남 → 다음 단계로 (실패해도 진행)
    private void FinishStep()
    {
        if (current.isSuccess) successCount++;
        priceSum += stepMultiplier;
        stepIndex++;
        if (stepIndex >= menu.steps.Length) order = 3;
        CloseMinigame();
        UpdateTargets();
    }

    private void CloseMinigame()
    {
        StartCoroutine(SwitchToCafe());
    }

    private IEnumerator SwitchToCafe()
    {
        state = 4;
        ShowTexts();
        yield return effects.FadeTo(1f);
        current.StopGame();
        current.gameObject.SetActive(false);
        current = null;
        cafeCamera.enabled = true;
        view.viewEnabled = true;
        yield return effects.FadeTo(0f);
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

    // 영수증 동안은 시야를 끄고 커서를 보여 준다
    private void ShowReceipt()
    {
        receiptPanel.SetActive(true);
        view.viewEnabled = false;
        // 상자가 아래에서 올라오고, 수입 숫자가 굴러 올라간다
        StartCoroutine(effects.SlideUp(receiptBox, 500f, 0.35f));
        StartCoroutine(effects.CountUp(receiptText, day + "일차 영수증\n\n판매 " + salesCount + "잔\n떠난 손님 " + leftCount + "명\n\n수입 ", 0, income, "원", 0.8f));
        Debug.Log("[CafeLoop] 영수증: 판매 " + salesCount + ", 떠남 " + leftCount + ", 수입 " + income);
    }

    // 영수증의 '다음날' 버튼
    public void NextDay()
    {
        receiptPanel.SetActive(false);
        view.viewEnabled = true;
        day++;
        dayTimer = daySeconds;
        dayOver = false;
        salesCount = 0;
        leftCount = 0;
        income = 0;
        fixedMenuIndex = 0;
        nextCustomerTimer = nextCustomerSeconds;
        ShowDay();
        ShowIncome();
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
        if (state == 1)
        {
            instructionText.text = current.instruction;
            string label = VariantLabel(current.stationId, current.variant);
            if (label != "") instructionText.text = current.instruction + "\n<size=60%>" + label + "</size>";
        }
        if (state == 2) timeText.text = "남은 시간 " + current.timeLimit.ToString("0.0") + "초";
        if (state == 3)
        {
            resultText.text = current.resultText;
            // 성공은 노랗게 튕기고, 실패는 붉게 흔들린다
            if (current.isSuccess)
            {
                // 등급: 완벽(금색, 크게 튕김) / 좋아요(노랑) / 아슬아슬(주황)
                if (current.quality >= perfectQuality)
                {
                    resultText.text = "완벽!";
                    resultText.color = new Color(1f, 0.82f, 0.2f);
                    StartCoroutine(effects.Pop(resultText.rectTransform, 1.9f));
                }
                else if (current.quality >= goodQuality)
                {
                    resultText.text = "좋아요!";
                    resultText.color = new Color(1f, 0.92f, 0.45f);
                    StartCoroutine(effects.Pop(resultText.rectTransform, 1.5f));
                }
                else
                {
                    resultText.text = "아슬아슬!";
                    resultText.color = new Color(1f, 0.7f, 0.4f);
                    StartCoroutine(effects.Pop(resultText.rectTransform, 1.3f));
                }
                if (combo >= 2) resultText.text += "\n<size=45%>콤보 ×" + combo + "</size>";
                effects.Play(effects.successClip);
            }
            else
            {
                resultText.color = new Color(1f, 0.6f, 0.55f);
                StartCoroutine(effects.Pop(resultText.rectTransform, 1.2f));
                StartCoroutine(effects.Shake(resultText.rectTransform, 18f));
                effects.Play(effects.failClip);
            }
        }
    }
}
