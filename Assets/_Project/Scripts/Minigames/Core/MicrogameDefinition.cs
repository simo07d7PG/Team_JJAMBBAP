using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 미니게임 정의 데이터. 코어 루프는 이 에셋으로 지시어·제한 시간·스테이션을 알고,
    /// id로 미니게임 씬 안의 MicrogameBase를 찾는다.
    /// </summary>
    [CreateAssetMenu(fileName = "MicrogameDefinition", menuName = "BariBarista/Microgame Definition")]
    public class MicrogameDefinition : ScriptableObject
    {
        [Tooltip("MicrogameBase.Id와 같은 값")]
        public string id;

        [Tooltip("화면 중앙 지시어 (예: 샷 내려라!)")]
        public string instruction;

        [Tooltip("메인 씬에서 이 미니게임을 여는 기계의 stationId")]
        public string stationId;

        [Tooltip("기본 제한 시간(초). 코어 루프가 스피드 배율을 곱해 실제 시간을 정한다")]
        [Min(0.5f)] public float baseTimeLimit = 7f;

        [Tooltip("미니게임 프리팹 (루트에 MicrogameBase 상속 컴포넌트)")]
        public GameObject prefab;

        [Tooltip("버티기형: 시간이 다 될 때까지 실패하지 않으면 성공")]
        public bool successOnTimeout;

        [Tooltip("난이도 기본값 (1~3)")]
        [Range(1, 3)] public int defaultDifficulty = 1;
    }
}
