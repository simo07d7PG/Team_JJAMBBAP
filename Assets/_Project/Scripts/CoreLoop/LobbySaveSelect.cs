using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 로비 '게임 시작'을 누르면 뜨는 패널. 이어하기 / 슬롯 1~3 불러오기 / 새 게임 중에서 고른다
// 이 컴포넌트를 패널 오브젝트에 붙이고 꺼 둔다 ('게임 시작' 버튼 → Open)
// 슬롯 불러오기와 새 게임은 작업본을 덮어쓰므로, 작업본이 있으면 같은 칸을 한 번 더 눌러야 한다
public class LobbySaveSelect : MonoBehaviour
{
    [Header("이어하기")]
    public Image continueBackground;
    [Tooltip("작업본 정보 (없으면 '진행 중인 게임 없음')")]
    public TMP_Text continueInfo;
    public Button continueButton;

    [Header("슬롯")]
    [Tooltip("슬롯 칸 3개 (위에서부터 1~3번)")]
    public SaveSlotCard[] cards;

    [Header("새 게임")]
    public Image newGameBackground;
    public TMP_Text newGameLabel;

    [Header("색")]
    public Color emptyColor = new Color(0.8f, 0.78f, 0.75f);
    public Color filledColor = new Color(0.98f, 0.88f, 0.76f);
    [Tooltip("한 번 더 누르기를 기다리는 칸")]
    public Color armedColor = new Color(1f, 0.75f, 0.6f);

    // 한 번 더 누르기를 기다리는 칸: 0~2 = 슬롯, ArmedNewGame = 새 게임
    private const int ArmedNone = -1;
    private const int ArmedNewGame = SaveSlots.Count;
    private int armed = ArmedNone;

    // 발표용 슬롯(F8): 3번 슬롯을 덮어쓴다. 진행이 있으면 2초 안에 한 번 더 눌러야 한다
    private const int DemoSlot = 2;
    private const float DemoConfirmSeconds = 2f;
    private float demoArmedUntil = -1f;
    private TMP_Text demoNotice;

    private void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].lobby = this;
            cards[i].index = i;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.f8Key.wasPressedThisFrame) PressDemoKey();
        if (demoArmedUntil > 0f && Time.unscaledTime > demoArmedUntil)
        {
            demoArmedUntil = -1f;
            if (demoNotice != null) demoNotice.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        demoArmedUntil = -1f;
        if (demoNotice != null) demoNotice.gameObject.SetActive(false);
    }

    private void PressDemoKey()
    {
        bool armedNow = demoArmedUntil > 0f && Time.unscaledTime <= demoArmedUntil;
        if (SaveSlots.Read(DemoSlot) != null && !armedNow)
        {
            demoArmedUntil = Time.unscaledTime + DemoConfirmSeconds;
            ShowDemoNotice("F8 한 번 더: 3번 슬롯을 발표용으로 덮어씁니다");
            return;
        }
        demoArmedUntil = -1f;
        if (SaveSlots.Write(DemoSlot, SaveSlots.BuildDemo()))
        {
            armed = ArmedNone;
            Refresh();
            ShowDemoNotice("3번 슬롯을 발표용 데이터로 채웠어요");
            demoArmedUntil = Time.unscaledTime + DemoConfirmSeconds;
        }
    }

    // 패널 아래쪽에 작은 안내 글자를 코드로 만들어 쓴다
    private void ShowDemoNotice(string message)
    {
        if (demoNotice == null)
        {
            GameObject go = new GameObject("DemoNotice", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(1200f, 60f);
            demoNotice = go.AddComponent<TextMeshProUGUI>();
            if (continueInfo != null) demoNotice.font = continueInfo.font;
            demoNotice.fontSize = 32f;
            demoNotice.alignment = TextAlignmentOptions.Center;
            demoNotice.color = new Color(1f, 0.9f, 0.5f);
            demoNotice.raycastTarget = false;
        }
        demoNotice.text = message;
        demoNotice.gameObject.SetActive(true);
        demoNotice.transform.SetAsLastSibling();
    }

    // '게임 시작' 버튼
    public void Open()
    {
        gameObject.SetActive(true);
        armed = ArmedNone;
        Refresh();
    }

    // '닫기' 버튼
    public void Close()
    {
        armed = ArmedNone;
        gameObject.SetActive(false);
    }

    // '이어하기' 버튼: 지금 작업본으로 시작
    public void Continue()
    {
        if (!CafeShop.HasProgress()) return;
        SaveSlots.StartGameScene(null, false);
    }

    // 슬롯 칸 버튼
    public void ClickSlot(int slot)
    {
        SaveData data = SaveSlots.Read(slot);
        if (data == null) return;
        if (NeedsConfirm(slot)) return;
        SaveSlots.StartGameScene(data, false);
    }

    // '새 게임' 버튼: 작업본을 지우고 1일차부터 (슬롯은 그대로)
    public void NewGame()
    {
        if (NeedsConfirm(ArmedNewGame)) return;
        SaveSlots.StartGameScene(null, true);
    }

    // 작업본이 있는데 이 칸을 처음 눌렀으면 확인을 기다리게 하고 true
    private bool NeedsConfirm(int target)
    {
        if (!CafeShop.HasProgress() || armed == target) return false;
        armed = target;
        Refresh();
        return true;
    }

    private void Refresh()
    {
        bool has = CafeShop.HasProgress();
        continueButton.interactable = has;
        continueBackground.color = has ? filledColor : emptyColor;
        continueInfo.text = has ? ProgressText() : "진행 중인 게임 없음";

        for (int i = 0; i < cards.Length; i++)
        {
            FillCard(cards[i], i, SaveSlots.Read(i));
        }

        bool newArmed = armed == ArmedNewGame;
        newGameBackground.color = newArmed ? armedColor : filledColor;
        newGameLabel.text = newArmed ? "한 번 더 누르면\n처음부터" : "새 게임";
    }

    // 이어하기 칸: 'N일차 · 보유 X원'. 정산·상점 대기면 마감한 날짜로
    private string ProgressText()
    {
        string money = "보유 " + CafeShop.ReadMoney().ToString("N0") + "원";
        if (CafeShop.ReadShopPending()) return (CafeShop.ReadDay() - 1) + "일차 마감 · 상점부터\n" + money;
        return CafeShop.ReadDay() + "일차 아침부터\n" + money;
    }

    private void FillCard(SaveSlotCard card, int slot, SaveData data)
    {
        card.slotText.text = (slot + 1) + "번";
        bool slotArmed = armed == slot;
        card.button.interactable = data != null;
        if (data == null)
        {
            card.infoText.text = "비어 있음";
            card.background.color = emptyColor;
            card.buttonLabel.text = "불러오기";
            return;
        }
        card.infoText.text = SaveSlots.Describe(data);
        card.background.color = slotArmed ? armedColor : filledColor;
        card.buttonLabel.text = slotArmed ? "한 번 더 누르면\n불러와요" : "불러오기";
    }
}
