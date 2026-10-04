namespace BariBarista.Minigames
{
    /// <summary>결과를 처음 한 번만 확정하는 래치. 이후 확정 시도는 무시한다.</summary>
    public sealed class ResultLatch
    {
        public bool IsSet { get; private set; }
        public MicrogameResult Result { get; private set; }

        public void Reset()
        {
            IsSet = false;
            Result = default;
        }

        /// <returns>이번 호출로 확정됐으면 true, 이미 확정돼 있었으면 false.</returns>
        public bool TrySet(MicrogameResult result)
        {
            if (IsSet) return false;
            IsSet = true;
            Result = result;
            return true;
        }
    }
}
