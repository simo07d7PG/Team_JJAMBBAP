using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;       // 세팅 패널
    [SerializeField] private GameObject guidePanel;         // 도감 패널
    [SerializeField] private GameObject menuPanel;          // 메뉴 패널
    [SerializeField] private GameObject questPanel;         // 업적 패널

    [Header("Coffee Detail Panels")]
    [SerializeField] private GameObject[] coffeePanels;     // 각 커피 상세 패널

    [Header("Coffee Buttons UI")]
    [SerializeField] private Image[] coffeeButtonImages;    // 각 커피 버튼의 최상위 Image 컴포넌트들

    // 선택 여부에 따른 버튼 색상 설정
    private Color normalColor = Color.white;                       // 선택됨
    private Color darkColor = new Color(0.5f, 0.5f, 0.5f, 1f);     // 선택 안 됨

    // 크기 변수
    private Vector3 normalScale = Vector3.one;
    private Vector3 selectedScale = new Vector3(1.1f, 1.1f, 1.1f);
    private void Start()
    {
        CloseAllCoffeePanels();
    }
    public void OpenCoffeePanel(int index)
    {
        // 선택한 패널만 켜고 나머지 끄기
        for (int i = 0; i < coffeePanels.Length; i++)
        {
            if (coffeePanels[i] != null)
            {
                coffeePanels[i].SetActive(i == index);
            }
        }

        // 선택된 패널 및 버튼 레이어, 색상 처리
        for (int i = 0; i < coffeeButtonImages.Length; i++)
        {
            if (coffeeButtonImages[i] != null)
            {
                bool isSelected = (i == index);
                Color targetColor = isSelected ? normalColor : darkColor;
                Vector3 targetScale = isSelected ? selectedScale : normalScale;

                // 버튼의 Transform 크기 설정
                coffeeButtonImages[i].transform.localScale = targetScale;

                // 클릭된 버튼만 밝게,안 눌린 버튼은 어둡게
                Image[] childImages = coffeeButtonImages[i].GetComponentsInChildren<Image>();
                foreach (Image img in childImages)
                {
                    img.color = targetColor;
                }
            }
        }
    }

    // 2. 커피 상세 패널의 닫기 버튼
    public void CloseAllCoffeePanels()
    {
        // 2-1. 모든 커피 상세 패널 끄기
        for (int i = 0; i < coffeePanels.Length; i++)
        {
            if (coffeePanels[i] != null)
            {
                coffeePanels[i].SetActive(false);
            }
        }

        // 2-2. 모든 커피 버튼들 색상 복구
        for (int i = 0; i < coffeeButtonImages.Length; i++)
        {
            if (coffeeButtonImages[i] != null)
            {
                // 크기 복원
                coffeeButtonImages[i].transform.localScale = normalScale;

                Image[] childImages = coffeeButtonImages[i].GetComponentsInChildren<Image>();
                foreach (Image img in childImages)
                {
                    img.color = normalColor; // 전부 밝게
                }
            }
        }
    }

    // --- 기본 로비 제어 메서드들 ---
    public void GameStart() => SceneManager.LoadScene("GameScene");
    public void SettingPopup() => settingPanel.SetActive(true);
    public void GuidePopup()
    {
        if (guidePanel != null) guidePanel.SetActive(true);
        CloseAllCoffeePanels();
    }

    public void OpenMenuPanel()
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (questPanel != null) questPanel.SetActive(false);
        CloseAllCoffeePanels();
    }

    public void OpenQuestPanel()
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
        CloseAllCoffeePanels();
    }

    public void CloseAllPanels()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        CloseAllCoffeePanels();
    }

    public void GameExit() => Application.Quit();
}