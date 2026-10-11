using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class HangingKey : MonoBehaviour
{
    [Header("Colisiones que NO la devuelven")]
    [Tooltip("La cerradura y sus partes. Tocar cualquier hijo de estos objetos no resetea la llave.")]
    [SerializeField] private Transform[] safeRoots;

    [Tooltip("Tiempo tras soltarla en que se ignoran colisiones (evita resets al soltarla cerca de la cerradura).")]
    [SerializeField] private float graceTime = 0.25f;

    [Header("Identidad")]
    [SerializeField] private int keyValue = 1;
    public int KeyValue => keyValue;

    private Rigidbody rb;
    private XRGrabInteractable grab;
    private Vector3 homePos;
    private Quaternion homeRot;
    private float lastReleaseTime = -10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        homePos = transform.position;
        homeRot = transform.rotation;

        Hang();
    }

    private void OnEnable()
    {
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void Hang()
    {
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        lastReleaseTime = Time.time;
        StartCoroutine(EnablePhysicsNextFrame());
    }

    private IEnumerator EnablePhysicsNextFrame()
    {
        yield return null; // deja que el Grab termine de restaurar el Rigidbody

        // Si un socket la tomó, él controla el Rigidbody
        if (grab.isSelected) yield break;

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (grab.isSelected) return;                          // en mano o en socket
        if (Time.time - lastReleaseTime < graceTime) return;  // recién soltada
        if (rb.isKinematic) return;                           // colgada, no aplica
        if (IsSafe(collision.collider.transform)) return;

        ReturnHome();
    }

    private bool IsSafe(Transform hit)
    {
        if (safeRoots == null) return false;
        foreach (var root in safeRoots)
            if (root != null && hit.IsChildOf(root)) return true;
        return false;
    }

    public void ReturnHome()
    {
        rb.velocity = Vector3.zero;      // en Unity 2022 o anterior: rb.velocity
        rb.angularVelocity = Vector3.zero;
        Hang();
        transform.SetPositionAndRotation(homePos, homeRot);
    }
}