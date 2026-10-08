using TMPro;
using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>결과 큰 글씨(성공 칭찬, 실패 이유). 세 미니게임이 같이 쓰는 동작 컴포넌트다.</summary>
    public class ResultBanner : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        public void Show(string text, bool success)
        {
            if (label == null) return;
            label.SetText(text);
            label.color = success ? MinigameUiPalette.ResultSuccess : MinigameUiPalette.ResultFail;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
