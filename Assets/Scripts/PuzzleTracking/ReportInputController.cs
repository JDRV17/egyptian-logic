using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ReportInputController : MonoBehaviour
{
    public enum Platform
    {
        PC,
        Quest
    }

    [Header("Platform")]
    [SerializeField] private Platform platform = Platform.PC;

    [Header("Input Fields")]
    [SerializeField] private InputField nameInput;
    [SerializeField] private InputField emailInput;

    [Header("Player")]
    [SerializeField] private MonoBehaviour playerMovement;

    // =========================================================
    // BOTÓN NOMBRE
    // =========================================================
    public void SelectName()
    {
        SetPlayerMovement(false);
        EventSystem.current.SetSelectedGameObject(nameInput.gameObject);

        // El teclado virtual maneja la escritura; ya no llamamos
        // nameInput.ActivateInputField() directamente aquí.
        if (VirtualKeyboardController.Instance != null)
            VirtualKeyboardController.Instance.OpenFor(nameInput);
    }

    // =========================================================
    // BOTÓN CORREO
    // =========================================================
    public void SelectEmail()
    {
        SetPlayerMovement(false);
        EventSystem.current.SetSelectedGameObject(emailInput.gameObject);

        if (VirtualKeyboardController.Instance != null)
            VirtualKeyboardController.Instance.OpenFor(emailInput);
    }

    // =========================================================
    // MOVIMIENTO
    // =========================================================
    private void SetPlayerMovement(bool enabled)
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = enabled;
        }
    }

    // Llamar esto desde MenuManager.CloseGenerarReporte() o desde
    // ReportFormController tras enviar, para reactivar el movimiento.
    public void RestorePlayerMovement()
    {
        SetPlayerMovement(true);
    }
}