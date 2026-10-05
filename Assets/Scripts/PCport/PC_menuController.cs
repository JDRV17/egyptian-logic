using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PC_menuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;     // Panel principal (botones Jugar, Creditos, Salir)
    public GameObject creditsPanel;  // Panel de Créditos

    void Start()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (mainPanel != null)
            mainPanel.SetActive(true);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void doExitGame()
    {
        Application.Quit();
        Debug.Log("Quitting");
    }

    public void ShowCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(true);

        if (mainPanel != null)
            mainPanel.SetActive(false);
    }

    public void HideCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (mainPanel != null)
            mainPanel.SetActive(true);
    }
}
