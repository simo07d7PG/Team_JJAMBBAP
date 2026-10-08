using UnityEngine;

namespace BariBarista.Minigames
{
    /// <summary>
    /// 팀 UI(UI/HongJunGi/Guide_Penal.png, Menu_Panel.png)에서 뽑은 색. 미니게임 Canvas는 이 색만 쓴다.
    /// 목표 구간은 초록이 아니라 버건디 테두리 + 밝은 갈색 채움이다.
    /// </summary>
    public static class MinigameUiPalette
    {
        /// <summary>베이지 종이 바탕.</summary>
        public static readonly Color32 Paper = new Color32(237, 189, 125, 255);
        /// <summary>종이 그늘·빈 칸.</summary>
        public static readonly Color32 PaperDark = new Color32(216, 170, 108, 255);
        /// <summary>밝은 갈색(목표 구간 채움).</summary>
        public static readonly Color32 BrownLight = new Color32(196, 140, 78, 255);
        /// <summary>진갈색 글씨.</summary>
        public static readonly Color32 Ink = new Color32(47, 22, 6, 255);
        /// <summary>가장 어두운 윤곽.</summary>
        public static readonly Color32 InkDeep = new Color32(33, 19, 19, 255);
        /// <summary>버건디 테두리.</summary>
        public static readonly Color32 Burgundy = new Color32(145, 39, 43, 255);
        /// <summary>강조 금색.</summary>
        public static readonly Color32 Gold = new Color32(250, 214, 112, 255);
        /// <summary>에스프레소 채움.</summary>
        public static readonly Color32 Espresso = new Color32(92, 52, 24, 255);
        /// <summary>우유 채움.</summary>
        public static readonly Color32 Milk = new Color32(250, 240, 220, 255);
        /// <summary>결과 글씨(실패).</summary>
        public static readonly Color32 ResultFail = new Color32(255, 160, 140, 255);
        /// <summary>결과 글씨(성공).</summary>
        public static readonly Color32 ResultSuccess = new Color32(255, 232, 150, 255);
    }
}
