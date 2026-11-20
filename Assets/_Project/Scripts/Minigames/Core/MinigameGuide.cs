// 미니게임 설명 데이터. 각 *Microgame.cs의 실제 조작·판정 수치(프리팹 직렬화 값 포함)를 그대로 옮겼다.
// 수치를 바꾸면 여기 문구도 함께 고친다. 게임을 처음 하는 사람이 읽는다고 생각하고 일상어로 쓴다.
// 용어: 꾹 누르기(버튼을 누르고 있기), 떼기(버튼에서 손 떼기), 게이지(차오르는 막대), 띠(게이지 위의 목표 칸)
public static class MinigameGuide
{
    // icon 토큰: LMB, LMB_HOLD, RMB, RMB_HOLD, WHEEL, MOUSE_MOVE, MOUSE_CIRCLE, MOUSE_VERT, SPACE, KEY_Q, KEY_W, KEY_E, KEY_R, KEY_QWER
    public class Control
    {
        public string icon;
        public string caption;
    }

    public class Entry
    {
        public string stationId;
        public string title;
        // 무엇을 만드는지 한 문장
        public string goal;
        // 따라 하는 순서 2~4줄 ("1. ..." 형식). null이면 카드가 조작 아이콘만 보여 준다
        public string[] steps;
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

    private static Entry E(string stationId, string title, string goal, string[] steps, Control[] controls, string success, string fail, string tip)
    {
        return new Entry { stationId = stationId, title = title, goal = goal, steps = steps, controls = controls, success = success, fail = fail, tip = tip };
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
            E("grinder", "원두 넣기", "원두 10알을 그라인더 통에 던져 넣어요",
                new string[]
                {
                    "1. 마우스로 주걱을 원두 주머니 위로 옮겨요",
                    "2. 좌클릭을 꾹 눌러 주걱에 원두를 담아요",
                    "3. 그라인더 입구 위에서 Q를 눌러 던져요",
                },
                new Control[] { C("MOUSE_MOVE", "주걱 옮기기"), C("LMB_HOLD", "주머니서 담기"), C("KEY_Q", "던지기") },
                "통에 원두가 10알 들어가면 성공",
                "8초 안에 10알을 못 채우면 실패",
                "주걱엔 8알까지만 담겨요. 던지고 다시 담아요"),
            E("espresso", "샷 추출", "에스프레소 샷을 알맞은 양만큼 내려요",
                new string[]
                {
                    "1. 좌클릭을 꾹 눌러 샷을 내려요",
                    "2. 오른쪽 게이지가 위로 차오르는지 봐요",
                    "3. 게이지가 '여기서 떼!' 띠에 닿으면 떼요",
                },
                new Control[] { C("LMB_HOLD", "꾹 눌러 추출") },
                "띠 안에서 손을 떼면 성공 (25~40ml)",
                "띠를 넘겨 떼거나 60ml 넘치면 실패",
                "중간에 치직 하고 멈춰도 계속 꾹 눌러요"),
            E("ice", "얼음 담기", "컵에 얼음을 알맞은 개수만큼 담아요",
                new string[]
                {
                    "1. 마우스로 스쿱을 제빙기 위로 옮겨요",
                    "2. 좌클릭을 꾹 눌러 스쿱에 얼음을 담아요",
                    "3. 스쿱을 컵 위로 옮겨 우클릭을 꾹 눌러요",
                    "4. 아래 칸에서 테두리 친 칸까지 채워요",
                },
                new Control[] { C("MOUSE_MOVE", "스쿱 옮기기"), C("LMB_HOLD", "제빙기서 담기"), C("RMB_HOLD", "기울여 쏟기") },
                "컵에 4~7개를 0.5초 동안 유지하면 성공",
                "10개를 넘기거나 6초가 지나면 실패",
                "주문에 따라 목표 개수가 달라져요. 스쿱 속은 안 세요"),
            E("milk", "우유 붓기", "컵에 우유를 알맞은 높이까지 부어요",
                new string[]
                {
                    "1. 좌클릭을 꾹 눌러 우유팩을 잡고 컵 위로 가요",
                    "2. 우클릭을 꾹 눌러 팩을 기울여 따라요",
                    "3. 왼쪽 컵 그림의 우유가 목표 띠에 닿으면",
                    "4. 우클릭을 떼서 팩을 다시 세워요",
                },
                new Control[] { C("MOUSE_MOVE", "팩 옮기기"), C("LMB_HOLD", "팩 잡기"), C("RMB_HOLD", "기울여 붓기") },
                "목표 띠 안에서 팩을 세우면 성공 (60~85%)",
                "컵이 넘치거나 밖으로 많이 흘리면 실패",
                "갈색은 앞 단계 커피예요. 그 위로 채워요"),
            E("handgrinder", "원두 갈기", "손잡이를 돌려 원두를 갈아요",
                new string[]
                {
                    "1. 마우스를 손잡이 가운데 축 둘레에 둬요",
                    "2. 클릭 없이 시계 방향으로 빙글빙글 돌려요",
                    "3. 화면에 세는 바퀴 수가 6이 되면 끝나요",
                },
                new Control[] { C("MOUSE_CIRCLE", "시계 방향 돌리기") },
                "손잡이를 6바퀴 돌리면 성공 (빠를수록 좋아요)",
                "7초 안에 6바퀴를 못 돌리면 실패",
                "반시계 방향이나 축 바로 옆 움직임은 안 세요"),
            E("dripper", "원두·물 넣기", "가루를 드리퍼에, 물을 주전자에 넣어요",
                new string[]
                {
                    "1. 가루 통을 좌클릭 꾹으로 잡고 드리퍼로 가요",
                    "2. 우클릭을 꾹 눌러 기울여 가루를 부어요",
                    "3. 가루 게이지가 목표 띠에 차면 떼요",
                    "4. 그다음 같은 방법으로 물병을 주전자에 부어요",
                },
                new Control[] { C("LMB_HOLD", "통 잡기"), C("RMB_HOLD", "기울여 붓기"), C("MOUSE_MOVE", "같은 색 목표에 맞추기") },
                "가루 70~90%, 물 60~80%에서 멈추면 성공",
                "100%를 넘치거나 엉뚱한 곳에 1.5초 부으면 실패",
                "가루가 끝나야 물병을 잡을 수 있어요. 순서대로요"),
            E("kettle", "물 끓이기", "나온 키를 눌러 물을 팔팔 끓여요",
                new string[]
                {
                    "1. 화면 가운데 큰 글자(Q W E R)를 봐요",
                    "2. 키보드에서 같은 키를 눌러요",
                    "3. 맞으면 온도가 올라가고 다음 키가 나와요",
                    "4. 틀리면 온도가 내려가고 잠깐 못 눌러요",
                },
                new Control[] { C("KEY_QWER", "나온 키 누르기") },
                "온도계가 92도까지 오르면 성공 (맞을 때 +9도)",
                "6초 안에 92도에 못 닿으면 실패",
                "오른쪽에 다음 키 3개가 미리 보여요"),
            E("drip", "드립 붓기", "타이밍을 맞춰 커피를 내려요",
                new string[]
                {
                    "1. 색 띠 위를 왔다 갔다 하는 선을 봐요",
                    "2. 선이 초록 칸에 있을 때 Space를 눌러요",
                    "3. 멈춘 색만큼 추출 게이지가 차요",
                    "4. 5번 안에 게이지를 100까지 채워요",
                },
                new Control[] { C("SPACE", "표시 멈추기") },
                "5번 안에 게이지 100 채우면 성공 (초록 +25)",
                "5번을 다 쓰고도 100이 안 되면 실패",
                "누를 때마다 선이 8%씩 빨라져요. 초록이 최고예요"),
            E("autogrind", "원두 갈기", "타이밍을 맞춰 원두를 갈아요",
                new string[]
                {
                    "1. 링 위를 시계 방향으로 도는 바늘을 봐요",
                    "2. 링에 밝게 칠해진 구간이 있어요",
                    "3. 바늘이 그 구간에 있을 때 Space를 눌러요",
                    "4. 구간 가운데 좁은 칸을 맞추면 완벽이에요",
                },
                new Control[] { C("SPACE", "구간에서 누르기") },
                "구간에서 4번 맞추면 성공",
                "3번 놓치거나 8초가 지나면 실패",
                "맞출 때마다 10%씩 빨라져요. 지나친 것도 놓친 거예요"),
            E("portafilter", "포타필터 담기", "간 원두 가루를 포타필터에 받아요",
                new string[]
                {
                    "1. 마우스를 좌우로 움직여 가루 줄기 밑에 맞춰요",
                    "2. 줄기 밑에서 좌클릭을 꾹 눌러 가루를 받아요",
                    "3. 오른쪽 게이지가 초록 띠에 닿으면 떼요",
                    "4. 뗀 채 0.4초 기다리면 끝나요",
                },
                new Control[] { C("MOUSE_MOVE", "좌우로 맞추기"), C("LMB_HOLD", "가루 받기") },
                "초록 띠에서 떼고 0.4초 기다리면 성공 (80~95%)",
                "100%를 넘기거나 8초가 지나면 실패",
                "줄기 밖에선 가루를 흘려요. 뗀 뒤에도 조금 더 나와요"),
            E("tamper", "탬핑", "포타필터의 가루를 꾹꾹 다져요",
                new string[]
                {
                    "1. 좌클릭을 꾹 눌러 압력 게이지를 올려요",
                    "2. 압력이 목표 띠(60~80%)에 들게 눌러요",
                    "3. 마우스를 좌우로 움직여 탬퍼를 수평으로 해요",
                    "4. 띠 안이고 수평인 시간이 1.4초 쌓이면 끝나요",
                },
                new Control[] { C("LMB_HOLD", "꾹 눌러 압력"), C("MOUSE_MOVE", "좌우로 기울기 보정") },
                "압력 띠 안에서 수평으로 1.4초 채우면 성공",
                "압력이 100%를 넘기거나 7초가 지나면 실패",
                "떼면 압력이 빠져요. 중간에 끊겨도 쌓인 시간은 남아요"),
            E("steam", "우유 스팀", "우유를 따뜻하고 부드럽게 데워요",
                new string[]
                {
                    "1. 마우스를 아래로 내려 노즐을 우유에 넣어요",
                    "2. 얕게 두고 좌클릭을 꾹 눌러 거품을 내요",
                    "3. 거품이 목표 띠에 닿으면 더 깊이 내려가요",
                    "4. 왼쪽 온도가 목표 띠일 때 손을 떼요",
                },
                new Control[] { C("MOUSE_VERT", "위아래로 깊이"), C("LMB_HOLD", "분사"), C("LMB", "떼면 판정") },
                "온도 55~70도, 거품 15~60%에서 떼면 성공",
                "75도가 되거나 띠 밖에서 떼면 실패",
                "우유 위 허공에 쏘면 튀어요. 떼는 건 딱 한 번이에요"),
            E("latteart", "라떼 아트", "우유 거품으로 컵 위에 하트를 그려요",
                new string[]
                {
                    "1. 컵 위에서 깜빡이는 점으로 붓을 옮겨요",
                    "2. 좌클릭을 꾹 눌러 우유를 부으며 움직여요",
                    "3. 깜빡이는 다음 점을 차례로 지나가요",
                    "4. 점 10개 중 7개 이상 이으면 하트예요",
                },
                new Control[] { C("MOUSE_MOVE", "붓 옮기기"), C("LMB_HOLD", "누르며 붓기") },
                "점을 7개 이상 차례로 지나면 성공",
                "8초 안에 7개를 못 이으면 실패",
                "누르는 시간은 합쳐서 5.5초뿐이에요. 아껴 써요"),
            E("water", "물 타기", "컵에 뜨거운 물을 알맞게 부어요",
                new string[]
                {
                    "1. 마우스 휠을 위로 굴려 밸브를 열어요",
                    "2. 물이 나오면 게이지가 올라가는지 봐요",
                    "3. 목표 띠에 닿으면 우클릭을 꾹 눌러 닫아요",
                    "4. 물이 멈춘 채 0.6초 기다려요",
                },
                new Control[] { C("WHEEL", "휠 올려 열기"), C("RMB_HOLD", "밸브 닫기") },
                "목표 띠에서 0.6초 멈춰 있으면 성공 (140~200ml)",
                "넘치거나 7초가 지나면 실패",
                "주문에 따라 목표가 달라져요. 밸브는 저절로 닫혀요"),
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
