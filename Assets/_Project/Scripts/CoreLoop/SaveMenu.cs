using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ESC 메뉴의 저장하기·불러오기. 누르면 슬롯 3칸 패널이 뜬다
// ESC 패널(targetPanel) 안에 둔다. ESC 메뉴가 timeScale 0으로 열리므로 시간에 기대는 연출은 쓰지 않는다
// 찬 칸에 저장하거나 불러올 때는 같은 칸을 한 번 더 눌러야 한다 (다른 칸을 누르거나 닫으면 취소)
public class SaveMenu : MonoBehaviour
{
    public CafeLoop loop;
    public CafeShop shop;

    [Header("ESC 메뉴 버튼")]
    [Tooltip("저장할 수 없는 때는 누를 수 없게 한다")]
    public Button saveButton;
    [Tooltip("저장할 수 없는 이유")]
    public TMP_Text blockedText;

    [Header("슬롯 패널")]
    public GameObject slotPanel;
    [Tooltip("'저장하기' / '불러오기'")]
    public TMP_Text titleText;
    [Tooltip("'저장했어요' 같은 결과 알림")]
    public TMP_Text messageText;
    [Tooltip("슬롯 칸 3개 (위에서부터 1~3번)")]
    public SaveSlotCard[] cards;
    public Color emptyColor = new Color(0.8f, 0.78f, 0.75f);
    public Color filledColor = new Color(0.98f, 0.88f, 0.76f);
    [Tooltip("한 번 더 누르기를 기다리는 칸")]
    public Color armedColor = new Color(1f, 0.75f, 0.6f);

    // 저장 모드면 true, 불러오기 모드면 false
    private bool saving;
    // 한 번 더 누르기를 기다리는 칸 (-1 = 없음)
    private int armedSlot = -1;

    private void Awake()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].menu = this;
            cards[i].index = i;
        }
    }

    // ESC 메뉴가 열릴 때마다 슬롯 패널은 닫힌 상태로, 저장 가능 여부는 새로
    private void OnEnable()
    {
        slotPanel.SetActive(false);
        armedSlot = -1;
        bool can = loop.CanSave();
        saveButton.interactable = can;
        blockedText.text = can ? "" : loop.SaveBlockedReason();
    }

    // ESC 메뉴의 '저장하기' 버튼
    public void OpenSave()
    {
        if (!loop.CanSave()) return;
        saving = true;
        OpenPanel();
    }

    // ESC 메뉴의 '불러오기' 버튼
    public void OpenLoad()
    {
        saving = false;
        OpenPanel();
    }

    // 슬롯 패널의 '닫기' 버튼
    public void Close()
    {
        armedSlot = -1;
        slotPanel.SetActive(false);
    }

    private void OpenPanel()
    {
        armedSlot = -1;
        titleText.text = saving ? "저장하기" : "불러오기";
        messageText.text = "";
        slotPanel.SetActive(true);
        RefreshCards();
    }

    // 슬롯 칸 버튼
    public void ClickSlot(int slot)
    {
        SaveData data = SaveSlots.Read(slot);
        if (!saving && data == null) return;
        // 찬 칸은 처음 누르면 확인만 기다린다
        if (data != null && armedSlot != slot)
        {
            armedSlot = slot;
            messageText.text = "";
            RefreshCards();
            return;
        }
        if (saving) SaveTo(slot);
        else LoadFrom(data);
    }

    private void SaveTo(int slot)
    {
        armedSlot = -1;
        if (!loop.CanSave())
        {
            messageText.text = loop.SaveBlockedReason();
            RefreshCards();
            return;
        }
        bool ok = SaveSlots.Write(slot, loop.MakeSave());
        messageText.text = ok ? (slot + 1) + "번 칸에 저장했어요" : "저장하지 못했어요";
        RefreshCards();
    }

    // 작업본을 덮어쓰고 씬을 다시 읽는다. 다시 읽은 CafeLoop가 pending을 적용한다
    private void LoadFrom(SaveData data)
    {
        shop.ApplySave(data);
        SaveSlots.pending = data;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void RefreshCards()
    {
        for (int i = 0; i < cards.Length; i++)
        {
            FillCard(cards[i], i, SaveSlots.Read(i));
        }
    }

    private void FillCard(SaveSlotCard card, int slot, SaveData data)
    {
        card.slotText.text = (slot + 1) + "번";
        bool armed = armedSlot == slot;
        if (data == null)
        {
            card.infoText.text = "비어 있음";
            card.background.color = emptyColor;
        }
        else
        {
            card.infoText.text = Describe(data);
            card.background.color = armed ? armedColor : filledColor;
        }
        card.button.interactable = saving || data != null;
        if (armed) card.buttonLabel.text = saving ? "한 번 더 누르면\n덮어써요" : "한 번 더 누르면\n불러와요";
        else if (!saving) card.buttonLabel.text = "불러오기";
        else card.buttonLabel.text = data == null ? "저장" : "덮어쓰기";
    }

    // 슬롯 칸 정보 5줄: 날짜·시간·손님 / 돈 / 메뉴·기계 / 상황 / 저장 시각
    private string Describe(SaveData data)
    {
        string head;
        string money = "보유 " + data.money.ToString("N0") + "원";
        if (data.shopPending)
        {
            head = data.playDay + "일차 마감 · 상점";
        }
        else
        {
            string count = " · 손님 " + data.customersToday + "/" + data.customersPerDay;
            if (data.dayOver)
            {
                head = data.playDay + "일차 마감" + count;
            }
            else
            {
                int seconds = Mathf.CeilToInt(data.dayTimer);
                head = data.playDay + "일차 · 남은 시간 " + (seconds / 60) + ":" + (seconds % 60).ToString("00") + count;
            }
            money += " · 오늘 수입 " + data.income.ToString("N0") + "원";
        }
        string owned = "메뉴 " + data.menuCount + "종 · 기계 " + data.machineCount + "대";
        return head + "\n" + money + "\n" + owned + "\n" + data.situation + "\n<size=80%>" + data.savedAt + "</size>";
    }
}
