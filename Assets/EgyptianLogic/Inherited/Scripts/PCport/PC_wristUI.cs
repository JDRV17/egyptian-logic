using UnityEngine;

public class PC_wristUI : MonoBehaviour
{
    private Canvas wristCanvasMenu;
    private bool isOpen = false;

    public MonoBehaviour playerControllerScript;

    void Start()
    {
        wristCanvasMenu = GetComponent<Canvas>();

        isOpen = false;
        wristCanvasMenu.enabled = false;

        Time.timeScale = 1f;

        if (playerControllerScript != null)
            playerControllerScript.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isOpen = !isOpen;
        wristCanvasMenu.enabled = isOpen;

        if (isOpen)
        {
            Time.timeScale = 0f;

            if (playerControllerScript != null)
                playerControllerScript.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;

            if (playerControllerScript != null)
                playerControllerScript.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
