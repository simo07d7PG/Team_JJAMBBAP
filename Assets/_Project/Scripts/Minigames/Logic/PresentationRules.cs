namespace BariBarista.Minigames
{
    /// <summary>
    /// 미니게임 화면에 나오는 한글 문구와 실패 이유 문구. 전부 const라 할당이 없다.
    /// 새 문구를 추가하면 PreloadCharacters에도 넣어야 동적 폰트가 □ 없이 그린다(PresentationRulesTests가 확인한다).
    /// </summary>
    public static class PresentationRules
    {
        // 정의(MicrogameDefinition.instruction)와 같은 문구
        public const string InstructionShot = "샷 내려라!";
        public const string InstructionIce = "얼음 퍼라!";
        public const string InstructionPour = "우유 부어라!";

        public const string TimeoutText = "시간 초과!";
        public const string WrongOrderText = "순서가 틀렸어!";
        public const string SuccessPerfect = "완벽해!";
        public const string SuccessGood = "좋아!";

        /// <summary>점수가 이 값 이상이면 "완벽해!".</summary>
        public const float PerfectScore = 0.9f;

        /// <summary>숫자 서식(SetText용). 값이 바뀔 때만 부른다.</summary>
        public const string MlFormat = "{0} ml";
        public const string PercentFormat = "{0}%";
        public const string CountFormat = "{0}개";

        public const string Target = "목표";

        public static string SuccessText(float score) => score >= PerfectScore ? SuccessPerfect : SuccessGood;

        /// <summary>결과에 맞는 문구. 성공이면 칭찬, 실패면 호출하는 쪽이 고른 게임별 이유.</summary>
        public static string ResultText(in MicrogameResult result, string failText)
            => result.Success ? SuccessText(result.Score) : failText;

        /// <summary>동적 폰트에 미리 넣을 글자. 모든 문구 + 숫자 + 단위 + 문장부호.</summary>
        public const string PreloadCharacters =
            InstructionShot + InstructionIce + InstructionPour +
            TimeoutText + WrongOrderText + SuccessPerfect + SuccessGood +
            MlFormat + PercentFormat + CountFormat + Target +
            ShotRules.All + IceRules.All + PourRules.All +
            "0123456789ml%/.,:!?+-~()· ";
    }

    /// <summary>에스프레소 샷 문구.</summary>
    public static class ShotRules
    {
        public const string Title = "샷 잔";
        public const string Release = "여기서 떼!";
        public const string GuideHold = "누르고 있으면 샷 추출";
        public const string GuideRelease = "목표 구간에서 떼기";
        public const string GuideCup = "먼저 컵을 받침으로 끌기";
        public const string Hint = "누르고 있기 · 목표에서 떼기";
        public const string HintCup = "먼저 컵을 받침으로 끌기";
        public const string FailOverflow = "컵이 넘쳤어!";
        public const string FailTooMuch = "너무 많이 뽑았어!";
        public const string FailTooLittle = "샷이 모자라!";

        public const string All = Title + Release + GuideHold + GuideRelease + GuideCup + Hint + HintCup +
            FailOverflow + FailTooMuch + FailTooLittle;

        public static string FailText(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.Overflow: return FailOverflow;
                case FailReason.TooMuch: return FailTooMuch;
                case FailReason.TooLittle: return FailTooLittle;
                case FailReason.Timeout: return PresentationRules.TimeoutText;
                case FailReason.WrongOrder: return PresentationRules.WrongOrderText;
                default: return string.Empty;
            }
        }
    }

    /// <summary>얼음 퍼기 문구.</summary>
    public static class IceRules
    {
        public const string Title = "얼음";
        public const string Hold = "유지";
        public const string GuideScoop = "제빙기 위에서 좌클릭 누르기: 얼음 담기";
        public const string GuideMove = "마우스 움직이기: 스쿱 옮기기";
        public const string GuideTilt = "우클릭 누르기: 기울여서 컵에 붓기";
        public const string Hint = "좌클릭 담기 · 우클릭 기울이기";
        public const string FailOverflow = "얼음이 넘쳤어!";
        public const string FailTooMuch = "얼음이 너무 많아!";
        public const string FailTooLittle = "얼음이 모자라!";

        public const string All = Title + Hold + GuideScoop + GuideMove + GuideTilt + Hint +
            FailOverflow + FailTooMuch + FailTooLittle;

        public static string FailText(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.Overflow: return FailOverflow;
                case FailReason.TooMuch: return FailTooMuch;
                case FailReason.TooLittle: return FailTooLittle;
                case FailReason.Timeout: return PresentationRules.TimeoutText;
                case FailReason.WrongOrder: return PresentationRules.WrongOrderText;
                default: return string.Empty;
            }
        }
    }

    /// <summary>우유 붓기 문구.</summary>
    public static class PourRules
    {
        public const string Title = "우유";
        public const string Spill = "흘림";
        public const string GuideGrab = "좌클릭 누르기: 우유팩 잡기";
        public const string GuideTilt = "우클릭 누르기: 기울여서 붓기";
        public const string GuideStop = "목표 선에서 다시 세우기";
        public const string Hint = "좌클릭 잡기 · 우클릭 붓기 · 목표에서 세우기";
        public const string FailOverflow = "우유가 넘쳤어!";
        public const string FailSpilled = "우유를 흘렸어!";
        public const string FailTooMuch = "너무 많이 부었어!";
        public const string FailTooLittle = "너무 적게 부었어!";

        public const string All = Title + Spill + GuideGrab + GuideTilt + GuideStop + Hint +
            FailOverflow + FailSpilled + FailTooMuch + FailTooLittle;

        public static string FailText(FailReason reason)
        {
            switch (reason)
            {
                case FailReason.Overflow: return FailOverflow;
                case FailReason.Spilled: return FailSpilled;
                case FailReason.TooMuch: return FailTooMuch;
                case FailReason.TooLittle: return FailTooLittle;
                case FailReason.Timeout: return PresentationRules.TimeoutText;
                case FailReason.WrongOrder: return PresentationRules.WrongOrderText;
                default: return string.Empty;
            }
        }
    }
}
