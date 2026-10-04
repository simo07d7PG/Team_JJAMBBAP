namespace BariBarista.Minigames
{
    /// <summary>결과 연출 시간 규칙. 미니게임이 따로 정하지 않으면 이 값을 쓴다.</summary>
    public static class ResultPresentation
    {
        public const float FailSeconds = 0.9f;
        public const float SuccessSeconds = 1.4f;

        /// <summary>결과에 맞는 연출 시간(초). 강제 종료(Aborted)는 연출이 없다.</summary>
        public static float DurationFor(in MicrogameResult result)
        {
            if (result.Success) return SuccessSeconds;
            return result.Reason == FailReason.Aborted ? 0f : FailSeconds;
        }
    }
}
