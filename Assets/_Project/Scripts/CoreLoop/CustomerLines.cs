using UnityEngine;

// 손님 말풍선 문구. 메뉴 이름은 항상 그대로 포함한다.
public static class CustomerLines
{
    private static readonly string[] Templates =
    {
        "{0} 하나 주세요!",
        "{0} 한 잔 부탁드려요~",
        "안녕하세요! {0} 주세요.",
        "{0} 있나요? 하나 주세요!",
        "오늘은 {0} 마시고 싶어요!",
    };

    private static int last = -1;

    // 미니게임 한 단계 직후 반응 (성공 / 실패 / 마지막 단계)
    private static readonly string[] StepGood = { "오, 좋아요!", "와, 잘하시네요!", "좋은데요?", "오~ 솜씨 좋아요!" };
    private static readonly string[] StepBad = { "어… 괜찮은 거죠?", "음… 조금 불안한데요.", "앗, 괜찮을까요?", "어어… 계속 가요!" };
    private static readonly string[] StepLast = { "다 됐나요?", "이제 다 된 거죠?", "완성인가요? 기대돼요!" };
    private static string lastStepLine;

    public static string StepReaction(bool success, bool last)
    {
        string[] pool = last ? StepLast : (success ? StepGood : StepBad);
        string line = pool[Random.Range(0, pool.Length)];
        for (int tries = 0; tries < 8 && line == lastStepLine && pool.Length > 1; tries++) line = pool[Random.Range(0, pool.Length)];
        lastStepLine = line;
        return line;
    }

    private static string lastReview;

    // 건넬 때 한마디. results: 단계별 0 실패/1 성공, names: 단계 이름. 맞지 않으면 null (호출부가 기존 문구로 대체)
    public static string Review(int[] results, string[] names)
    {
        if (results == null || names == null || results.Length == 0 || results.Length != names.Length) return null;
        int fail = -1, good = 0, ok = 0;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] == 1) ok++;
            else if (fail < 0) fail = i;
        }
        string line;
        if (ok == results.Length)
        {
            good = Random.Range(0, results.Length);
            string n = names[good];
            if (string.IsNullOrEmpty(n)) return null;
            string[] t = { n + Josa(n) + " 딱 좋네요!", n + Josa(n) + " 정말 훌륭해요!", "와, " + n + Josa(n) + " 완벽해요!" };
            line = t[Random.Range(0, t.Length)];
        }
        else if (ok == 0)
        {
            string[] t = { "이건… 좀 아쉽네요…", "다음엔 더 잘 부탁드려요…", "음… 다시 만들어 주실래요?" };
            line = t[Random.Range(0, t.Length)];
        }
        else
        {
            string n = names[fail];
            if (string.IsNullOrEmpty(n)) return null;
            string[] t = { n + Josa(n) + " 좀 아쉬워요…", n + " 쪽만 조금 더 신경 써 주세요…", n + Josa(n) + " 살짝 부족했어요…" };
            line = t[Random.Range(0, t.Length)];
        }
        if (line == lastReview) return null;
        lastReview = line;
        return line;
    }

    // 받침 있으면 이, 없으면 가
    private static string Josa(string word)
    {
        char c = word[word.Length - 1];
        if (c >= 0xAC00 && c <= 0xD7A3) return (c - 0xAC00) % 28 != 0 ? "이" : "가";
        return "이";
    }

    // request가 있으면 둘째 줄에 작은 글자로 붙인다
    private static readonly string[] HurryTemplates =
    {
        "{0} 빨리요!", "{0} 얼른 주세요!", "바빠요! {0} 빨리 부탁해요!",
    };
    private static readonly string[] RelaxTemplates =
    {
        "{0} 천천히 부탁드려요~", "여유롭게 {0} 한 잔 주세요~", "음~ {0} 주세요. 천천히 해요~",
    };

    // 성격: 0 보통, 1 급한 손님, 2 느긋한 손님
    public static string PersonalityLabel(int personality)
    {
        return personality == 1 ? "급한 손님" : (personality == 2 ? "느긋한 손님" : "");
    }

    // 새 손님의 성격을 뽑는다. 보통 60% / 급함 20% / 느긋함 20%. forceNormal이면 항상 보통
    public static int RollPersonality(bool forceNormal)
    {
        if (forceNormal) return 0;
        float r = Random.value;
        return r < 0.6f ? 0 : (r < 0.8f ? 1 : 2);
    }

    public static string Order(string menuName, string request, int personality = 0)
    {
        string[] pool = personality == 1 ? HurryTemplates : (personality == 2 ? RelaxTemplates : Templates);
        int i = Random.Range(0, pool.Length);
        if (personality == 0)
        {
            if (i == last) i = (i + 1) % pool.Length;
            last = i;
        }
        string line = string.Format(pool[i], menuName);
        string tag = PersonalityLabel(personality);
        if (tag != "") line = "<size=60%>" + tag + "</size>\n" + line;
        if (!string.IsNullOrEmpty(request)) line += "\n<size=70%>" + request + "</size>";
        return line;
    }
}
