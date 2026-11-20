// 미니게임 설명 데이터. 각 *Microgame.cs의 실제 조작·판정 수치(프리팹 직렬화 값 포함)를 그대로 옮겼다.
// 수치를 바꾸면 여기 문구도 함께 고친다.
public static class MinigameGuide
{
    // icon 토큰: LMB, LMB_HOLD, RMB, RMB_HOLD, WHEEL, MOUSE_MOVE, MOUSE_CIRCLE, SPACE, KEY_Q, KEY_W, KEY_E, KEY_R, KEY_QWER
    public class Control
    {
        public string icon;
        public string caption;
    }

    public class Entry
    {
        public string stationId;
        public string title;
        public string goal;
        public Control[] controls;
        public string success;
        public string fail;
        public string tip;
    }

    // steps = stationId 순서
    public class MenuRecipe
    {
        public string menuName;
        public string[] steps;
    }

    private static Control C(string icon, string caption)
    {
        return new Control { icon = icon, caption = caption };
    }

    private static Entry E(string stationId, string title, string goal, Control[] controls, string success, string fail, string tip)
    {
        return new Entry { stationId = stationId, title = title, goal = goal, controls = controls, success = success, fail = fail, tip = tip };
    }

    private static Entry[] all;
    private static MenuRecipe[] menus;

    public static Entry[] All
    {
        get
        {
            if (all == null) all = Build();
            return all;
        }
    }

    public static MenuRecipe[] Menus
    {
        get
        {
            if (menus == null) menus = BuildMenus();
            return menus;
        }
    }

    // 없으면 null
    public static Entry Get(string stationId)
    {
        Entry[] list = All;
        for (int i = 0; i < list.Length; i++)
        {
            if (list[i].stationId == stationId) return list[i];
        }
        return null;
    }

    private static Entry[] Build()
    {
        return new Entry[]
        {
            E("grinder", "원두 넣기", "원두를 퍼서 그라인더에 던져 넣기",
                new Control[] { C("MOUSE_MOVE", "주걱 옮기기"), C("LMB_HOLD", "주머니서 담기"), C("KEY_Q", "던지기") },
                "그라인더 통에 원두 10알 넣기",
                "8초 안에 10알을 못 채움",
                "주걱에는 한 번에 8알까지 담겨요"),
            E("espresso", "샷 추출", "샷을 목표 양에서 멈추기",
                new Control[] { C("LMB_HOLD", "꾹 눌러 추출") },
                "25~40ml에서 떼기",
                "40ml를 넘겨 떼거나 60ml 넘침",
                "주문에 따라 구간이 달라져요. 중간에 한 번 치직하고 멈춰요"),
            E("ice", "얼음 담기", "컵에 얼음을 목표 개수만큼 담기",
                new Control[] { C("MOUSE_MOVE", "스쿱 옮기기"), C("LMB_HOLD", "제빙기서 담기"), C("RMB_HOLD", "기울여 쏟기") },
                "컵에 4~7개를 0.5초 유지",
                "10개를 넘기거나 시간 초과",
                "주문에 따라 구간이 달라져요. 스쿱에 담긴 채로는 세지 않아요"),
            E("milk", "우유 붓기", "우유를 목표 선까지 붓고 팩 세우기",
                new Control[] { C("MOUSE_MOVE", "팩 옮기기"), C("LMB_HOLD", "팩 잡기"), C("RMB_HOLD", "기울여 붓기") },
                "컵의 60~85%에서 팩 세우기",
                "85% 넘기거나 밖으로 60ml 흘림",
                "앞 단계의 커피가 이미 들어 있어요(갈색). 그 위로 구간이 정해져요"),
            E("handgrinder", "원두 갈기", "손잡이를 시계 방향으로 돌리기",
                new Control[] { C("MOUSE_CIRCLE", "시계 방향 돌리기") },
                "6바퀴를 채우기 (빠를수록 좋은 등급)",
                "7초 안에 6바퀴를 못 채움",
                "클릭은 필요 없어요. 반시계 방향과 축 근처 움직임은 세지 않아요"),
            E("dripper", "원두·물 넣기", "가루는 드리퍼에, 물은 주전자에 붓기",
                new Control[] { C("LMB_HOLD", "통 잡기"), C("RMB_HOLD", "기울여 붓기"), C("MOUSE_MOVE", "입구에 맞추기") },
                "가루 70~90%, 물 60~80%에서 0.3초 멈추기",
                "100%를 넘치거나 엉뚱한 곳에 0.3초 부음",
                "가루 통은 드리퍼에, 물병은 주전자에 부어요"),
            E("kettle", "물 끓이기", "나온 키를 눌러 물을 92도까지 데우기",
                new Control[] { C("KEY_QWER", "나온 키 누르기") },
                "맞는 키마다 +9도, 92도 도달",
                "틀리면 -5도에 0.25초 잠김, 6초 안에 못 끓임",
                "오른쪽에 다음 키 3개가 미리 보여요"),
            E("drip", "드립 붓기", "표시를 멈춰 추출 게이지 100 채우기",
                new Control[] { C("SPACE", "표시 멈추기") },
                "5번 안에 합계 100 이상 (초록 +25, 노랑 +20)",
                "5번을 다 쓰고도 100 미만",
                "멈출 때마다 8%씩 빨라져요. 가운데 초록이 가장 높아요"),
            E("autogrind", "원두 갈기", "바늘이 밝은 구간에 있을 때 누르기",
                new Control[] { C("SPACE", "구간에서 누르기") },
                "4번 맞추기 (가운데 좁은 구간은 완벽)",
                "3번 놓치거나 8초 시간 초과",
                "맞출 때마다 10%씩 빨라져요. 구간을 지나쳐도 놓친 거예요"),
            E("portafilter", "포타필터 담기", "포타필터에 가루를 알맞게 받기",
                new Control[] { C("MOUSE_MOVE", "좌우로 맞추기"), C("LMB_HOLD", "가루 받기") },
                "80~95%에서 떼고 0.4초 유지",
                "100%를 넘기거나 시간 초과",
                "토출구 밑에서만 담겨요. 뗀 뒤 조금 더 나와요"),
            E("tamper", "탬핑", "압력을 띠 안에 두고 수평 맞추기",
                new Control[] { C("LMB_HOLD", "꾹 눌러 압력"), C("MOUSE_MOVE", "좌우로 기울기 보정") },
                "압력 60~80%에 기울기 3도 이내로 1.4초 채우기",
                "압력이 100%를 넘기거나 7초 시간 초과",
                "떼면 압력이 빠져요. 조건이 끊겨도 누적은 남아요"),
            E("steam", "우유 스팀", "우유를 알맞은 온도와 거품으로 데우기",
                new Control[] { C("MOUSE_VERT", "위아래로 깊이"), C("LMB_HOLD", "분사"), C("LMB", "떼면 판정") },
                "55~70도에 거품 15~60%에서 떼기",
                "75도 도달, 표면 위 0.8초 분사 또는 범위 밖",
                "화면 위쪽은 거품, 아래쪽은 가열이에요. 한 번만 분사할 수 있어요"),
            E("latteart", "라떼 아트", "점을 순서대로 지나 하트 그리기",
                new Control[] { C("MOUSE_MOVE", "붓 옮기기"), C("LMB_HOLD", "누르며 붓기") },
                "10점 중 7점 이상 채우기",
                "8초 안에 7점을 못 채움 (붓는 양은 5.5초치)",
                "깜빡이는 점이 다음 차례예요. 한 점은 건너뛰어도 돼요"),
            E("water", "물 타기", "물을 목표 양만큼 붓고 밸브 닫기",
                new Control[] { C("WHEEL", "휠 올려 열기"), C("RMB_HOLD", "밸브 닫기") },
                "140~200ml에서 0.6초 멈춰 있기",
                "260ml를 넘기면 넘침, 시간 초과",
                "주문에 따라 구간이 달라져요. 열어도 저절로 서서히 닫혀요"),
        };
    }

    // Assets/_Project/Data/Menus/*.asset 의 menuName·steps 그대로
    private static MenuRecipe[] BuildMenus()
    {
        return new MenuRecipe[]
        {
            new MenuRecipe { menuName = "에스프레소", steps = new string[] { "grinder", "espresso" } },
            new MenuRecipe { menuName = "아이스아메리카노", steps = new string[] { "grinder", "espresso", "ice" } },
            new MenuRecipe { menuName = "라떼", steps = new string[] { "grinder", "espresso", "milk" } },
            new MenuRecipe { menuName = "아이스 라떼", steps = new string[] { "grinder", "espresso", "ice", "milk" } },
            new MenuRecipe { menuName = "핸드드립 커피", steps = new string[] { "grinder", "handgrinder", "dripper", "kettle", "drip" } },
            new MenuRecipe { menuName = "아메리카노", steps = new string[] { "grinder", "handgrinder", "dripper", "kettle", "drip", "water" } },
            new MenuRecipe { menuName = "기계 라떼", steps = new string[] { "grinder", "autogrind", "portafilter", "tamper", "espresso", "steam", "milk" } },
            new MenuRecipe { menuName = "라떼 아트", steps = new string[] { "grinder", "autogrind", "portafilter", "tamper", "espresso", "steam", "milk", "latteart" } },
        };
    }
}
