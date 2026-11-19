// 저장 슬롯 하나에 담는 내용. JsonUtility로 save_slotN.json에 쓴다
// 정산·상점에서 저장했으면(shopPending) 하루 중 상태는 비어 있다
[System.Serializable]
public class SaveData
{
    // 작업본 (CafeShop의 PlayerPrefs 값 그대로)
    public int day;
    public int money;
    public string[] owned;
    public bool shopPending;
    public string newMenuId;

    // 하루 중 상태 (CafeLoop)
    public float dayTimer;
    public int customersToday;
    public int income;
    // 오늘의 베스트 음료 (구버전 슬롯엔 없다 → 빈 글자·0)
    public string bestName;
    public int bestPrice;
    public int salesCount;
    public int failCount;
    public int leftCount;
    public int combo;
    public bool dayOver;
    public int order;
    // CafeLoop.menus 안의 번호 (손님 없으면 -1)
    public int menuIndex;
    public int stepIndex;
    public int successCount;
    public float priceSum;
    public int[] stepVariants;
    // 손님 성격 (0 보통, 1 급한 손님, 2 느긋한 손님. 옛 슬롯엔 없다 → 0)
    public int personality;
    public float patience;
    public float nextCustomerTimer;
    // 손님 말풍선의 요청 글자 ("진하게, 우유 적게")
    public string request;
    // 주문 중 컵 내용물 (옛 슬롯 파일엔 없다 → 빈 컵)
    public Cup cup;

    // 슬롯 칸 표시용
    public string savedAt;
    public int playDay;
    public int customersPerDay;
    public int menuCount;
    public int machineCount;
    public string situation;
}
