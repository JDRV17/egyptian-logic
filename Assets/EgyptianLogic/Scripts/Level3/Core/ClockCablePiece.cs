using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// La pieza que repara un cable del reloj. Va en un objeto con XRGrabInteractable y Rigidbody.
/// El ClockCableSocket solo acepta objetos con este componente.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class ClockCablePiece : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody body;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
    }

    /// <summary>La deja fija en el hueco: ya no se puede agarrar.</summary>
    public void LockAt(Transform socket)
    {
        StartCoroutine(LockRoutine(socket));
    }

    private IEnumerator LockRoutine(Transform socket)
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        if (grab != null) grab.enabled = false;
        Freeze();

        // Un frame para que el Drop del grab termine (puede restaurar parent y Rigidbody)
        yield return null;

        transform.SetParent(socket, true);
        transform.SetPositionAndRotation(pos, rot);
        Freeze();
    }

    private void Freeze()
    {
        if (body == null) return;
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
    }
}
