using System;

namespace BariBarista.Minigames
{
    /// <summary>컵에 들어갈 수 있는 재료. 얼음은 개수, 나머지는 ml 단위.</summary>
    public enum Ingredient
    {
        Espresso = 0,
        Water = 1,
        Milk = 2,
        Ice = 3,
        Syrup = 4,
    }

    /// <summary>
    /// 컵 내용물 데이터. 한 주문 동안 미니게임 사이를 이어 가며 "컵이 모든 걸 기억한다" 판정의 근거가 된다.
    /// 순수 C#이라 엔진 없이 테스트할 수 있다.
    /// </summary>
    [Serializable]
    public sealed class CupContents
    {
        public const int IngredientCount = 5;

        private readonly float[] amounts = new float[IngredientCount];

        public float Get(Ingredient ingredient) => amounts[(int)ingredient];

        /// <summary>재료를 더한다. 음수나 0은 무시한다.</summary>
        public void Add(Ingredient ingredient, float amount)
        {
            if (amount <= 0f) return;
            amounts[(int)ingredient] += amount;
        }

        /// <summary>모든 재료를 비운다.</summary>
        public void Clear()
        {
            Array.Clear(amounts, 0, IngredientCount);
        }

        /// <summary>액체 총량(ml). 얼음은 개수라서 제외한다.</summary>
        public float TotalLiquid
        {
            get
            {
                float sum = 0f;
                for (int i = 0; i < IngredientCount; i++)
                {
                    if (i == (int)Ingredient.Ice) continue;
                    sum += amounts[i];
                }
                return sum;
            }
        }

        public int IceCount => (int)Math.Round(amounts[(int)Ingredient.Ice]);

        public bool IsEmpty => TotalLiquid <= 0f && IceCount <= 0;

        /// <summary>다른 컵의 내용을 그대로 복사한다(할당 없음).</summary>
        public void CopyFrom(CupContents other)
        {
            if (other == null) { Clear(); return; }
            Array.Copy(other.amounts, amounts, IngredientCount);
        }

        public CupContents Clone()
        {
            var copy = new CupContents();
            copy.CopyFrom(this);
            return copy;
        }

        public override string ToString()
        {
            return $"Espresso {Get(Ingredient.Espresso):0}ml, Water {Get(Ingredient.Water):0}ml, Milk {Get(Ingredient.Milk):0}ml, Ice {IceCount}, Syrup {Get(Ingredient.Syrup):0}ml";
        }
    }
}
