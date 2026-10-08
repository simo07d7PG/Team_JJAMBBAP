namespace BariBarista.Minigames
{
    /// <summary>컵 액체 맨 위에 올리는 층.</summary>
    public enum TopLayer
    {
        None = 0,
        Crema = 1,
        Foam = 2,
    }

    /// <summary>재료 양으로 컵 액체 맨 위 층을 고른다. 순수 함수라 엔진 없이 테스트한다.</summary>
    public static class LiquidLook
    {
        /// <summary>우유가 액체의 이 비율 이상이면 거품이 맨 위를 덮는다.</summary>
        public const float FoamShare = 0.2f;

        /// <summary>양(ml)은 에스프레소·우유·물·시럽 순서로 받는다. 음수는 0으로 본다.</summary>
        public static TopLayer TopLayerFor(float espresso, float milk, float water, float syrup)
        {
            espresso = espresso > 0f ? espresso : 0f;
            milk = milk > 0f ? milk : 0f;
            water = water > 0f ? water : 0f;
            syrup = syrup > 0f ? syrup : 0f;
            float total = espresso + milk + water + syrup;
            if (total <= 0f) return TopLayer.None;
            if (milk / total >= FoamShare) return TopLayer.Foam;
            if (espresso > 0f) return TopLayer.Crema;
            return TopLayer.None;
        }
    }
}
