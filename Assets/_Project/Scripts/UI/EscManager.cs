using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class EscManager : MonoBehaviour
{
    [Header("Panel to Toggle with ESC")]
    [SerializeField] private GameObject targetPanel;

    public void GameOut()
    {
        Time.timeScale = 1f; // 일시정지 상태로 메인씬으로 돌아가지 않도록 복구
        SceneManager.LoadScene("MainScene");
    }

    private void Update()
    {
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

            if (isActive)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }
    }

    public void OpenPanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void ClosePanel()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시(씬 전환 등) 혹시 시간이 멈춰있지 않도록 안전장치
        Time.timeScale = 1f;
    }
}