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
        try
        {
            return DescribeCore(data);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveSlots] 칸 정보 만들기 실패: " + e.Message);
            return "저장 정보를 읽을 수 없어요";
        }
    }

    private static string DescribeCore(SaveData data)
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
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[SaveSlots] 빈 슬롯 파일: " + path);
                return null;
            }
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return null;
            Sanitize(data);
            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[SaveSlots] 읽기 실패 " + path + ": " + e.Message);
            return null;
        }
    }

    // 깨졌거나 옛 버전 파일에서 빠진 값을 안전한 값으로 채운다
    private static void Sanitize(SaveData data)
    {
        if (data.day < 1) data.day = 1;
        if (data.money < 0) data.money = 0;
        if (data.playDay < 1) data.playDay = data.day;
        if (data.owned == null) data.owned = new string[0];
        if (data.newMenuId == null) data.newMenuId = "";
        if (data.stepVariants == null) data.stepVariants = new int[0];
        if (data.request == null) data.request = "";
        if (data.bestName == null) data.bestName = "";
        if (data.cup == null) data.cup = new Cup();
        if (data.savedAt == null) data.savedAt = "";
        if (data.situation == null) data.situation = "";
    }

    // 쓰지 못하면 false
    public static bool Write(int slot, SaveData data)
    {
        string path = PathOf(slot);
        try
        {
            // 임시 파일에 먼저 쓴 뒤 교체해서 반쯤 써진 슬롯 파일을 남기지 않는다
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data));
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tmp, path, null);
                }
                catch (System.Exception)
                {
                    File.Copy(tmp, path, true);
                    File.Delete(tmp);
                }
            }
            else
            {
                File.Move(tmp, path);
            }
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
