using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 월드 공간 세로 게이지. fill·band는 아래쪽이 원점(0)이고 높이 1 기준으로 만든 피벗 Transform이다.
    /// 텍스트가 없어서 한글 폰트 문제와 무관하다.
    /// </summary>
    public class VerticalGauge : MonoBehaviour
    {
        [Tooltip("채움 피벗. localScale.y = 값")]
        [SerializeField] private Transform fill;
        [Tooltip("목표 구간 피벗. localPosition.y = 최소, localScale.y = 폭")]
        [SerializeField] private Transform band;

        private float value01 = -1f;

        public void SetRange(float min01, float max01)
        {
            if (band == null) return;
            min01 = Mathf.Clamp01(min01);
            max01 = Mathf.Clamp01(Mathf.Max(min01, max01));
            // 폭이 0인 구간(정확히 맞추기)도 보이게 최소 두께를 준다
            float width = Mathf.Max(max01 - min01, 0.02f);
            Vector3 p = band.localPosition;
            band.localPosition = new Vector3(p.x, min01 - (width - (max01 - min01)) * 0.5f, p.z);
            Vector3 s = band.localScale;
            band.localScale = new Vector3(s.x, width, s.z);
        }

        public void SetValue(float v01)
        {
            if (fill == null) return;
            v01 = Mathf.Clamp01(v01);
            if (Mathf.Approximately(v01, value01)) return;
            value01 = v01;
            Vector3 s = fill.localScale;
            fill.localScale = new Vector3(s.x, Mathf.Max(v01, 0.0001f), s.z);
            fill.gameObject.SetActive(v01 > 0f);
        }
    }
}
