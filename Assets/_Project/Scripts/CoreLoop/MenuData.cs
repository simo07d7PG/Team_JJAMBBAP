using UnityEngine;

// 메뉴 에셋. 메뉴를 추가하려면 Project 창에서 Create > BariBarista > Menu로 에셋을 하나 만든다
[CreateAssetMenu(fileName = "Menu", menuName = "BariBarista/Menu")]
public class MenuData : ScriptableObject
{
    [Tooltip("말풍선에 보이는 이름")]
    public string menuName;
    [Tooltip("정가(원). 성공한 단계 비율만큼 받는다")]
    public int price;
    [Tooltip("만드는 순서대로 기계 id (espresso / ice / milk 등)")]
    public string[] steps;
}
