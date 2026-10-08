namespace BariBarista.Minigames
{
    /// <summary>
    /// 큰 조작 안내를 언제까지 보여 줄지 센다. Prepare부터 Playing 첫 1.5초 또는 첫 입력까지 크게, 이후는 구석 힌트.
    /// </summary>
    public sealed class GuideTimer
    {
        public const float BigSeconds = 1.5f;

        private float elapsed;
        private bool inputSeen;

        public void Reset()
        {
            elapsed = 0f;
            inputSeen = false;
        }

        public void MarkInput() => inputSeen = true;

        /// <summary>Playing 중 매 Tick 부른다. 큰 안내를 계속 보여야 하면 true.</summary>
        public bool Tick(float dt)
        {
            elapsed += dt;
            return !inputSeen && elapsed < BigSeconds;
        }
    }
}
