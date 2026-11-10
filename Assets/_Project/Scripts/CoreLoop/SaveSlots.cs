using System.IO;
using UnityEngine;

// 저장 슬롯 파일 읽기·쓰기 (persistentDataPath/save_slot1~3.json)
// 불러오기는 씬을 다시 읽으므로, 다시 읽은 CafeLoop가 적용할 데이터를 pending에 맡겨 둔다
public static class SaveSlots
{
    public const int Count = 3;

    // 불러온 뒤 씬 시작 때 적용할 데이터. 적용하면 null
    public static SaveData pending;

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
