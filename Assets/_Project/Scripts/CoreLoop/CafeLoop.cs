using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// 코어 루프 관리. 손님이 오면 주문을 받고, 메뉴 단계 순서대로 기계(미니게임)를 거쳐 손님에게 제공·결제한다
// 하루: 시간이 다 되거나 그날 손님을 다 받으면 마감 → 영수증(저장) → 상점 → 다음날
public class CafeLoop : MonoBehaviour
{
    [Tooltip("씬에 꺼 둔 미니게임들 (카페 밖 먼 곳)")]
    public Minigame[] minigames;
    public CafeView view;
    public Camera cafeCamera;

    [Header("손님·주문")]
    public Customer customer;
    [Tooltip("손님이 고를 메뉴 (무작위). 상점에서 파는 레시피는 산 뒤에만 나온다")]
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

    [Header("하루")]
    [Tooltip("하루 길이(초). 카페 화면과 미니게임 플레이 중에만 흐른다")]
    public float daySeconds = 75f;
    [Tooltip("1일차 손님 수. 하루마다 customersPerDayStep씩 늘고 maxCustomersPerDay에서 멈춘다")]
    public int firstDayCustomers = 3;
    public int customersPerDayStep = 1;
    public int maxCustomersPerDay = 6;
    [Tooltip("날짜와 남은 시간")]
    public TMP_Text dayText;
    [Tooltip("하루가 끝나면 켜는 영수증 패널 (상점 버튼 → OpenShop)")]
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
    [Tooltip("이번 실행에서 처음 들어가는 미니게임의 지시어 시간(초). 조작 안내가 함께 보인다")]
    public float firstInstructionSeconds = 2.6f;
    [Tooltip("기계·손님 앞에 도착한 뒤 상호작용까지 멈추는 시간(초)")]
    public float arriveDelay = 0.15f;
    [Tooltip("실패 결과를 보여 주는 시간(초)")]
    public float failSeconds = 0.9f;
    [Tooltip("성공 결과를 보여 주는 시간(초)")]
    public float successSeconds = 1.4f;
    [Tooltip("결제·떠남 알림을 보여 주는 시간(초)")]
    public float noticeSeconds = 1.5f;
    [Tooltip("마감 후 손님·알림이 다 사라지고 영수증이 뜨기까지(초)")]
    public float receiptDelay = 0.6f;

    [Header("진행·상점·로딩")]
    [Tooltip("저장·상점 (영수증 다음에 열림, 시작 버튼 → NextDay)")]
    public CafeShop shop;
    [Tooltip("씬 시작 때 덮는 로딩 화면")]
    public LoadingScreen loading;
    [Tooltip("가운데 점 둘레의 다음 대상 방향 표시")]
    public TargetPointer pointer;

    [Header("하루 시작 연출")]
    [Tooltip("로딩이 끝난 뒤 배경 음악이 커지는 시간(초)")]
    public float bgmFadeSeconds = 1.6f;
    [Tooltip("'영업 시작' 알림 뒤 첫 손님까지(초)")]
    public float firstCustomerSeconds = 1f;

    // 화면 상태 (state)
    private const int StateCafe = 0;        // 카페
    private const int StateInstruction = 1; // 지시어
    private readonly HashSet<string> seenStations = new HashSet<string>(); // 이번 실행에서 들어가 본 미니게임
    private bool showControlHint; // 이번 지시어에 조작 안내를 붙일지
    private const int StatePlaying = 2;     // 미니게임 플레이
    private const int StateResult = 3;      // 결과
    private const int StateFade = 4;        // 페이드·걷기 전환 중 (시간 정지)

    // 이번 주문의 컵. 수락 때 만들고 단계마다 같은 컵을 넘기며, 손님이 떠나면 버린다
    private Cup orderCup = new Cup();

    // 주문 상태 (order)
    private const int OrderNone = 0;        // 손님 없음
    private const int OrderWaitAccept = 1;  // 수락 대기
    private const int OrderMaking = 2;      // 제조 중
    private const int OrderWaitServe = 3;   // 제공 대기

    // 단계 등급 (GradeOf)
    private const int GradeClose = 0;       // 아슬아슬
    private const int GradeGood = 1;        // 좋아요
    private const int GradePerfect = 2;     // 완벽

    // 가격·콤보 기준
    private const int PriceUnit = 100;      // 가격·팁을 맞추는 단위(원)
    private const int TipMinCombo = 2;      // 팁이 붙고 결과 글자에 콤보가 보이는 최소 콤보

    // 변형·연출 값
    private const float LessVariantRatio = 0.5f;     // 변형이 붙을 때 '적게'가 될 확률
    private const float CountUpSeconds = 0.6f;       // 결제 수입 숫자가 굴러 올라가는 시간(초)
    private const float ReceiptSlideDistance = 500f; // 영수증 상자가 올라오는 거리
    private const float ReceiptSlideSeconds = 0.35f; // 영수증 상자가 올라오는 시간(초)
    private const float ReceiptCountSeconds = 0.8f;  // 영수증 수입 숫자가 굴러 올라가는 시간(초)
    private const float PopPerfect = 1.9f;           // 결과 글자 튕김 배율: 완벽
    private const float PopGood = 1.5f;              // 좋아요
    private const float PopClose = 1.3f;             // 아슬아슬
    private const float PopFail = 1.2f;              // 실패
    private const float ShakeFail = 18f;             // 실패 흔들림 세기

    // 결과 글자 색
    private static readonly Color PerfectColor = new Color(1f, 0.82f, 0.2f);
    private static readonly Color GoodColor = new Color(1f, 0.92f, 0.45f);
    private static readonly Color CloseColor = new Color(1f, 0.7f, 0.4f);
    private static readonly Color FailColor = new Color(1f, 0.6f, 0.55f);

    private int state;
    private Minigame current;
    private float timer;
    private int returnFrame = -10;

    private int order;
    private MenuData menu;
    private int stepIndex;
    private int successCount;
    private float nextCustomerTimer;
    private float patience;
    // 이번 주문의 인내도 최대값(초). 메뉴 값이 있으면 그것, 없으면 patienceSeconds
    private float patienceMax;
    private int income;
    private float noticeTimer;
    private Station customerStation;

    private int day = 1;
    private float dayTimer;
    // 마감됨 (시간 종료 또는 오늘 손님 소진). 새 손님은 오지 않고, 진행 중인 주문이 끝나면 영수증
    private bool dayOver;
    private int salesCount;
    private int failCount;
    private int leftCount;
    // 오늘 온 손님 수 (지금 손님 포함)
    private int customersToday;
    // 마감 후 영수증까지 남은 시간. 손님 퇴장·알림·수입 굴리기가 다 끝난 뒤부터 센다
    private float receiptTimer;
    // 오늘 영수증을 이미 띄웠다 (상점으로 넘어가도 다시 띄우지 않게)
    private bool receiptShown;
    // 하루 시작 전 (로딩 화면). 이 동안 손님이 오지 않는다
    private bool opening;

    // 이번 주문의 단계별 변형(-1/0/1), 단계 가격 배율 합, 방금 끝난 단계 배율, 연속 성공
    private int[] stepVariants = new int[0];
    private float priceSum;
    private float stepMultiplier;
    private int combo;
    // 슬롯에서 불러온 하루 중 상태로 시작했다 (OpenDay 대신 '불러왔어요')
    private bool loadedSave;

    private void Start()
    {
        // 새 게임·슬롯 불러오기(로비·ESC 메뉴)면 작업본부터 바꾼다. 하루 중 상태는 RestoreSave가 되살린다
        if (SaveSlots.newGame) shop.ResetSave();
        else if (SaveSlots.pending != null) shop.ApplySave(SaveSlots.pending);
        SaveSlots.newGame = false;
        // 저장된 날·돈·산 기계를 읽는다 (하루 도중 나갔으면 그날 아침부터)
        shop.Load();
        day = shop.SavedDay();
        for (int i = 0; i < minigames.Length; i++)
        {
            minigames[i].gameObject.SetActive(false);
        }
        customerStation = customer.GetComponent<Station>();
        customer.Hide();
        noticeText.gameObject.SetActive(false);
        minigamePatienceBar.SetActive(false);
        receiptPanel.SetActive(false);
        floatText.gameObject.SetActive(false);
        dayTimer = daySeconds;
        // HUD 패널·하루 진행 바·수입 튕김 (글자 참조는 그대로)
        gameObject.AddComponent<CafeHud>().Init(this);
        ShowDay();
        ShowIncome();
        UpdateTargets();
        RestoreSave();
        StartCoroutine(BeginGame());
    }

    // 씬 시작: 로딩 화면 → 배경 음악 → 하루 시작. 로딩 동안(state 4) 시계·입력이 멈춘다
    private IEnumerator BeginGame()
    {
        opening = true;
        state = StateFade;
        ShowTexts();
        view.viewEnabled = false;
        yield return loading.Run(day);
        StartCoroutine(effects.StartBgm(bgmFadeSeconds));
        state = StateCafe;
        ShowTexts();
        // 영수증·상점에서 나갔으면 상점부터 다시 연다 (저장된 날은 이미 다음날)
        if (shop.ShopPending())
        {
            day--;
            dayOver = true;
            receiptShown = true;
            ShowDay();
            shop.Open(day + 1);
            yield break;
        }
        view.viewEnabled = true;
        if (loadedSave)
        {
            ShowNotice("불러왔어요");
            opening = false;
            yield break;
        }
        OpenDay();
    }

    // 하루 시작: "N일차 영업 시작!" → firstCustomerSeconds 뒤 첫 손님
    private void OpenDay()
    {
        ShowNotice(day + "일차 영업 시작!");
        nextCustomerTimer = firstCustomerSeconds;
        opening = false;
        Debug.Log("[CafeLoop] " + day + "일차 영업 시작, 손님 " + CustomersPerDay() + "명");
    }

    private void Update()
    {
        if (HandleCheatKeys()) return;
        ClearUiSelection();
        UpdateDay();
        UpdateCustomer();
        UpdatePatience();
        UpdateNotice();
        if (order == OrderNone)
        {
            // 주문이 없는 프레임에 들어온 클릭은 버린다 (다음 손님 때 저절로 걸어가지 않게)
            view.clickedStation = null;
            return;
        }

        switch (state)
        {
            case StateCafe:
                UpdateCafeClick();
                break;
            case StateInstruction:
                UpdateInstruction();
                break;
            case StatePlaying:
                UpdatePlaying();
                break;
            case StateResult:
                UpdateResult();
                break;
        }
    }

    // 버튼을 클릭하면 EventSystem에 선택이 남아 Enter·Space가 그 버튼을 다시 누른다. 일시정지(ESC 메뉴) 중이 아니면 선택을 비운다
    private void ClearUiSelection()
    {
        EventSystem system = EventSystem.current;
        if (system == null || Time.timeScale <= 0f) return;
        if (system.currentSelectedGameObject != null) system.SetSelectedGameObject(null);
    }

    // 치트 키(발표·테스트용): F9 = 저장을 지우고 1일차부터, F10 = 돈 +10000원
    // F9로 씬을 다시 불러오면 true (이번 프레임의 나머지 처리를 건너뛴다)
    private bool HandleCheatKeys()
    {
        if (Keyboard.current == null) return false;
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            shop.ResetSave();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
        }
        if (Keyboard.current.f10Key.wasPressedThisFrame) shop.AddMoney(10000);
        return false;
    }

    // 카페 화면: 기계·손님 클릭이 들어오면 그쪽으로 걸어간다
    private void UpdateCafeClick()
    {
        if (view.clickedStation == null) return;
        Station clicked = view.clickedStation;
        view.clickedStation = null;
        // 결과를 건너뛴 클릭이 바로 다음 클릭으로 이어지지 않게
        if (Time.frameCount > returnFrame + 1) StartCoroutine(WalkThenClick(clicked));
    }

    // 지시어를 보여 주는 동안 기다렸다가 미니게임을 시작한다
    private void UpdateInstruction()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        current.StartGame();
        timer = current.timeLimit;
        state = StatePlaying;
        ShowTexts();
        Debug.Log("[CafeLoop] 미니게임 시작: " + current.stationId);
    }

    // 미니게임 플레이: 제한 시간을 세고, 끝나면 판정해 결과 화면으로 넘어간다
    private void UpdatePlaying()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f && current.isPlaying) current.TimeOver();
        timeText.text = "남은 시간 " + Mathf.Max(0f, timer).ToString("0.0") + "초";
        if (!current.isFinished) return;
        JudgeStep();
        LogStepResult();
        timer = current.isSuccess ? successSeconds : failSeconds;
        state = StateResult;
        ShowTexts();
    }

    private void LogStepResult()
    {
        string outcome = current.isSuccess ? "성공 q=" + current.quality.ToString("0.00") + " x" + stepMultiplier : "실패";
        Debug.Log("[CafeLoop] " + current.stationId + " 결과: " + outcome + " / " + current.resultText + " 콤보 " + combo);
    }

    // 결과 화면: 시간이 지나거나 클릭하면 다음 단계로
    private void UpdateResult()
    {
        timer -= Time.deltaTime;
        // 클릭하면 결과 표시를 건너뛴다
        if (timer <= 0f || current.clickAction.action.WasPressedThisFrame())
        {
            FinishStep();
        }
    }

    private void ClickStation(Station clicked)
    {
        if (clicked == customerStation)
        {
            if (order == OrderWaitAccept) AcceptOrder();
            else if (order == OrderWaitServe) Serve();
            return;
        }
        Minigame game = FindMinigame(clicked.stationId);
        if (game != null) StartCoroutine(SwitchToMinigame(game));
    }

    // 하루 시계는 인내도처럼 카페 화면과 미니게임 플레이 중에만 흐른다
    private void UpdateDay()
    {
        if (dayOver) return;
        if (state == StateCafe || state == StatePlaying) dayTimer -= Time.deltaTime;
        if (dayTimer <= 0f)
        {
            dayTimer = 0f;
            EndDay();
        }
        ShowDay();
    }

    // 마감: 시간이 다 됐거나 오늘 손님을 다 받았다. 새 손님은 오지 않고, 진행 중인 주문이 끝나면 영수증
    private void EndDay()
    {
        dayOver = true;
        Debug.Log("[CafeLoop] " + day + "일차 마감");
        // 수락한 주문만 끝까지 한다. 수락 전 손님은 돌려보낸다
        if (order == OrderWaitAccept) CustomerLeave("내일 올게요");
        // 미니게임 중이면 결과 글자와 겹치니 알림을 띄우지 않는다 (시계가 "마감"으로 바뀐다)
        if (IsOrderAccepted())
        {
            if (state == StateCafe) ShowNotice("영업 끝! 마지막 손님만 마무리해요");
        }
        // 방금 뜬 결제·팁 알림을 덮지 않는다
        else if (noticeTimer <= 0f) ShowNotice("오늘 영업 끝!");
        receiptTimer = receiptDelay;
        ShowDay();
    }

    // 주문을 수락한 상태인가 (제조 중 또는 제공 대기)
    private bool IsOrderAccepted()
    {
        return order == OrderMaking || order == OrderWaitServe;
    }

    // 손님을 눌러야 하는 차례인가 (주문 받기 또는 음료 건네기)
    private bool IsCustomerTurn()
    {
        return order == OrderWaitAccept || order == OrderWaitServe;
    }

    // 오늘 받을 손님 수
    private int CustomersPerDay()
    {
        return Mathf.Min(maxCustomersPerDay, firstDayCustomers + (day - 1) * customersPerDayStep);
    }

    // 하루 진행 비율 (0 = 시작, 1 = 마감). 하루 조명이 읽는다
    public float DayProgress()
    {
        return 1f - dayTimer / daySeconds;
    }

    private void ShowDay()
    {
        string count = "  <size=70%>손님 " + customersToday + "/" + CustomersPerDay() + "</size>";
        string head = "<color=#FFD166>" + day + "일차</color>";
        if (dayOver)
        {
            dayText.text = head + "  마감" + count;
            return;
        }
        int seconds = Mathf.CeilToInt(dayTimer);
        dayText.text = head + "  " + (seconds / 60) + ":" + (seconds % 60).ToString("00") + count;
    }

    // 손님이 없으면 시간을 세다가 새 손님을 부른다. 하루가 끝났으면 영수증
    private void UpdateCustomer()
    {
        if (order != OrderNone) return;
        if (dayOver)
        {
            UpdateReceiptWait();
            return;
        }
        if (opening) return;
        // 오늘 손님을 다 받았다 (마지막 손님이 떠난 뒤) → 마감
        if (customersToday >= CustomersPerDay())
        {
            EndDay();
            return;
        }
        nextCustomerTimer -= Time.deltaTime;
        if (nextCustomerTimer > 0f) return;
        SpawnCustomer();
    }

    // 손님 퇴장·알림·미니게임 복귀가 다 끝나고 receiptDelay 뒤에 영수증 (겹쳐 뜨지 않게)
    private void UpdateReceiptWait()
    {
        if (receiptShown) return;
        bool busy = state != StateCafe || customer.gameObject.activeSelf || noticeTimer > 0f;
        if (busy)
        {
            receiptTimer = receiptDelay;
            return;
        }
        receiptTimer -= Time.deltaTime;
        if (receiptTimer <= 0f) ShowReceipt();
    }

    // 새 손님을 메뉴·변형과 함께 부른다
    private void SpawnCustomer()
    {
        menu = PickMenu();
        customersToday++;
        string request = RollVariants();
        customer.Show(menu, request);
        effects.Play(effects.bellClip);
        if (customersToday == CustomersPerDay()) ShowNotice("오늘 마지막 손님이에요!");
        order = OrderWaitAccept;
        UpdateTargets();
        ShowDay();
        Debug.Log("[CafeLoop] 손님 등장(" + customersToday + "/" + CustomersPerDay() + "): " + menu.menuName);
    }

    // 단계마다 변형을 정한다 (예: 추출 '진하게', 우유 '적게'). stepVariants를 채우고 손님 요청 글자를 돌려준다
    private string RollVariants()
    {
        stepVariants = new int[menu.steps.Length];
        string request = "";
        for (int i = 0; i < menu.steps.Length; i++)
        {
            if (Random.value < variantChance) stepVariants[i] = Random.value < LessVariantRatio ? -1 : 1;
            string label = VariantLabel(menu.steps[i], stepVariants[i]);
            if (label == "") continue;
            if (request != "") request += ", ";
            request += label;
        }
        return request;
    }

    // 어제 상점에서 산 레시피가 있으면 첫 손님이 그것을 주문한다. 아니면 열린 메뉴 중 무작위
    private MenuData PickMenu()
    {
        MenuData fresh = shop.TakeNewMenu();
        if (fresh != null) return fresh;
        List<MenuData> open = new List<MenuData>();
        for (int i = 0; i < menus.Length; i++)
        {
            if (shop.IsMenuOpen(menus[i])) open.Add(menus[i]);
        }
        return open[Random.Range(0, open.Count)];
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
            case "water": return variant < 0 ? "물 적게" : "물 많이";
        }
        return "";
    }

    // 처음 들어가는 미니게임의 조작 안내 (실제 입력은 각 미니게임 스크립트 기준)
    private string ControlLabel(string stationId)
    {
        switch (stationId)
        {
            case "espresso": return "좌클릭 꾹, 목표 구간에서 떼기";
            case "ice": return "좌클릭 꾹";
            case "milk": return "우클릭으로 기울이기";
            case "grinder": return "좌클릭으로 원두 담고 Q로 넘기기";
            case "autogrind": return "Space로 타이밍";
            case "portafilter": return "마우스로 맞추고 좌클릭 꾹";
            case "tamper": return "좌클릭 꾹 + 마우스 좌우로 수평";
            case "water": return "휠로 열고 우클릭으로 잠그기";
            case "steam": return "마우스 위아래 + 좌클릭 분사";
            case "latteart": return "마우스로 컵 위를 움직이며 좌클릭 꾹";
        }
        return "";
    }

    // quality → 등급 (완벽 / 좋아요 / 아슬아슬)
    private int GradeOf(float quality)
    {
        if (quality >= perfectQuality) return GradePerfect;
        if (quality >= goodQuality) return GradeGood;
        return GradeClose;
    }

    // 방금 끝난 단계의 등급 → 가격 배율과 콤보
    private void JudgeStep()
    {
        stepMultiplier = 0f;
        if (current.isSuccess)
        {
            int grade = GradeOf(current.quality);
            if (grade == GradePerfect) stepMultiplier = perfectPrice;
            else if (grade == GradeGood) stepMultiplier = goodPrice;
            else stepMultiplier = closePrice;
        }
        if (current.isSuccess && current.quality >= goodQuality) combo++;
        else combo = 0;
    }

    private void AcceptOrder()
    {
        effects.Play(effects.clickClip);
        order = OrderMaking;
        orderCup = new Cup();
        stepIndex = 0;
        successCount = 0;
        priceSum = 0f;
        patienceMax = PatienceOf(menu);
        patience = patienceMax;
        UpdateTargets();
        Debug.Log("[CafeLoop] 주문 수락: " + menu.menuName);
    }

    // 메뉴의 인내도(초). 비어 있으면(0) 공통 값
    private float PatienceOf(MenuData target)
    {
        return target.patienceSeconds > 0f ? target.patienceSeconds : patienceSeconds;
    }

    private void Serve()
    {
        // 모든 단계를 실패한 음료는 팔지 않는다 (0원 판매·"+0원"이 뜨지 않게)
        if (successCount == 0)
        {
            failCount++;
            effects.Play(effects.failClip);
            ShowNotice("제조 실패 · 0원");
            Debug.Log("[CafeLoop] 제공 실패: " + menu.menuName);
            CustomerLeave("이건 못 마시겠어요…");
            return;
        }
        // 가격 = 정가 × 단계 등급 배율 평균 (100원 단위), 콤보 팁은 그 위에
        int price = RoundToHundred(menu.price * priceSum / menu.steps.Length);
        int tip = 0;
        if (combo >= TipMinCombo) tip = RoundToHundred(price * comboTip * Mathf.Min(combo - 1, comboTipMax));
        int total = price + tip;
        int before = income;
        income += total;
        salesCount++;
        // 수입 숫자가 굴러 올라가고, 손님 머리 위로 +원이 떠오른다
        StartCoroutine(effects.CountUp(incomeText, "수입 ", before, income, "원", CountUpSeconds));
        StartCoroutine(effects.FloatUp(floatText, "+" + total + "원", customer.bubble.position, cafeCamera));
        effects.Play(effects.coinClip);
        string notice = successCount + "/" + menu.steps.Length + " 성공";
        if (tip > 0) notice += " · 콤보 ×" + combo + " 팁 +" + tip + "원";
        ShowNotice(notice);
        Debug.Log("[CafeLoop] 제공: " + menu.menuName + " " + price + "원 + 팁 " + tip);
        CustomerLeave(successCount == menu.steps.Length ? "고마워요!" : "음… 다음엔 잘해 줘요");
    }

    // 금액을 PriceUnit(100원) 단위로 반올림
    private int RoundToHundred(float value)
    {
        return Mathf.RoundToInt(value / PriceUnit) * PriceUnit;
    }

    // 손님이 한마디 남기고 떠난다
    private void CustomerLeave(string message)
    {
        customer.Leave(message);
        order = OrderNone;
        orderCup = new Cup();
        menu = null;
        nextCustomerTimer = nextCustomerSeconds;
        UpdateTargets();
    }

    // 인내도는 카페 화면과 미니게임 플레이 중에만 준다 (지시어·결과 표시 중에는 정지)
    private void UpdatePatience()
    {
        bool accepted = IsOrderAccepted();
        if (accepted && (state == StateCafe || state == StatePlaying)) patience -= Time.deltaTime;

        float ratio = patienceMax > 0f ? patience / patienceMax : 0f;
        customer.SetPatience(accepted, ratio);
        // 미니게임이 보이는 동안에만 (걷기·전환 페이드·카페에서는 숨김)
        bool inMinigame = current != null && (state == StateInstruction || state == StatePlaying || state == StateResult);
        minigamePatienceBar.SetActive(accepted && inMinigame);
        minigamePatienceFill.localScale = new Vector3(Mathf.Clamp01(ratio), 1f, 1f);

        if (accepted && patience <= 0f)
        {
            // 미니게임 중이면 바로 중단하고 카페로 돌아온다
            if (state != StateCafe) CloseMinigame();
            ShowNotice("손님이 떠났어요");
            leftCount++;
            Debug.Log("[CafeLoop] 인내도 초과, 손님 떠남");
            CustomerLeave("너무 오래 걸려요!");
        }
    }

    // 다음 단계의 대상만 클릭할 수 있게 하고, 가운데 점 둘레 화살표가 그쪽을 가리키게 한다
    private void UpdateTargets()
    {
        string nextStation = "";
        if (order == OrderMaking) nextStation = menu.steps[stepIndex];
        Station next = null;
        string what = "";
        for (int i = 0; i < stations.Length; i++)
        {
            bool was = stations[i].clickable;
            stations[i].clickable = stations[i].stationId == nextStation;
            if (!stations[i].clickable) continue;
            next = stations[i];
            what = StepLabel(nextStation);
            // 새로 차례가 된 기계는 깜빡여서 알린다
            if (!was && stations[i].gameObject.activeInHierarchy) stations[i].Blink();
        }
        bool customerWas = customerStation.clickable;
        bool customerTurn = IsCustomerTurn();
        customerStation.clickable = customerTurn;
        if (!customerWas && customerTurn) customerStation.Blink();
        if (customerTurn)
        {
            next = customerStation;
            what = order == OrderWaitAccept ? "주문 받기" : "음료 건네기";
        }
        pointer.SetTarget(next, what);
    }

    // 방향 표시 아래 글자
    private string StepLabel(string stationId)
    {
        switch (stationId)
        {
            case "grinder": return "원두 넣기";
            case "espresso": return "샷 추출";
            case "ice": return "얼음 담기";
            case "milk": return "우유 붓기";
            case "handgrinder": return "원두 갈기";
            case "dripper": return "원두·물 넣기";
            case "kettle": return "물 끓이기";
            case "drip": return "드립 붓기";
            case "autogrind": return "원두 갈기";
            case "tamper": return "탬핑";
            case "water": return "물 타기";
            case "portafilter": return "포타필터 담기";
            case "steam": return "우유 스팀";
            case "latteart": return "라떼 아트";
        }
        return "";
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
        state = StateFade;
        ShowTexts();
        effects.Play(effects.clickClip);
        Debug.Log("[CafeLoop] 이동 시작: " + clicked.stationId);
        yield return view.WalkTo(clicked);
        yield return new WaitForSeconds(arriveDelay);
        Debug.Log("[CafeLoop] 도착 → 상호작용: " + clicked.stationId);
        state = StateCafe;
        ShowTexts();
        ClickStation(clicked);
    }

    // 검게 → 카메라 전환 → 밝게. 전환 중(state 4)에는 시계·인내도·입력이 멈춘다
    private IEnumerator SwitchToMinigame(Minigame game)
    {
        state = StateFade;
        ShowTexts();
        yield return effects.FadeTo(1f);
        current = game;
        game.variant = stepVariants[stepIndex];
        view.viewEnabled = false;
        game.gameObject.SetActive(true);
        // 카메라 컴포넌트만 끈다 (AudioListener는 카페 카메라에 남김)
        cafeCamera.enabled = false;
        game.viewCamera.enabled = true;
        game.cup = orderCup;
        // 지시어 동안 이전 판의 잔상이 보이지 않게, 화면이 검을 때 한 번 초기화 (cup·variant를 넣은 뒤)
        game.PrepareGame();
        // 이번 실행에서 처음 들어가는 미니게임이면 조작 안내를 읽을 시간을 더 준다
        bool firstVisit = seenStations.Add(game.stationId);
        timer = firstVisit ? Mathf.Max(instructionSeconds, firstInstructionSeconds) : instructionSeconds;
        showControlHint = firstVisit;
        state = StateInstruction;
        ShowTexts();
        yield return effects.FadeTo(0f);
    }

    // 미니게임 하나가 끝남 → 다음 단계로 (실패해도 진행)
    private void FinishStep()
    {
        if (current.isSuccess) successCount++;
        priceSum += stepMultiplier;
        stepIndex++;
        if (stepIndex >= menu.steps.Length) order = OrderWaitServe;
        CloseMinigame();
        UpdateTargets();
    }

    private void CloseMinigame()
    {
        StartCoroutine(SwitchToCafe());
    }

    private IEnumerator SwitchToCafe()
    {
        state = StateFade;
        ShowTexts();
        yield return effects.FadeTo(1f);
        current.StopGame();
        current.gameObject.SetActive(false);
        current = null;
        cafeCamera.enabled = true;
        view.viewEnabled = true;
        yield return effects.FadeTo(0f);
        returnFrame = Time.frameCount;
        state = StateCafe;
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
        if (noticeTimer <= 0f) HideNotice();
    }

    private void HideNotice()
    {
        noticeTimer = 0f;
        noticeText.gameObject.SetActive(false);
    }

    // 영수증 동안은 시야를 끄고 커서를 보여 준다. 이때 번 돈을 더해 다음날로 저장한다
    private void ShowReceipt()
    {
        receiptShown = true;
        shop.SaveEndOfDay(day + 1, income);
        receiptPanel.SetActive(true);
        view.viewEnabled = false;
        pointer.SetTarget(null, "");
        // 상자가 아래에서 올라오고, 수입 숫자가 굴러 올라간다 (0원이면 굴리지 않는다)
        StartCoroutine(effects.SlideUp(receiptBox, ReceiptSlideDistance, ReceiptSlideSeconds));
        string head = day + "일차 영수증\n\n판매 " + salesCount + "잔 · 실패 " + failCount + "잔\n떠난 손님 " + leftCount + "명\n\n수입 ";
        string tail = "원\n<size=80%>보유 " + shop.Money().ToString("N0") + "원</size>";
        if (income > 0)
        {
            StartCoroutine(effects.CountUp(receiptText, head, 0, income, tail, ReceiptCountSeconds));
            effects.Play(effects.coinClip);
        }
        else
        {
            receiptText.text = head + "0" + tail;
        }
        Debug.Log("[CafeLoop] 영수증: 판매 " + salesCount + ", 실패 " + failCount + ", 떠남 " + leftCount + ", 수입 " + income + ", 보유 " + shop.Money());
    }

    // 영수증의 '상점' 버튼
    public void OpenShop()
    {
        receiptPanel.SetActive(false);
        shop.Open(day + 1);
    }

    // 상점의 'N일차 영업 시작' 버튼
    public void NextDay()
    {
        shop.Close();
        shop.ClearPending();
        receiptPanel.SetActive(false);
        view.viewEnabled = true;
        day++;
        dayTimer = daySeconds;
        dayOver = false;
        receiptShown = false;
        ResetDayStats();
        ShowDay();
        ShowIncome();
        UpdateTargets();
        OpenDay();
    }

    // 저장 가능: 카페 화면에 서 있을 때(손님 없음·수락 대기·제조 중·제공 대기)와 정산·상점
    // 미니게임(지시어·플레이·결과)과 전환·걷기·로딩 중에는 안 된다
    public bool CanSave()
    {
        // 정산·상점은 opening이 켜진 채일 수 있다 (나갔다 들어와 상점부터 열 때)
        if (state != StateCafe) return false;
        return !opening || shop.ShopPending();
    }

    // 저장 버튼 아래 이유 글자
    public string SaveBlockedReason()
    {
        if (state == StateInstruction || state == StatePlaying || state == StateResult) return "미니게임 중에는 저장할 수 없어요";
        return "지금은 저장할 수 없어요";
    }

    // 저장 슬롯에 쓸 데이터: 작업본 + 하루 중 상태 + 표시용 정보. 정산·상점이면 하루 중 상태는 담지 않는다
    public SaveData MakeSave()
    {
        SaveData data = shop.ExportSave();
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.playDay = day;
        data.customersPerDay = CustomersPerDay();
        data.menuCount = OpenMenuCount();
        data.machineCount = MachineCount();
        data.situation = SituationText(data.shopPending);
        data.menuIndex = -1;
        data.stepVariants = new int[0];
        data.request = "";
        data.cup = new Cup();
        if (data.shopPending) return data;
        data.dayTimer = dayTimer;
        data.customersToday = customersToday;
        data.income = income;
        data.salesCount = salesCount;
        data.failCount = failCount;
        data.leftCount = leftCount;
        data.combo = combo;
        data.dayOver = dayOver;
        data.nextCustomerTimer = nextCustomerTimer;
        data.order = order;
        if (order == OrderNone) return data;
        data.menuIndex = System.Array.IndexOf(menus, menu);
        data.stepIndex = stepIndex;
        data.successCount = successCount;
        data.priceSum = priceSum;
        data.stepVariants = stepVariants;
        data.cup = CopyOfCup(orderCup);
        data.patience = patience;
        data.request = RequestText();
        return data;
    }

    // 슬롯에서 불러왔으면(SaveSlots.pending) 하루 중 상태를 되살린다. 정산·상점 저장은 BeginGame의 상점 경로가 맡는다
    // 손님 등장 소리·새 레시피 꺼내기(TakeNewMenu)는 다시 하지 않는다
    private void RestoreSave()
    {
        SaveData data = SaveSlots.pending;
        SaveSlots.pending = null;
        if (data == null || data.shopPending) return;
        loadedSave = true;
        dayTimer = data.dayTimer;
        customersToday = data.customersToday;
        income = data.income;
        salesCount = data.salesCount;
        failCount = data.failCount;
        leftCount = data.leftCount;
        combo = data.combo;
        dayOver = data.dayOver;
        nextCustomerTimer = data.nextCustomerTimer;
        receiptTimer = receiptDelay;
        if (data.order != OrderNone && data.menuIndex >= 0 && data.menuIndex < menus.Length)
        {
            menu = menus[data.menuIndex];
            order = data.order;
            stepIndex = data.stepIndex;
            successCount = data.successCount;
            priceSum = data.priceSum;
            stepVariants = data.stepVariants;
            orderCup = data.cup != null ? CopyOfCup(data.cup) : new Cup();
            patience = data.patience;
            patienceMax = PatienceOf(menu);
            customer.Show(menu, data.request);
        }
        ShowDay();
        ShowIncome();
        UpdateTargets();
        Debug.Log("[CafeLoop] 불러오기: " + day + "일차, " + data.situation);
    }

    // 저장·복원할 때 같은 객체를 공유하지 않게 복사한다
    private Cup CopyOfCup(Cup source)
    {
        Cup copy = new Cup();
        copy.espresso = source.espresso;
        copy.milk = source.milk;
        copy.water = source.water;
        copy.ice = source.ice;
        return copy;
    }

    // 손님 말풍선의 요청 글자 (RollVariants가 만든 것과 같음)
    private string RequestText()
    {
        string request = "";
        for (int i = 0; i < menu.steps.Length; i++)
        {
            string label = VariantLabel(menu.steps[i], stepVariants[i]);
            if (label == "") continue;
            if (request != "") request += ", ";
            request += label;
        }
        return request;
    }

    // 열린 메뉴 수
    private int OpenMenuCount()
    {
        int count = 0;
        for (int i = 0; i < menus.Length; i++)
        {
            if (shop.IsMenuOpen(menus[i])) count++;
        }
        return count;
    }

    // 카페에 놓인 기계 수 (안 산 기계는 꺼져 있다)
    private int MachineCount()
    {
        int count = 0;
        for (int i = 0; i < stations.Length; i++)
        {
            if (!stations[i].gameObject.activeInHierarchy) continue;
            Transform parent = stations[i].transform.parent;
            if (parent != null && parent.GetComponentInParent<Station>() != null) continue;
            count++;
        }
        return count;
    }

    // 슬롯 칸의 상황 한 줄
    private string SituationText(bool shopPending)
    {
        if (shopPending) return "정산·상점";
        switch (order)
        {
            case OrderWaitAccept: return menu.menuName + " 주문 대기";
            case OrderMaking: return menu.menuName + " 제조 중 (" + stepIndex + "/" + menu.steps.Length + ")";
            case OrderWaitServe: return menu.menuName + " 건네기 직전";
        }
        return dayOver ? "영업 마감 · 정산 대기" : "손님 기다리는 중";
    }

    // 하루 통계 초기화
    private void ResetDayStats()
    {
        combo = 0;
        salesCount = 0;
        failCount = 0;
        leftCount = 0;
        income = 0;
        customersToday = 0;
    }

    private void ShowIncome()
    {
        incomeText.text = "수입 " + income + "원";
    }

    // 상태에 맞는 글자만 켠다
    private void ShowTexts()
    {
        instructionText.gameObject.SetActive(state == StateInstruction);
        timeText.gameObject.SetActive(state == StatePlaying);
        resultText.gameObject.SetActive(state == StateResult);
        // 지시어·결과는 알림과 같은 자리라 겹치지 않게 알림을 숨긴다
        if (state == StateInstruction || state == StateResult) HideNotice();
        if (state == StateInstruction)
        {
            instructionText.text = current.instruction;
            string label = VariantLabel(current.stationId, current.variant);
            if (label != "") instructionText.text = current.instruction + "\n<size=60%>" + label + "</size>";
            // 처음 들어간 미니게임은 조작 한 줄을 덧붙인다 (매번 원문에서 새로 만들어 누적되지 않는다)
            string control = showControlHint ? ControlLabel(current.stationId) : "";
            if (control != "") instructionText.text += "\n<size=60%>조작: " + control + "</size>";
        }
        if (state == StatePlaying) timeText.text = "남은 시간 " + current.timeLimit.ToString("0.0") + "초";
        if (state == StateResult) ShowResult();
    }

    // 결과 화면 연출: 성공은 등급 색으로 튕기고, 실패는 붉게 흔들린다
    private void ShowResult()
    {
        resultText.text = current.resultText;
        if (current.isSuccess) ShowSuccessResult();
        else ShowFailResult();
    }

    // 등급: 완벽(금색, 크게 튕김) / 좋아요(노랑) / 아슬아슬(주황)
    private void ShowSuccessResult()
    {
        int grade = GradeOf(current.quality);
        if (grade == GradePerfect)
        {
            resultText.text = "완벽!";
            resultText.color = PerfectColor;
            StartCoroutine(effects.Pop(resultText.rectTransform, PopPerfect));
        }
        else if (grade == GradeGood)
        {
            resultText.text = "좋아요!";
            resultText.color = GoodColor;
            StartCoroutine(effects.Pop(resultText.rectTransform, PopGood));
        }
        else
        {
            resultText.text = "아슬아슬!";
            resultText.color = CloseColor;
            StartCoroutine(effects.Pop(resultText.rectTransform, PopClose));
        }
        if (combo >= TipMinCombo) resultText.text += "\n<size=45%>콤보 ×" + combo + "</size>";
        effects.Play(effects.successClip);
    }

    private void ShowFailResult()
    {
        resultText.color = FailColor;
        StartCoroutine(effects.Pop(resultText.rectTransform, PopFail));
        StartCoroutine(effects.Shake(resultText.rectTransform, ShakeFail));
        effects.Play(effects.failClip);
    }
}
