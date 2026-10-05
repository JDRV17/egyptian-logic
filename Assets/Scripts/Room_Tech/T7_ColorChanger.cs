using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class T7_ColorChanger : MonoBehaviour
{
    public Color BlueColor;
    public Color RedColor;
    public Color StartColor;

    public string currentMarker;

    void Start()
    {
        transform.GetComponent<Renderer>().material.color = StartColor;
    }

    // --- PC: clic de mouse directo sobre el área ---
    // Requiere un Collider en este objeto (ya lo tenías, para el trigger anterior).
    void OnMouseDown()
    {
        Paint();
    }

    // --- VR: conectar este método al evento "Select Entered" de un
    // XRSimpleInteractable agregado a este mismo objeto, así el gatillo
    // del controlador dispara lo mismo que el clic en PC. ---
    public void OnXRSelect(SelectEnterEventArgs args)
    {
        Paint();
    }

    private void Paint()
    {
        if (PaintModeManager.Instance == null)
        {
            Debug.LogWarning("PaintModeManager.Instance es null. Verifica que exista en la escena.");
            return;
        }

        string colorToApply = PaintModeManager.Instance.CurrentColor;

        if (colorToApply == "R")
        {
            transform.GetComponent<Renderer>().material.color = RedColor;
            currentMarker = "R";
        }
        else if (colorToApply == "B")
        {
            transform.GetComponent<Renderer>().material.color = BlueColor;
            currentMarker = "B";
        }
    }
}