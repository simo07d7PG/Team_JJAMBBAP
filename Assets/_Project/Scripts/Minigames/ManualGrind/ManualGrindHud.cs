using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 수동 그라인더 화면. 진행 막대(원형·가로 모두 Filled Image) + "3 / 6바퀴" + 시작 안내(화살표). 표시만 한다
public class ManualGrindHud : MonoBehaviour
{
    [Header("진행")]
    [Tooltip("Image Type = Filled. Radial360이면 원형, Horizontal이면 가로 막대")]
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text turnsText;

    [Header("안내")]
    [Tooltip("\"시계 방향으로 돌려!\" 글씨 + 화살표를 묶은 오브젝트")]
    [SerializeField] private GameObject hintRoot;
    [Tooltip("빙글 도는 화살표 (비우면 회전 없음)")]
    [SerializeField] private RectTransform hintArrow;
    [Tooltip("화살표가 시계 방향으로 도는 속도(도/초)")]
    [SerializeField] private float arrowSpinSpeed = 240f;

    [Header("연출")]
    [Tooltip("한 바퀴마다 바퀴 글자가 커지는 정도")]
    [SerializeField] private float pulseScale = 0.3f;
    [Tooltip("커진 글자가 가라앉는 속도")]
    [SerializeField] private float pulseDecaySpeed = 5f;

    private const string TurnsFormat = "{0} / {1}바퀴";

    private bool saved;
    private Vector3 textRestScale;
    private int neededTurns;
    private float pulse;

    // 처음 쓸 때 원래 크기를 기억한다 (Configure가 Awake보다 먼저 불릴 수 있다)
    private void SaveRest()
    {
        if (saved) return;
        saved = true;
        textRestScale = turnsText.rectTransform.localScale;
    }

    public void Configure(int needed)
    {
        SaveRest();
        neededTurns = needed;
        pulse = 0f;
        turnsText.rectTransform.localScale = textRestScale;
        if (hintArrow != null) hintArrow.localRotation = Quaternion.identity;
        SetProgress(0f, 0);
    }

    public void SetProgress(float progress01, int turnsDone)
    {
        progressFill.fillAmount = Mathf.Clamp01(progress01);
        turnsText.SetText(TurnsFormat, turnsDone, neededTurns);
    }

    public void SetHint(bool visible)
    {
        if (hintRoot.activeSelf != visible) hintRoot.SetActive(visible);
    }

    public void PulseTurn()
    {
        pulse = 1f;
    }

    private void Update()
    {
        if (!saved) return;
        if (pulse > 0f)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * pulseDecaySpeed);
            turnsText.rectTransform.localScale = textRestScale * (1f + pulseScale * pulse);
        }
        // UI에서 시계 방향은 z 음수
        if (hintArrow != null && hintRoot.activeInHierarchy)
        {
            hintArrow.Rotate(0f, 0f, -arrowSpinSpeed * Time.deltaTime);
        }
    }
}
