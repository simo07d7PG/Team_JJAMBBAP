using UnityEngine;
using UnityEngine.InputSystem;

// 모든 미니게임의 부모. 러너(나중에 코어 루프)가 StartGame()으로 시작하고, 제한 시간이 끝나면 TimeOver()를 부른다.
// 하위 클래스는 Update()에서 if (!isPlaying) return; 뒤에 조작·판정을 하고 Finish(...)를 부른다.
public class Minigame : MonoBehaviour
{
    [Header("미니게임 정보")]
    [Tooltip("기계 id (espresso / ice / milk 등)")]
    public string stationId;
    [Tooltip("시작 전에 크게 보여 줄 지시어")]
    public string instruction;
    [Tooltip("제한 시간(초). 러너가 센다")]
    public float timeLimit = 7f;
    public Camera viewCamera;

    [Header("입력 (InputSystem_Actions > UI)")]
    public InputActionReference pointAction;
    public InputActionReference clickAction;
    public InputActionReference rightClickAction;

    [HideInInspector] public bool isPlaying;
    [HideInInspector] public bool isFinished;
    [HideInInspector] public bool isSuccess;
    [HideInInspector] public string resultText;
    // 성공했을 때 목표 구간 한가운데에 얼마나 가까웠나 (1 = 한가운데, 0 = 경계). 코어 루프가 등급·가격에 쓴다
    [HideInInspector] public float quality;
    // 주문 변형: -1 = 적게·연하게, 0 = 보통, 1 = 많이·진하게. 코어 루프가 StartGame 전에 넣고, 미니게임은 목표 구간을 옮긴다
    [HideInInspector] public int variant;

    // 러너/코어 루프가 넣어 줌. 단계 사이로 이어짐
    [HideInInspector] public Cup cup;

    public void StartGame()
    {
        pointAction.action.Enable();
        clickAction.action.Enable();
        rightClickAction.action.Enable();
        isFinished = false;
        isSuccess = false;
        resultText = "";
        quality = 0f;
        ResetGame();
        isPlaying = true;
    }

    // 인내도 초과 등으로 강제 중단
    public void StopGame()
    {
        isPlaying = false;
    }

    public virtual void TimeOver()
    {
        Finish(false, "시간 초과!");
    }

    protected virtual void ResetGame()
    {
    }

    // 결과는 처음 한 번만 반영
    protected void Finish(bool success, string text)
    {
        if (!isPlaying || isFinished) return;
        isPlaying = false;
        isFinished = true;
        isSuccess = success;
        resultText = text;
    }

    // 값이 구간 한가운데면 1, 경계면 0
    protected float QualityOf(float value, float min, float max)
    {
        float half = (max - min) * 0.5f;
        if (half <= 0f) return 1f;
        return Mathf.Clamp01(1f - Mathf.Abs(value - (min + max) * 0.5f) / half);
    }

    protected Vector2 PointerPosition()
    {
        return pointAction.action.ReadValue<Vector2>();
    }

    protected bool ClickHeld()
    {
        return clickAction.action.IsPressed();
    }

    protected bool ClickPressed()
    {
        return clickAction.action.WasPressedThisFrame();
    }

    protected bool ClickReleased()
    {
        return clickAction.action.WasReleasedThisFrame();
    }

    protected bool RightHeld()
    {
        return rightClickAction.action.IsPressed();
    }
}
