namespace BariBarista.Minigames
{
    /// <summary>
    /// 에스프레소 샷 판정. 목표 구간에서 버튼을 떼면 성공, 컵 용량을 넘기면 넘침 실패.
    /// </summary>
    public sealed class ShotJudge
    {
        public float TargetMin { get; private set; }
        public float TargetMax { get; private set; }
        /// <summary>이번 판에 컵이 받을 수 있는 최대 ml.</summary>
        public float Capacity { get; private set; }
        /// <summary>컵 안에 들어간 ml(용량을 넘지 않음).</summary>
        public float Extracted { get; private set; }
        /// <summary>컵에서 넘친 ml.</summary>
        public float Spilled { get; private set; }
        public JudgeOutcome Outcome { get; private set; }

        public void Reset(float targetMin, float targetMax, float capacity)
        {
            TargetMin = targetMin;
            TargetMax = targetMax;
            Capacity = capacity;
            Extracted = 0f;
            Spilled = 0f;
            Outcome = JudgeOutcome.Pending;
        }

        /// <summary>컵에 에스프레소를 붓는다. 용량을 넘으면 넘친 만큼 Spilled에 쌓이고 실패.</summary>
        public JudgeOutcome AddToCup(float ml)
        {
            if (Outcome.IsDecided || ml <= 0f) return Outcome;
            Extracted += ml;
            if (Extracted > Capacity + JudgeMath.Epsilon)
            {
                Spilled += Extracted - Capacity;
                Extracted = Capacity;
                Outcome = JudgeOutcome.Lose(FailReason.Overflow);
            }
            return Outcome;
        }

        /// <summary>추출 버튼을 뗐을 때. 구간 안이면 성공, 넘었으면 실패, 모자라면 계속 진행.</summary>
        public JudgeOutcome Release()
        {
            if (Outcome.IsDecided) return Outcome;
            if (JudgeMath.InRange(Extracted, TargetMin, TargetMax))
                Outcome = JudgeOutcome.Win(JudgeMath.RangeScore(Extracted, TargetMin, TargetMax));
            else if (Extracted > TargetMax)
                Outcome = JudgeOutcome.Lose(FailReason.TooMuch);
            return Outcome;
        }

        /// <summary>시간 초과. 구간 안이면 성공으로 인정, 아니면 실패.</summary>
        public JudgeOutcome TimeUp()
        {
            if (Outcome.IsDecided) return Outcome;
            if (JudgeMath.InRange(Extracted, TargetMin, TargetMax))
                Outcome = JudgeOutcome.Win(JudgeMath.RangeScore(Extracted, TargetMin, TargetMax));
            else if (Extracted > TargetMax)
                Outcome = JudgeOutcome.Lose(FailReason.TooMuch);
            else
                Outcome = JudgeOutcome.Lose(Extracted > 0f ? FailReason.TooLittle : FailReason.Timeout);
            return Outcome;
        }
    }
}
