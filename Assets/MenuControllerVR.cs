using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuControllerVR : MonoBehaviour
{
    public GameObject creditosPanel;
    public GameObject menu;

    void Start()
    {
        if (creditosPanel != null)
        {
            creditosPanel.SetActive(false);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void doExitGame()
    {
        Application.Quit();
        Debug.Log("Quitting");
    }

    public void ShowCreditos()
    {
        if (creditosPanel != null)
        {
            creditosPanel.SetActive(true);
        }
    }

    public void HideCreditos()
    {
        if (creditosPanel != null)
        {
            creditosPanel.SetActive(false);
        }
    }

    public void ShowMenu()
    {
        if (menu != null)
        {
            menu.SetActive(true);
        }
    }

    public void HideMenu()
    {
        if (menu != null)
        {
            menu.SetActive(false);
        }
    }
}
