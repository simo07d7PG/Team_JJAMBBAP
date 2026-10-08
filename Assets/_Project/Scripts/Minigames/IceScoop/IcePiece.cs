using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>얼음 크기·커지기 계산 (순수 함수).</summary>
    public static class IceSizing
    {
        /// <summary>기본 크기에 곱할 배율. t01=0이면 1-variation, 1이면 1+variation.</summary>
        public static float SizeFactor(float t01, float variation)
        {
            return 1f + (Mathf.Clamp01(t01) * 2f - 1f) * Mathf.Max(0f, variation);
        }

        /// <summary>커지는 중인 보이는 크기 비율(0~1). duration이 0 이하면 바로 1.</summary>
        public static float GrowScale(float elapsed, float duration, float startFraction)
        {
            if (duration <= 0f) return 1f;
            float t = Mathf.Clamp01(elapsed / duration);
            return Mathf.Lerp(Mathf.Clamp01(startFraction), 1f, t);
        }
    }

    /// <summary>
    /// 얼음 조각 하나. 풀에서 꺼내 쓰고 반납한다.
    /// 콜라이더는 처음부터 최종 크기이고, 보이는 모델(visual)만 작게 시작해 커진다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class IcePiece : MonoBehaviour
    {
        [Tooltip("커지는 보이는 모델. 비우면 커지기 없이 바로 최종 크기")]
        [SerializeField] private Transform visual;
        [SerializeField] private float growSeconds = 0.1f;
        [Range(0f, 1f)]
        [SerializeField] private float growStartFraction = 0.3f;

        public Rigidbody Body { get; private set; }
        /// <summary>바닥에 떨어진 것으로 이미 보고했는지.</summary>
        public bool Dropped { get; set; }

        private Vector3 baseScale = Vector3.one;
        private Vector3 visualFinalScale = Vector3.one;
        private float growElapsed;
        private bool growing;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            baseScale = transform.localScale;
            if (visual != null) visualFinalScale = visual.localScale;
        }

        /// <param name="sizeFactor">기본 크기에 곱할 배율(콜라이더 포함 최종 크기)</param>
        public void ResetPiece(Vector3 position, Quaternion rotation, float sizeFactor = 1f)
        {
            if (Body == null) Body = GetComponent<Rigidbody>();
            Dropped = false;
            transform.localScale = baseScale * sizeFactor;
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;

            growElapsed = 0f;
            growing = visual != null && growSeconds > 0f;
            ApplyGrow();
        }

        private void Update()
        {
            if (!growing) return;
            growElapsed += Time.deltaTime;
            if (growElapsed >= growSeconds) growing = false;
            ApplyGrow();
        }

        private void ApplyGrow()
        {
            if (visual == null) return;
            visual.localScale = visualFinalScale * IceSizing.GrowScale(growing ? growElapsed : growSeconds, growSeconds, growStartFraction);
        }
    }
}
