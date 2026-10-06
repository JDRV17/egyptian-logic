using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ObjectZoomPC : MonoBehaviour
{
    [Header("Configuración de Zoom General")]
    [Tooltip("Velocidad con la que se acerca/aleja el objeto sujetado")]
    public float scrollSpeed = 2.0f;

    [Tooltip("Distancia mínima respecto a la cámara")]
    public float minDistance = 0.3f;

    [Tooltip("Distancia máxima respecto a la cámara")]
    public float maxDistance = 3.0f;

    private XRRayInteractor rayInteractor;
    private Transform attachTransform;
    private Vector3 originalAttachLocalPos;

    void Awake()
    {
        rayInteractor = GetComponent<XRRayInteractor>();

        if (rayInteractor != null)
        {
            // Si el interactor no tiene un attachTransform asignado, usamos el suyo propio
            if (rayInteractor.attachTransform == null)
            {
                GameObject newAttach = new GameObject("Dynamic_Attach_Transform");
                newAttach.transform.SetParent(transform);
                newAttach.transform.localPosition = Vector3.forward * 0.5f;
                newAttach.transform.localRotation = Quaternion.identity;
                rayInteractor.attachTransform = newAttach.transform;
            }

            attachTransform = rayInteractor.attachTransform;
            originalAttachLocalPos = attachTransform.localPosition;
        }
    }

    void Update()
    {
        if (rayInteractor == null || attachTransform == null) return;

        // Verificar si hay algún objeto agarrado
        if (rayInteractor.hasSelection)
        {
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scrollInput) > 0.001f)
            {
                // Mover el punto de anclaje hacia adelante o hacia atrás en Z local
                Vector3 newLocalPos = attachTransform.localPosition + Vector3.forward * (scrollInput * scrollSpeed);

                // Limitar la distancia para que no se acerque ni se aleje demasiado
                newLocalPos.z = Mathf.Clamp(newLocalPos.z, minDistance, maxDistance);
                attachTransform.localPosition = newLocalPos;
            }
        }
        else
        {
            // Resetear la posición del attach al soltar el objeto
            if (attachTransform.localPosition != originalAttachLocalPos)
            {
                attachTransform.localPosition = originalAttachLocalPos;
            }
        }
    }
}