namespace BariBarista.Minigames
{
    /// <summary>
    /// 제한 시간 계산기. 시간은 밖에서 Advance로 넣어 준다(일시정지·스피드 업은 호출하는 쪽이 관리).
    /// </summary>
    public sealed class MicrogameClock
    {
        public float TimeLimit { get; private set; }
        public float Remaining { get; private set; }
        public float Elapsed => TimeLimit - Remaining;
        public bool IsTimeUp { get; private set; }

        /// <summary>남은 시간 비율(1 → 0).</summary>
        public float Remaining01 => TimeLimit > 0f ? Remaining / TimeLimit : 0f;

        public void Start(float timeLimit)
        {
            TimeLimit = timeLimit > 0f ? timeLimit : 0.01f;
            Remaining = TimeLimit;
            IsTimeUp = false;
        }

        /// <returns>이번 호출에서 처음으로 시간이 다 됐으면 true. 이후 호출은 false.</returns>
        public bool Advance(float deltaTime)
        {
            if (IsTimeUp || deltaTime <= 0f) return false;
            Remaining -= deltaTime;
            if (Remaining > 0f) return false;
            Remaining = 0f;
            IsTimeUp = true;
            return true;
        }
    }
}
