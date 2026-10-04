namespace BariBarista.Minigames
{
    /// <summary>미니게임 실패 이유.</summary>
    public enum FailReason
    {
        None = 0,
        Timeout,
        Overflow,
        TooLittle,
        TooMuch,
        Spilled,
        WrongOrder,
        /// <summary>ForceEnd로 강제 종료됨(게임오버·재시작).</summary>
        Aborted,
    }

    /// <summary>
    /// 미니게임 결과. 매 판 할당하지 않도록 구조체로 둔다.
    /// Amount/Wasted의 단위는 미니게임마다 다르다(ml 또는 개수).
    /// </summary>
    public struct MicrogameResult
    {
        public bool Success;
        public FailReason Reason;
        /// <summary>0~1 점수. 실패면 0.</summary>
        public float Score;
        /// <summary>시작부터 결과 확정까지 걸린 시간(초).</summary>
        public float Elapsed;
        /// <summary>주요 결과량. 예: 추출 ml, 얼음 개수, 부은 ml.</summary>
        public float Amount;
        /// <summary>버린 양. 예: 넘친 ml, 떨어뜨린 얼음 개수.</summary>
        public float Wasted;

        public static MicrogameResult Succeeded(float score)
        {
            return new MicrogameResult { Success = true, Reason = FailReason.None, Score = Clamp01(score) };
        }

        public static MicrogameResult Failed(FailReason reason)
        {
            return new MicrogameResult { Success = false, Reason = reason, Score = 0f };
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
