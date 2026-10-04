using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>미니게임을 시작할 때 호출하는 쪽이 넘겨 주는 값.</summary>
    public sealed class MicrogameContext
    {
        public MicrogameDefinition Definition { get; }
        /// <summary>실제 제한 시간(초). 스피드 배율이 이미 반영된 값.</summary>
        public float TimeLimit { get; }
        /// <summary>러시 스피드 단계(0부터).</summary>
        public int SpeedTier { get; }
        /// <summary>난이도 1~3.</summary>
        public int Difficulty { get; }
        /// <summary>한 주문 동안 미니게임 사이를 이어 가는 컵. 미니게임은 여기에 결과를 직접 반영한다.</summary>
        public CupContents Cup { get; }
        /// <summary>(선택) 메인 씬 스테이션 Transform. 코어 루프가 넘겨 줄 수 있다.</summary>
        public Transform Station { get; }

        public MicrogameContext(MicrogameDefinition definition, float timeLimit, int speedTier, int difficulty, CupContents cup, Transform station = null)
        {
            Definition = definition;
            TimeLimit = timeLimit;
            SpeedTier = speedTier;
            Difficulty = Mathf.Clamp(difficulty, 1, 3);
            Cup = cup ?? new CupContents();
            Station = station;
        }
    }
}
