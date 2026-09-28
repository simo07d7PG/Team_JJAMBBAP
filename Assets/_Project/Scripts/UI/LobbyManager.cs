using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

    public void GameStart() // scene change
    {
        SceneManager.LoadScene("GameScene");
    }
    public void SettingPopup() // setting panel popup
    {
        settingPanel.SetActive(true);
    }
    public void GameExit() // game exit
    {
        Application.Quit();
    }
}
