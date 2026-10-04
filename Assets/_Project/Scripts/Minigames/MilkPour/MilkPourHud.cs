using TMPro;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 우유 붓기 전용 화면. 왼쪽 컵 단면(우유 높이 + 목표 띠) + 퍼센트 + 아래 "흘림" 막대.
    /// 표시 전용이다. 값이 바뀔 때만 글자와 위치를 갱신해서 매 프레임 할당이 없다.
    /// </summary>
    public class MilkPourHud : MonoBehaviour
    {
        [Header("큰 안내 (Prepare ~ 첫 입력)")]
        [SerializeField] private GameObject guideBig;
        [SerializeField] private TMP_Text instructionText;

        [Header("구석 힌트")]
        [SerializeField] private GameObject hintRoot;

        [Header("컵 단면")]
        [Tooltip("우유 높이. anchorMax.y = Fill01")]
        [SerializeField] private RectTransform fill;
        [Tooltip("목표 띠. anchorMin.y/anchorMax.y = 구간")]
        [SerializeField] private RectTransform band;
        [SerializeField] private RectTransform bandLabel;
        [SerializeField] private TMP_Text percentText;

        [Header("흘림 막대")]
        [Tooltip("흘린 양. anchorMax.x = OutsideMl / MaxOutsideMl")]
        [SerializeField] private RectTransform spillFill;

        [SerializeField] private ResultBanner banner;

        private bool bigVisible;
        private float fill01 = -1f;
        private float spill01 = -1f;
        private int shownPercent = -1;

        public void Configure(float targetMin01, float targetMax01)
        {
            float width = Mathf.Max(targetMax01 - targetMin01, 0.02f);
            float mid = (targetMin01 + targetMax01) * 0.5f;
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
            fill01 = -1f;
            spill01 = -1f;
            shownPercent = -1;
            SetFill(0f);
            SetSpill(0f);
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

        public void SetFill(float v01)
        {
            v01 = Mathf.Clamp01(v01);
            if (!Mathf.Approximately(v01, fill01))
            {
                fill01 = v01;
                fill.anchorMax = new Vector2(fill.anchorMax.x, v01);
                fill.gameObject.SetActive(v01 > 0f);
            }
            int percent = Mathf.RoundToInt(v01 * 100f);
            if (percent != shownPercent)
            {
                shownPercent = percent;
                if (percentText != null) percentText.SetText(PresentationRules.PercentFormat, percent);
            }
        }

        public void SetSpill(float v01)
        {
            v01 = Mathf.Clamp01(v01);
            if (Mathf.Approximately(v01, spill01)) return;
            spill01 = v01;
            spillFill.anchorMax = new Vector2(v01, spillFill.anchorMax.y);
        }

        public void ShowResult(in MicrogameResult result)
        {
            // 결과가 나면 큰 안내는 접는다
            bigVisible = true;
            SetGuideBig(false);
            if (banner == null) return;
            banner.Show(PresentationRules.ResultText(result, PourRules.FailText(result.Reason)), result.Success);
        }
    }
}
