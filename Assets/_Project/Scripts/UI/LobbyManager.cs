using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject questPanel;

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
    }

    public void OpenQuestPanel() // QuestPanel
    {
        if (questPanel != null) questPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
    }
    public void CloseAllPanels() // all Panel
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (questPanel != null) questPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
    }
    public void GameExit() // game exit
    {
        Application.Quit();
    }
}
