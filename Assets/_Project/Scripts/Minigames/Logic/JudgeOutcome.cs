namespace BariBarista.Minigames
{
    /// <summary>판정 클래스가 내놓는 현재 판정 상태.</summary>
    public struct JudgeOutcome
    {
        public bool IsDecided;
        public bool Success;
        public FailReason Reason;
        public float Score;

        public static readonly JudgeOutcome Pending = default;

        public static JudgeOutcome Win(float score)
        {
            return new JudgeOutcome { IsDecided = true, Success = true, Reason = FailReason.None, Score = score };
        }

        public static JudgeOutcome Lose(FailReason reason)
        {
            return new JudgeOutcome { IsDecided = true, Success = false, Reason = reason, Score = 0f };
        }
    }

    internal static class JudgeMath
    {
        /// <summary>부동소수 경계값을 성공으로 인정하기 위한 여유.</summary>
        public const float Epsilon = 1e-4f;

        public static bool InRange(float value, float min, float max)
        {
            return value >= min - Epsilon && value <= max + Epsilon;
        }

        /// <summary>구간 중앙이면 1, 경계면 0.5. 폭이 0이면(정확히 맞춰야 하는 경우) 1.</summary>
        public static float RangeScore(float value, float min, float max)
        {
            float half = (max - min) * 0.5f;
            if (half <= Epsilon) return 1f;
            float center = (min + max) * 0.5f;
            float t = System.Math.Abs(value - center) / half;
            if (t > 1f) t = 1f;
            return 1f - 0.5f * t;
        }
    }
}
