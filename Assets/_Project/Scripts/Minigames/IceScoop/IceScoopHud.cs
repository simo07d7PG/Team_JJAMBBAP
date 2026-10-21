using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 얼음 퍼기 화면. 아래쪽 얼음 칸 줄(cupMax칸, 목표 칸은 버건디 테두리) + 개수 + 오른쪽 "유지" 파이. 표시만 한다
public class IceScoopHud : MonoBehaviour
{
    [Header("얼음 칸 (최대 칸 수만큼 미리 만들어 둔다)")]
    [SerializeField] private GameObject[] slotRoots;
    [SerializeField] private Image[] slotFills;
    [Tooltip("목표 칸에서만 켜지는 버건디 테두리")]
    [SerializeField] private GameObject[] slotTargetBorders;
    [SerializeField] private TMP_Text countText;

    [Header("유지 파이")]
    [SerializeField] private Image holdFill;

    private int cupMax;
    private int targetMin;
    private int targetMax;

    public void Configure(int targetMinCount, int targetMaxCount, int cupMaxCount)
    {
        targetMin = targetMinCount;
        targetMax = targetMaxCount;
        cupMax = Mathf.Min(cupMaxCount, slotRoots.Length);
        for (int i = 0; i < slotRoots.Length; i++)
        {
            slotRoots[i].SetActive(i < cupMax);
            // 칸 번호 1부터 목표 구간(targetMin..targetMax)이 목표 칸
            slotTargetBorders[i].SetActive(i + 1 >= targetMin && i + 1 <= targetMax);
        }
        SetState(0, 0f);
    }

    // 컵 안 얼음 수와 목표 구간 유지 진행도(0~1)
    public void SetState(int count, float hold01)
    {
        for (int i = 0; i < cupMax; i++)
        {
            bool inTarget = i + 1 >= targetMin && i + 1 <= targetMax;
            if (i >= count) slotFills[i].color = MinigameUiPalette.PaperDark;
            else if (inTarget) slotFills[i].color = MinigameUiPalette.Gold;
            else slotFills[i].color = MinigameUiPalette.Espresso;
        }
        countText.SetText("{0}개", count);
        holdFill.fillAmount = hold01;
    }
}
