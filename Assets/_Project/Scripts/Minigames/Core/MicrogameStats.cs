using System;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 대참사 통계 보고 창구. 나중에 코어 루프의 이벤트 버스에 Reported를 연결한다.
    /// 키는 StatKeys 상수를 써서 문자열 할당을 피한다.
    /// </summary>
    public static class MicrogameStats
    {
        public static event Action<string, float> Reported;

        public static void Report(string key, float value)
        {
            if (value == 0f) return;
            Reported?.Invoke(key, value);
        }

        // 도메인 리로드를 끈 에디터에서도 이전 플레이의 구독자가 남지 않게
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Reported = null;
        }
    }

    /// <summary>통계 키 모음.</summary>
    public static class StatKeys
    {
        public const string EspressoSpilledMl = "espresso_spilled_ml";
        public const string EspressoFloorMl = "espresso_floor_ml";
        public const string IceDropped = "ice_dropped";
        public const string IceOverflow = "ice_overflow";
        public const string MilkSpilledMl = "milk_spilled_ml";
        public const string MilkFloorMl = "milk_floor_ml";
    }
}
