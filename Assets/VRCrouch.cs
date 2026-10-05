using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class VRCrouch : MonoBehaviour
{
    public Transform cameraOffset; // Camera Offset dentro del XR Origin

    public float standingHeight = 1.6f;
    public float crouchHeight = 1.0f;

    public float transitionSpeed = 5f;

    public InputActionProperty crouchAction; // Left Stick Click

    private bool isCrouching = false;
    private float targetHeight;

    void Start()
    {
        targetHeight = standingHeight;
    }

    void Update()
    {
        // Detectar click del joystick izquierdo
        if (crouchAction.action.WasPressedThisFrame())
        {
            ToggleCrouch();
        }

        // Transición suave de altura
        Vector3 pos = cameraOffset.localPosition;
        pos.y = Mathf.Lerp(pos.y, targetHeight, Time.deltaTime * transitionSpeed);
        cameraOffset.localPosition = pos;
    }

    void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        if (isCrouching)
            targetHeight = crouchHeight;
        else
            targetHeight = standingHeight;
    }
}
