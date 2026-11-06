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

    /// <summary>Renderers de la pieza (incluye los hijos). El hueco se los pasa al reloj.</summary>
    public Renderer[] Renderers => GetComponentsInChildren<Renderer>();

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();

        // Empieza apagada aunque su material tenga emisión: solo brilla cuando ya está conectada
        var block = new MaterialPropertyBlock();
        foreach (var r in Renderers)
        {
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(block);
        }
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