namespace BariBarista.Minigames
{
    /// <summary>
    /// 얼음 판정. 컵 안 얼음 개수가 목표 구간에 일정 시간 머물면 성공, 최대치를 넘으면 넘침 실패.
    /// </summary>
    public sealed class IceJudge
    {
        public int TargetMin { get; private set; }
        public int TargetMax { get; private set; }
        public int CupMax { get; private set; }
        public float HoldSeconds { get; private set; }
        public int Count { get; private set; }
        /// <summary>목표 구간에 연속으로 머문 시간.</summary>
        public float HoldTimer { get; private set; }
        public float Hold01 => HoldSeconds > 0f ? (HoldTimer >= HoldSeconds ? 1f : HoldTimer / HoldSeconds) : 1f;
        public JudgeOutcome Outcome { get; private set; }

        public void Reset(int targetMin, int targetMax, int cupMax, float holdSeconds)
        {
            TargetMin = targetMin;
            TargetMax = targetMax;
            CupMax = cupMax;
            HoldSeconds = holdSeconds;
            Count = 0;
            HoldTimer = 0f;
            Outcome = JudgeOutcome.Pending;
        }

        /// <summary>매 틱 컵 안 얼음 개수를 넣어 준다.</summary>
        public JudgeOutcome Update(int countInCup, float deltaTime)
        {
            if (Outcome.IsDecided) return Outcome;
            Count = countInCup;

            if (countInCup > CupMax)
            {
                Outcome = JudgeOutcome.Lose(FailReason.Overflow);
                return Outcome;
            }

            if (countInCup >= TargetMin && countInCup <= TargetMax)
            {
                if (deltaTime > 0f) HoldTimer += deltaTime;
                if (HoldTimer >= HoldSeconds - JudgeMath.Epsilon)
                    Outcome = JudgeOutcome.Win(JudgeMath.RangeScore(countInCup, TargetMin, TargetMax));
            }
            else
            {
                HoldTimer = 0f;
            }
            return Outcome;
        }

        /// <summary>시간 초과. 유지 시간을 못 채웠으면 구간 안이어도 실패(Timeout).</summary>
        public JudgeOutcome TimeUp()
        {
            if (Outcome.IsDecided) return Outcome;
            if (Count >= TargetMin && Count <= TargetMax)
                Outcome = JudgeOutcome.Lose(FailReason.Timeout);
            else if (Count > TargetMax)
                Outcome = JudgeOutcome.Lose(FailReason.TooMuch);
            else
                Outcome = JudgeOutcome.Lose(Count > 0 ? FailReason.TooLittle : FailReason.Timeout);
            return Outcome;
        }
    }
}
