using UnityEngine;

public class DogamManager : MonoBehaviour
{
    [Header("Coffee Detail Panels")]
    [SerializeField] private GameObject[] coffeePanels; // 각 커피 패널들을 배열로 등록
    public void OpenCoffeePanel(int index)
    {
        // 모든 패널 끄기
        for (int i = 0; i < coffeePanels.Length; i++)
        {
            if (coffeePanels[i] != null)
            {
                coffeePanels[i].SetActive(false);
            }
        }

        // 선택한 패널 키기
        if (index >= 0 && index < coffeePanels.Length)
        {
            coffeePanels[index].SetActive(true);
        }
    }
}