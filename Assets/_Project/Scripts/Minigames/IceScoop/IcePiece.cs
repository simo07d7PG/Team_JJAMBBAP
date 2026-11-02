using UnityEngine;

// 얼음 조각 하나. 풀에서 꺼내 쓰고 반납한다.
// 콜라이더는 처음부터 최종 크기이고, 보이는 모델(visual)만 작게 시작해 커진다.
[RequireComponent(typeof(Rigidbody))]
public class IcePiece : MonoBehaviour
{
    [Tooltip("커지는 보이는 모델. 비우면 커지기 없이 바로 최종 크기")]
    [SerializeField] private Transform visual;
    [SerializeField] private float growSeconds = 0.1f;
    [Range(0f, 1f)]
    [SerializeField] private float growStartFraction = 0.3f;

    [HideInInspector] public Rigidbody body;

    private Vector3 baseScale = Vector3.one;
    private Vector3 visualFinalScale = Vector3.one;
    private float growElapsed;
    private bool growing;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        // 풀에서 처음 만들어진 시점의 크기를 저장해 두고, 재사용할 때마다 이 값을 기준으로 삼는다
        baseScale = transform.localScale;
        if (visual != null) visualFinalScale = visual.localScale;
    }

    // sizeFactor: 기본 크기에 곱할 배율(콜라이더 포함 최종 크기)
    public void ResetPiece(Vector3 position, Quaternion rotation, float sizeFactor)
    {
        transform.localScale = baseScale * sizeFactor;
        transform.SetPositionAndRotation(position, rotation);
        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        growElapsed = 0f;
        growing = CanGrow();
        ApplyGrow();
    }

    // 보이는 모델이 있고 커지는 시간이 있을 때만 커진다 (visual을 비우면 커지기 없음)
    private bool CanGrow()
    {
        return visual != null && growSeconds > 0f;
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
        float t = growing ? Mathf.Clamp01(growElapsed / growSeconds) : 1f;
        visual.localScale = visualFinalScale * Mathf.Lerp(growStartFraction, 1f, t);
    }
}
