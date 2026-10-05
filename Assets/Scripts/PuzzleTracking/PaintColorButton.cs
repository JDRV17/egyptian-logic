using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PaintColorButton : MonoBehaviour
{
    [Tooltip("Marca true en el botón/objeto ROJO, false en el AZUL.")]
    public bool isRedButton;

    // --- PC: clic de mouse ---
    void OnMouseDown()
    {
        SelectColor();
    }

    // --- VR: conectar al evento "Select Entered" de un XRSimpleInteractable ---
    public void OnXRSelect(SelectEnterEventArgs args)
    {
        SelectColor();
    }

    private void SelectColor()
    {
        if (PaintModeManager.Instance == null) return;

        if (isRedButton)
            PaintModeManager.Instance.SetRed();
        else
            PaintModeManager.Instance.SetBlue();
    }
}