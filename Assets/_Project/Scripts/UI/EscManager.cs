using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class EscManager : MonoBehaviour
{
    [Header("Panel to Toggle with ESC")]
    [SerializeField] private GameObject targetPanel;

    public void GameOut() // scene change
    {
        SceneManager.LoadScene("MainScene");
    }
    private void Update()
    {
        // Keyboard.current가 존재하는지 확인 후 Escape 키가 눌렸는지 체크
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (targetPanel != null)
        {
            bool isActive = targetPanel.activeSelf;
            targetPanel.SetActive(!isActive);
        }
    }

    public void ClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }

    public void OpenPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
        }
    }
}