using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 상점 물건 칸 하나. CafeShop이 원본을 복제해 채운다. 버튼 OnClick → OnBuy
public class ShopCard : MonoBehaviour
{
    [HideInInspector] public CafeShop shop;
    [HideInInspector] public int index;

    public Image background;
    public Image icon;
    public GameObject lockIcon;
    public TMP_Text kindText;
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text priceText;
    public Button button;
    public TMP_Text buttonLabel;

    public void OnBuy()
    {
        shop.Buy(index);
    }
}
