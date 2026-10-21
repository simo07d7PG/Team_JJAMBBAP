// 컵 내용물. 한 주문 동안 미니게임 사이로 이어진다. 얼음은 개수, 나머지는 ml
[System.Serializable]
public class Cup
{
    public float espresso;
    public float milk;
    public float water;
    public int ice;

    // 액체 총량(ml). 얼음은 빼고 센다
    public float TotalLiquid()
    {
        return espresso + milk + water;
    }

    public void Clear()
    {
        espresso = 0f;
        milk = 0f;
        water = 0f;
        ice = 0;
    }
}
