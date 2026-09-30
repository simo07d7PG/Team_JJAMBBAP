using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject questPanel;

    [Header("Buttons")]
    [SerializeField] private Image menuButtonImage;
    [SerializeField] private Image questButtonImage;

    private Color normalColor = Color.white; // 기본 상태
    private Color activeColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public void GameStart() // scene change
    {
        SceneManager.LoadScene("GameScene");
    }
    public void SettingPopup() // setting panel popup
    {
        settingPanel.SetActive(true);
    }
    public void OpenMenuPanel() // MenuPanel
    {
        if (menuPanel != null) menuPanel.SetActive(true);
        if (questPanel != null) questPanel.SetActive(false);

        // button color
        SetButtonColor(menuButtonImage, activeColor);
        SetButtonColor(questButtonImage, normalColor);
    }

    public void OpenQuestPanel() // QuestPanel
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);

        // button color
        SetButtonColor(menuButtonImage, normalColor);  
        SetButtonColor(questButtonImage, activeColor);
    }
    public void CloseAllPanels() // all Panel
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);

        // button color
        SetButtonColor(menuButtonImage, normalColor);
        SetButtonColor(questButtonImage, normalColor);
    }
    private void SetButtonColor(Image btnImage, Color targetColor)
    {
        if (btnImage != null)
        {
            btnImage.color = targetColor;
        }
    }
    public void GameExit() // game exit
    {
        Application.Quit();
    }
}
