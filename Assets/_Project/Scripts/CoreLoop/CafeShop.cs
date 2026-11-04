using TMPro;
using UnityEngine;

// 하루 정산(영수증) 뒤에 여는 상점. 기계·레시피를 사면 다음날부터 쓴다
// 진행(날짜·보유 금액·산 물건)은 PlayerPrefs에 저장한다. 날짜·돈은 하루가 끝날 때만 저장하므로
// 하루 도중 메인으로 나가면 그날 진행은 버려지고, 다시 시작하면 그날 아침부터 한다
public class CafeShop : MonoBehaviour
{
    [System.Serializable]
    public class Item
    {
        [Tooltip("저장 키. 바꾸면 산 기록이 사라진다")]
        public string id;
        public string title;
        public string description;
        public int price;
        public Sprite icon;
        [Tooltip("기계면 체크. 사기 전에는 stationObject를 숨긴다")]
        public bool isMachine;
        public GameObject stationObject;
        [Tooltip("레시피면 이 메뉴를 손님이 주문하게 된다. 상점에 없는 메뉴는 처음부터 주문")]
        public MenuData menu;
        [Tooltip("먼저 사야 하는 물건 id")]
        public string[] requires;
    }

    public Item[] items;
    public CafeEffects effects;

    [Header("UI")]
    public GameObject panel;
    [Tooltip("아래에서 올라오는 상자")]
    public RectTransform box;
    public TMP_Text moneyText;
    [Tooltip("'N일차 영업 시작' 버튼 글자")]
    public TMP_Text startLabel;
    [Tooltip("물건 칸 원본 (꺼 둔다). 물건 수만큼 복제한다")]
    public ShopCard cardTemplate;
    public Color machineColor = new Color(0.85f, 0.9f, 0.93f);
    public Color recipeColor = new Color(0.98f, 0.88f, 0.76f);
    public Color ownedColor = new Color(0.82f, 0.92f, 0.78f);
    public Color lockedColor = new Color(0.8f, 0.78f, 0.75f);

    private const string DayKey = "bari_day";
    private const string MoneyKey = "bari_money";
    private const string OwnKey = "bari_own_";
    // 영수증까지 끝났고 상점을 아직 안 닫았다 (이때 나가면 다시 들어와서 상점부터)
    private const string PendingKey = "bari_shop_pending";
    // 가장 최근에 산 레시피 id. 다음날 첫 손님이 주문한다
    private const string NewMenuKey = "bari_new_menu";

    private ShopCard[] cards;
    private int money;

    // 저장된 진행을 읽고, 사지 않은 기계를 숨긴다
    public void Load()
    {
        money = PlayerPrefs.GetInt(MoneyKey, 0);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].isMachine && items[i].stationObject != null) items[i].stationObject.SetActive(Owns(items[i].id));
        }
    }

    public int SavedDay()
    {
        return PlayerPrefs.GetInt(DayKey, 1);
    }

    public int Money()
    {
        return money;
    }

    public bool Owns(string id)
    {
        return PlayerPrefs.GetInt(OwnKey + id, 0) == 1;
    }

    // 상점에서 파는 레시피면 산 뒤에만 주문이 들어온다
    public bool IsMenuOpen(MenuData menu)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].menu == menu) return Owns(items[i].id);
        }
        return true;
    }

    // 어제 산 레시피 메뉴 (한 번 꺼내면 지워진다). 없으면 null
    public MenuData TakeNewMenu()
    {
        string id = PlayerPrefs.GetString(NewMenuKey, "");
        if (id == "") return null;
        PlayerPrefs.DeleteKey(NewMenuKey);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].id == id) return items[i].menu;
        }
        return null;
    }

    public bool ShopPending()
    {
        return PlayerPrefs.GetInt(PendingKey, 0) == 1;
    }

    // 상점을 닫고 다음날을 시작할 때
    public void ClearPending()
    {
        PlayerPrefs.DeleteKey(PendingKey);
        PlayerPrefs.Save();
    }

    // 하루가 끝나 영수증이 뜰 때: 번 돈을 더하고 다음날로 저장
    public void SaveEndOfDay(int nextDay, int income)
    {
        money += income;
        PlayerPrefs.SetInt(DayKey, nextDay);
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.SetInt(PendingKey, 1);
        PlayerPrefs.Save();
    }

    // 저장 지우기 (치트 키). 기계도 다시 숨긴다
    public void ResetSave()
    {
        PlayerPrefs.DeleteKey(DayKey);
        PlayerPrefs.DeleteKey(MoneyKey);
        PlayerPrefs.DeleteKey(PendingKey);
        PlayerPrefs.DeleteKey(NewMenuKey);
        for (int i = 0; i < items.Length; i++) PlayerPrefs.DeleteKey(OwnKey + items[i].id);
        PlayerPrefs.Save();
    }

    // 치트 키: 돈 더하기 (바로 저장)
    public void AddMoney(int amount)
    {
        money += amount;
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        if (panel.activeSelf) Refresh();
    }

    public void Open(int nextDay)
    {
        if (cards == null) BuildCards();
        panel.SetActive(true);
        startLabel.text = nextDay + "일차 영업 시작";
        Refresh();
        StartCoroutine(effects.SlideUp(box, 600f, 0.35f));
    }

    public void Close()
    {
        panel.SetActive(false);
    }

    // 물건 칸 버튼
    public void Buy(int index)
    {
        Item item = items[index];
        if (Owns(item.id) || !RequirementsMet(item) || money < item.price) return;
        money -= item.price;
        PlayerPrefs.SetInt(OwnKey + item.id, 1);
        PlayerPrefs.SetInt(MoneyKey, money);
        PlayerPrefs.Save();
        effects.Play(effects.coinClip);
        if (item.isMachine && item.stationObject != null) item.stationObject.SetActive(true);
        if (item.menu != null) PlayerPrefs.SetString(NewMenuKey, item.id);
        PlayerPrefs.Save();
        Refresh();
        StartCoroutine(effects.Pop(cards[index].GetComponent<RectTransform>(), 1.08f));
        Debug.Log("[CafeShop] 구매: " + item.title + " -" + item.price + "원, 남은 돈 " + money);
    }

    private void BuildCards()
    {
        cards = new ShopCard[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            ShopCard card = Instantiate(cardTemplate, cardTemplate.transform.parent);
            card.gameObject.SetActive(true);
            card.shop = this;
            card.index = i;
            cards[i] = card;
        }
        cardTemplate.gameObject.SetActive(false);
    }

    private void Refresh()
    {
        moneyText.text = "보유 " + money.ToString("N0") + "원";
        for (int i = 0; i < items.Length; i++)
        {
            Item item = items[i];
            ShopCard card = cards[i];
            card.kindText.text = item.isMachine ? "기계" : "레시피";
            card.titleText.text = item.title;
            card.descText.text = item.description;
            card.icon.sprite = item.icon;
            card.icon.gameObject.SetActive(item.icon != null);
            card.priceText.text = item.price.ToString("N0") + "원";
            card.priceText.color = new Color(0.35f, 0.22f, 0.13f);

            if (Owns(item.id))
            {
                card.background.color = ownedColor;
                card.buttonLabel.text = "보유 중";
                card.button.interactable = false;
                card.priceText.text = "구매 완료";
            }
            else if (!RequirementsMet(item))
            {
                card.background.color = lockedColor;
                card.buttonLabel.text = MissingTitle(item) + " 필요";
                card.button.interactable = false;
            }
            else
            {
                card.background.color = item.isMachine ? machineColor : recipeColor;
                bool afford = money >= item.price;
                card.buttonLabel.text = afford ? "구매" : "돈 부족";
                card.button.interactable = afford;
                if (!afford) card.priceText.color = new Color(0.75f, 0.25f, 0.2f);
            }
            card.lockIcon.SetActive(!Owns(item.id) && !RequirementsMet(item));
        }
    }

    private bool RequirementsMet(Item item)
    {
        if (item.requires == null) return true;
        for (int i = 0; i < item.requires.Length; i++)
        {
            if (!Owns(item.requires[i])) return false;
        }
        return true;
    }

    // 아직 안 산 선행 물건의 이름
    private string MissingTitle(Item item)
    {
        for (int i = 0; i < item.requires.Length; i++)
        {
            if (Owns(item.requires[i])) continue;
            for (int j = 0; j < items.Length; j++)
            {
                if (items[j].id == item.requires[i]) return items[j].title;
            }
        }
        return "";
    }
}
