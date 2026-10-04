namespace BariBarista.Minigames
{
    /// <summary>
    /// 우유 붓기 판정. 액체 높이가 목표 선 안에 있을 때 붓기를 멈추면 성공.
    /// 100%를 넘기면 넘침, 컵 밖으로 부은 양이 한도를 넘으면 흘림 실패.
    /// </summary>
    public sealed class PourJudge
    {
        public float TargetMin01 { get; private set; }
        public float TargetMax01 { get; private set; }
        public float CapacityMl { get; private set; }
        public float MaxOutsideMl { get; private set; }
        /// <summary>시작 시 컵에 이미 있던 양(ml 환산).</summary>
        public float InitialMl { get; private set; }
        /// <summary>현재 컵 안의 총량(ml 환산, 용량을 넘지 않음).</summary>
        public float LiquidMl { get; private set; }
        /// <summary>이번 판에 컵 안으로 들어간 양.</summary>
        public float PouredInMl => LiquidMl - InitialMl;
        public float OverflowMl { get; private set; }
        public float OutsideMl { get; private set; }
        public float Fill01 => CapacityMl > 0f ? LiquidMl / CapacityMl : 0f;
        public JudgeOutcome Outcome { get; private set; }

        public void Reset(float targetMin01, float targetMax01, float capacityMl, float initialMl, float maxOutsideMl)
        {
            TargetMin01 = targetMin01;
            TargetMax01 = targetMax01;
            CapacityMl = capacityMl;
            MaxOutsideMl = maxOutsideMl;
            InitialMl = initialMl < 0f ? 0f : (initialMl > capacityMl ? capacityMl : initialMl);
            LiquidMl = InitialMl;
            OverflowMl = 0f;
            OutsideMl = 0f;
            Outcome = JudgeOutcome.Pending;
        }

        public JudgeOutcome AddToCup(float ml)
        {
            if (Outcome.IsDecided || ml <= 0f) return Outcome;
            LiquidMl += ml;
            if (LiquidMl > CapacityMl + JudgeMath.Epsilon)
            {
                OverflowMl += LiquidMl - CapacityMl;
                LiquidMl = CapacityMl;
                Outcome = JudgeOutcome.Lose(FailReason.Overflow);
            }
            return Outcome;
        }

        public JudgeOutcome AddOutside(float ml)
        {
            if (Outcome.IsDecided || ml <= 0f) return Outcome;
            OutsideMl += ml;
            if (OutsideMl > MaxOutsideMl + JudgeMath.Epsilon)
                Outcome = JudgeOutcome.Lose(FailReason.Spilled);
            return Outcome;
        }

        /// <summary>팩을 다시 세워 붓기를 멈췄을 때. 모자라거나 컵에 한 방울도 안 들어갔으면 계속 진행.</summary>
        public JudgeOutcome StopPouring()
        {
            if (Outcome.IsDecided || PouredInMl <= 0f) return Outcome;
            float fill = Fill01;
            if (JudgeMath.InRange(fill, TargetMin01, TargetMax01))
                Outcome = JudgeOutcome.Win(JudgeMath.RangeScore(fill, TargetMin01, TargetMax01));
            else if (fill > TargetMax01)
                Outcome = JudgeOutcome.Lose(FailReason.TooMuch);
            return Outcome;
        }

        public JudgeOutcome TimeUp()
        {
            if (Outcome.IsDecided) return Outcome;
            float fill = Fill01;
            if (PouredInMl <= 0f)
                Outcome = JudgeOutcome.Lose(FailReason.Timeout);
            else if (JudgeMath.InRange(fill, TargetMin01, TargetMax01))
                Outcome = JudgeOutcome.Win(JudgeMath.RangeScore(fill, TargetMin01, TargetMax01));
            else if (fill > TargetMax01)
                Outcome = JudgeOutcome.Lose(FailReason.TooMuch);
            else
                Outcome = JudgeOutcome.Lose(PouredInMl > 0f ? FailReason.TooLittle : FailReason.Timeout);
            return Outcome;
        }
    }
}
