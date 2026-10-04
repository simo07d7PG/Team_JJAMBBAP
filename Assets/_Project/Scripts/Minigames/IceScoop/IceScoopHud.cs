using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 얼음 퍼기 전용 화면. 아래쪽 얼음 칸 줄(cupMax칸, 목표 칸은 버건디 테두리) + 개수 + 오른쪽 "유지" 파이.
    /// 표시 전용이다. 개수가 바뀔 때만 칸 색과 글자를 갱신한다.
    /// </summary>
    public class IceScoopHud : MonoBehaviour
    {
        [Header("큰 안내 (Prepare ~ 첫 입력)")]
        [SerializeField] private GameObject guideBig;
        [SerializeField] private TMP_Text instructionText;

        [Header("구석 힌트")]
        [SerializeField] private GameObject hintRoot;

        [Header("얼음 칸 (빌더가 최대 칸 수만큼 만든다)")]
        [SerializeField] private GameObject[] slotRoots;
        [SerializeField] private Image[] slotFills;
        [Tooltip("목표 칸에서만 켜지는 버건디 테두리")]
        [SerializeField] private GameObject[] slotTargetBorders;
        [SerializeField] private TMP_Text countText;

        [Header("유지 파이")]
        [SerializeField] private Image holdFill;

        [SerializeField] private ResultBanner banner;

        private bool bigVisible;
        private int cupMax;
        private int targetMin;
        private int targetMax;
        private int shownCount = -1;
        private float shownHold = -1f;

        public void Configure(int targetMinCount, int targetMaxCount, int cupMaxCount)
        {
            targetMin = targetMinCount;
            targetMax = targetMaxCount;
            cupMax = Mathf.Min(cupMaxCount, slotRoots != null ? slotRoots.Length : 0);
            for (int i = 0; i < slotRoots.Length; i++)
            {
                slotRoots[i].SetActive(i < cupMax);
                // 칸 번호 1부터 목표 구간(targetMin..targetMax)이 목표 칸
                slotTargetBorders[i].SetActive(i + 1 >= targetMin && i + 1 <= targetMax);
            }
            shownCount = -1;
            shownHold = -1f;
            SetState(0, 0f);
            if (banner != null) banner.Hide();
        }

        public void ShowGuide(string instruction)
        {
            if (instructionText != null) instructionText.SetText(instruction ?? string.Empty);
            bigVisible = false;
            SetGuideBig(true);
        }

        public void SetGuideBig(bool visible)
        {
            if (bigVisible == visible) return;
            bigVisible = visible;
            if (guideBig != null) guideBig.SetActive(visible);
            if (hintRoot != null) hintRoot.SetActive(!visible);
        }

        /// <summary>컵 안 얼음 수와 목표 구간 유지 진행도(0~1).</summary>
        public void SetState(int count, float hold01)
        {
            if (count != shownCount)
            {
                shownCount = count;
                for (int i = 0; i < cupMax; i++)
                {
                    bool filled = i < count;
                    bool inTarget = i + 1 >= targetMin && i + 1 <= targetMax;
                    slotFills[i].color = !filled ? (Color)MinigameUiPalette.PaperDark
                        : inTarget ? (Color)MinigameUiPalette.Gold : (Color)MinigameUiPalette.Espresso;
                }
                if (countText != null) countText.SetText(PresentationRules.CountFormat, count);
            }
            if (!Mathf.Approximately(hold01, shownHold))
            {
                shownHold = hold01;
                if (holdFill != null) holdFill.fillAmount = hold01;
            }
        }

        public void ShowResult(in MicrogameResult result)
        {
            // 결과가 나면 큰 안내는 접는다
            bigVisible = true;
            SetGuideBig(false);
            if (banner == null) return;
            banner.Show(PresentationRules.ResultText(result, IceRules.FailText(result.Reason)), result.Success);
        }
    }
}
