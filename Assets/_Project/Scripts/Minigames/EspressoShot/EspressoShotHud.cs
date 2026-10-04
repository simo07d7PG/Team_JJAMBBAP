using TMPro;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 에스프레소 샷 전용 화면. 오른쪽 세로 "샷 잔" 게이지(목표 띠 + "여기서 떼!") + 숫자 ml.
    /// 표시 전용이다. 값이 바뀔 때만 글자와 위치를 갱신해서 매 프레임 할당이 없다.
    /// </summary>
    public class EspressoShotHud : MonoBehaviour
    {
        [Header("큰 안내 (Prepare ~ 첫 입력)")]
        [SerializeField] private GameObject guideBig;
        [SerializeField] private TMP_Text instructionText;
        [Tooltip("컵이 받침 밖에서 시작할 때만 보이는 안내 줄")]
        [SerializeField] private GameObject guideCupRow;

        [Header("구석 힌트")]
        [SerializeField] private GameObject hintRoot;
        [SerializeField] private TMP_Text hintText;

        [Header("게이지")]
        [Tooltip("채움. anchorMax.y = 값")]
        [SerializeField] private RectTransform fill;
        [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
        [SerializeField] private RectTransform band;
        [Tooltip("\"여기서 떼!\" 글씨. 띠 가운데 높이로 옮긴다")]
        [SerializeField] private RectTransform bandLabel;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text valueText;

        [SerializeField] private ResultBanner banner;

        private bool bigVisible;
        private int hintState = -1;
        private float fill01 = -1f;
        private int shownMl = -1;

        /// <summary>Lv마다 목표·용량을 맞추고 화면을 처음 상태로 돌린다(ResetState에서).</summary>
        public void Configure(float targetMinMl, float targetMaxMl, float capacityMl, bool misplacedCup)
        {
            float min01 = Mathf.Clamp01(targetMinMl / capacityMl);
            float max01 = Mathf.Clamp01(targetMaxMl / capacityMl);
            // 폭이 아주 좁은 구간도 보이게 최소 두께를 준다
            float width = Mathf.Max(max01 - min01, 0.02f);
            float mid = (min01 + max01) * 0.5f;
            band.anchorMin = new Vector2(band.anchorMin.x, mid - width * 0.5f);
            band.anchorMax = new Vector2(band.anchorMax.x, mid + width * 0.5f);
            band.offsetMin = new Vector2(band.offsetMin.x, 0f);
            band.offsetMax = new Vector2(band.offsetMax.x, 0f);
            if (bandLabel != null)
            {
                bandLabel.anchorMin = new Vector2(bandLabel.anchorMin.x, mid);
                bandLabel.anchorMax = new Vector2(bandLabel.anchorMax.x, mid);
                bandLabel.offsetMin = new Vector2(0f, -24f);
                bandLabel.offsetMax = new Vector2(0f, 24f);
            }
            if (capacityText != null) capacityText.SetText(PresentationRules.MlFormat, Mathf.RoundToInt(capacityMl));
            if (guideCupRow != null) guideCupRow.SetActive(misplacedCup);

            fill01 = -1f;
            shownMl = -1;
            hintState = -1;
            SetAmount(0f, capacityMl);
            SetCupHint(false);
            if (banner != null) banner.Hide();
        }

        /// <summary>Prepare에서: 지시어를 쓰고 큰 안내를 켠다.</summary>
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

        /// <summary>받침 밖의 컵을 먼저 끌어 놓으라는 구석 힌트. 값이 바뀔 때만 글자를 바꾼다.</summary>
        public void SetCupHint(bool needCup)
        {
            int state = needCup ? 1 : 0;
            if (hintState == state) return;
            hintState = state;
            if (hintText != null) hintText.SetText(needCup ? ShotRules.HintCup : ShotRules.Hint);
        }

        public void SetAmount(float ml, float capacityMl)
        {
            float v = capacityMl > 0f ? Mathf.Clamp01(ml / capacityMl) : 0f;
            if (!Mathf.Approximately(v, fill01))
            {
                fill01 = v;
                fill.anchorMax = new Vector2(fill.anchorMax.x, v);
                fill.gameObject.SetActive(v > 0f);
            }
            int whole = Mathf.RoundToInt(ml);
            if (whole != shownMl)
            {
                shownMl = whole;
                if (valueText != null) valueText.SetText(PresentationRules.MlFormat, whole);
            }
        }

        public void ShowResult(in MicrogameResult result)
        {
            // 결과가 나면 큰 안내는 접는다
            bigVisible = true;
            SetGuideBig(false);
            if (banner == null) return;
            banner.Show(PresentationRules.ResultText(result, ShotRules.FailText(result.Reason)), result.Success);
        }
    }
}
