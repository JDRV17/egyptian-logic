using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class MouseVerticalRotation : MonoBehaviour
{
    private XRGrabInteractable grabInteractable;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void Update()
    {
        // Solo si ESTE objeto está siendo agarrado
        if (!grabInteractable.isSelected) return;

        // Click derecho (una sola vez)
        if (Input.GetMouseButtonDown(1))
        {
            RotateObject();
        }
    }

    void RotateObject()
    {
        // Obtener cámara del jugador
        Transform cam = Camera.main.transform;

        // Eje vertical relativo a la cámara (para inclinar hacia adelante)
        Vector3 rotationAxis = cam.right;

        // Rotar 90 grados respecto al jugador
        transform.Rotate(rotationAxis, 90f, Space.World);
    }
}
