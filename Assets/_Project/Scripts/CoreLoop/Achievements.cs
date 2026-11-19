using System;
using UnityEngine;

// 업적 정의와 저장. 슬롯 저장과 무관한 전역 값(PlayerPrefs)이라 새 게임을 해도 남는다
// 달성: bari_ach_<id> = 1, 진행도: bari_achp_<id>
public static class Achievements
{
    public class Def
    {
        public string id;
        public string title;
        public string description;
        public int target;

        public Def(string id, string title, string description, int target)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.target = target;
        }
    }

    private const string UnlockKey = "bari_ach_";
    private const string ProgressKey = "bari_achp_";

    public static readonly Def[] All =
    {
        new Def("first_cup", "첫 잔", "음료를 처음 판매한다", 1),
        new Def("sales_10", "단골 만들기", "음료를 누적 10잔 판매한다", 10),
        new Def("sales_30", "바리스타", "음료를 누적 30잔 판매한다", 30),
        new Def("perfect", "완벽주의", "모든 단계를 '완벽'으로 판매한다", 1),
        new Def("combo_3", "콤보 3", "콤보 3 이상으로 판매한다", 1),
        new Def("combo_5", "콤보 5", "콤보 5 이상으로 판매한다", 1),
        new Def("goal", "목표 달성", "하루 목표 수입을 달성한다", 1),
        new Def("first_buy", "큰손", "상점에서 처음 구매한다", 1),
        new Def("collector", "수집가", "상점의 모든 물건을 구매한다", 1),
        new Def("day_5", "5일차 도달", "5일차 영업을 시작한다", 1),
        new Def("no_leave", "무사고 영업", "떠난 손님 없이 하루를 마감한다", 1),
    };

    // 달성 순간 한 번 호출된다 (알림 표시용)
    public static event Action<Def> OnUnlocked;

    public static Def Find(string id)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].id == id) return All[i];
        }
        return null;
    }

    public static bool IsUnlocked(string id)
    {
        return PlayerPrefs.GetInt(UnlockKey + id, 0) == 1;
    }

    public static int Progress(string id)
    {
        Def def = Find(id);
        if (def == null) return 0;
        if (IsUnlocked(id)) return def.target;
        return Mathf.Min(def.target, PlayerPrefs.GetInt(ProgressKey + id, 0));
    }

    // 한 번에 달성되는 업적
    public static void Report(string id)
    {
        Def def = Find(id);
        if (def == null) return;
        AddProgress(id, def.target);
    }

    public static void AddProgress(string id, int amount)
    {
        Def def = Find(id);
        if (def == null || amount <= 0 || IsUnlocked(id)) return;
        int now = Mathf.Min(def.target, PlayerPrefs.GetInt(ProgressKey + id, 0) + amount);
        PlayerPrefs.SetInt(ProgressKey + id, now);
        if (now >= def.target)
        {
            PlayerPrefs.SetInt(UnlockKey + id, 1);
            PlayerPrefs.Save();
            Debug.Log("[Achievements] 달성: " + def.title);
            if (OnUnlocked != null) OnUnlocked(def);
        }
        else PlayerPrefs.Save();
    }

    public static int UnlockedCount()
    {
        int n = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (IsUnlocked(All[i].id)) n++;
        }
        return n;
    }

    // F9 저장 초기화 치트용
    public static void ResetAll()
    {
        for (int i = 0; i < All.Length; i++)
        {
            PlayerPrefs.DeleteKey(UnlockKey + All[i].id);
            PlayerPrefs.DeleteKey(ProgressKey + All[i].id);
        }
        PlayerPrefs.Save();
    }
}
