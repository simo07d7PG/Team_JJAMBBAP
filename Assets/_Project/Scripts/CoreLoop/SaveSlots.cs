using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// 저장 슬롯 파일 읽기·쓰기 (persistentDataPath/save_slot1~3.json)
// 불러오기·새 게임은 할 일을 pending·newGame에 맡겨 두고 GameScene을 읽는다. 적용은 CafeLoop.Start (ESC 메뉴·로비 공통)
public static class SaveSlots
{
    public const int Count = 3;
    private const string GameSceneName = "GameScene";

    // 씬 시작 때 작업본에 덮어쓰고 하루 중 상태를 되살릴 데이터. 적용하면 null
    public static SaveData pending;
    // 씬 시작 때 작업본을 지우고 1일차부터 (슬롯 파일은 그대로)
    public static bool newGame;

    // data: 불러올 슬롯 (없으면 null), fresh: 새 게임. 둘 다 아니면 지금 작업본으로 시작
    public static void StartGameScene(SaveData data, bool fresh)
    {
        pending = data;
        newGame = fresh;
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }

    // 슬롯 칸 정보 5줄: 날짜·시간·손님 / 돈 / 메뉴·기계 / 상황 / 저장 시각
    public static string Describe(SaveData data)
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

    // 발표용 데이터: 3일차 아침, 6,000원, 기계·레시피를 모두 산 상태 (id는 GameScene의 CafeShop 물건 id)
    public static SaveData BuildDemo()
    {
        SaveData data = new SaveData();
        data.day = 3;
        data.money = 6000;
        data.shopPending = false;
        data.newMenuId = "";
        data.owned = new string[]
        {
            "ice_machine", "milk_fridge",
            "recipe_americano", "recipe_iced_americano", "recipe_latte", "recipe_iced_latte",
            "recipe_hand_drip", "recipe_machine_latte", "recipe_latte_art"
        };
        data.dayTimer = 75f;
        data.nextCustomerTimer = 2f;
        data.order = 0;
        data.menuIndex = -1;
        data.stepVariants = new int[0];
        data.request = "";
        data.cup = new Cup();
        data.savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.playDay = 3;
        data.customersPerDay = 5;
        data.menuCount = 8;
        data.machineCount = 8;
        data.situation = "발표용 데이터";
        return data;
    }

    // slot: 0부터
    private static string PathOf(int slot)
    {
        return Path.Combine(Application.persistentDataPath, "save_slot" + (slot + 1) + ".json");
    }

    // 빈 칸이거나 읽지 못하면 null
    public static SaveData Read(int slot)
    {
        string path = PathOf(slot);
        if (!File.Exists(path)) return null;
        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveSlots] 읽기 실패 " + path + ": " + e.Message);
            return null;
        }
    }

    // 쓰지 못하면 false
    public static bool Write(int slot, SaveData data)
    {
        string path = PathOf(slot);
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(data));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveSlots] 쓰기 실패 " + path + ": " + e.Message);
            return false;
        }
        Debug.Log("[SaveSlots] 저장: " + path);
        return true;
    }
}
