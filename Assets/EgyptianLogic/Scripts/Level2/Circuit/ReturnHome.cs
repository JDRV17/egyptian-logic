using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Si el jugador suelta la pieza (compuerta o enchufe) y no queda encajada en un socket,
/// vuelve sola a su sitio de origen. Evita piezas perdidas por el suelo de la cámara.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class ReturnHome : MonoBehaviour
{
    [SerializeField] private float delay = 0.75f;

    private XRBaseInteractable interactable;
    private Rigidbody body;
    private Transform homeParent;
    private Vector3 homeLocalPos;
    private Quaternion homeLocalRot;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        body = GetComponent<Rigidbody>();
        homeParent = transform.parent;
        homeLocalPos = transform.localPosition;
        homeLocalRot = transform.localRotation;
    }

    private void OnEnable() => interactable.selectExited.AddListener(OnReleased);
    private void OnDisable() => interactable.selectExited.RemoveListener(OnReleased);

    private void OnReleased(SelectExitEventArgs args)
    {
        if (isActiveAndEnabled) StartCoroutine(CheckLater());
    }

    private IEnumerator CheckLater()
    {
        yield return new WaitForSeconds(delay);
        if (!interactable.isSelected) GoHome();
    }

    [ContextMenu("Volver al origen")]
    public void GoHome()
    {
        if (homeParent != null && transform.parent != homeParent) transform.SetParent(homeParent, true);

        Vector3 pos = homeParent != null ? homeParent.TransformPoint(homeLocalPos) : homeLocalPos;
        Quaternion rot = homeParent != null ? homeParent.rotation * homeLocalRot : homeLocalRot;
        transform.SetPositionAndRotation(pos, rot);

        if (body != null && !body.isKinematic)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
