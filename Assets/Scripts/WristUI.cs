using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WristUI : MonoBehaviour
{
    public InputActionAsset inputActions;
    private Canvas wristCanvasMenu;
    public InputAction menu;
    // Start is called before the first frame update
    void Start()
    {
        wristCanvasMenu = GetComponent<Canvas>();
        menu = inputActions.FindActionMap("XRI RightHand").FindAction("Menu");
        menu.Enable();
        wristCanvasMenu.enabled = false;
        menu.performed += ToggleMenu;
    }

    // Update is called once per frame
    private void OnDestroy() {
        menu.performed -= ToggleMenu;
    }

    public void ToggleMenu(InputAction.CallbackContext context){
        wristCanvasMenu.enabled = !wristCanvasMenu.enabled;
    }
}
