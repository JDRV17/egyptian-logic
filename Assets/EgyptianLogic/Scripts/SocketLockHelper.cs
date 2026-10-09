using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketLockHelper : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías se buscan solas)")]
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Rigidbody objectRigidbody;
    [SerializeField] private XRSwitchToggle switchToggle;

    [Header("Padre Fijo")]
    [Tooltip("La base fija. Opcional.")]
    [SerializeField] private Transform staticParent;

    [Header("Ajustes de Tiempo")]
    [SerializeField] private float socketSnapDelay = 0.25f;

    private bool locked;

    private void Awake()
    {
        if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();
        if (objectRigidbody == null) objectRigidbody = GetComponent<Rigidbody>();
        if (switchToggle == null) switchToggle = GetComponent<XRSwitchToggle>();
    }

    /// <summary>Conéctalo al evento Select Entered del XR Socket Interactor.</summary>
    public void LockInSocket()
    {
        if (locked) return;
        locked = true;
        Debug.Log($"[SocketLockHelper] {name}: LockInSocket llamado.");
        StartCoroutine(LockRoutine());
    }

    private IEnumerator LockRoutine()
    {
        // 1. Esperar a que el socket termine de atraer la palanca
        yield return new WaitForSeconds(socketSnapDelay);

        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // 2. Soltar del socket y congelar de inmediato
        if (grabInteractable != null) grabInteractable.enabled = false;
        Freeze();

        // 3. Un frame para que el Drop del Grab termine
        //    (puede restaurar el parent original y el Rigidbody)
        yield return null;

        // 4. Re-emparentar conservando la pose y volver a congelar
        if (staticParent != null) transform.SetParent(staticParent, true);
        transform.SetPositionAndRotation(pos, rot);
        Freeze();

        // 5. Interactable nuevo para el gatillo (se crea si no existe)
        var simple = GetComponent<XRSimpleInteractable>();
        if (simple == null) simple = gameObject.AddComponent<XRSimpleInteractable>();
        simple.enabled = true;

        // 6. Activar el switch
        if (switchToggle != null) switchToggle.EnableToggle();
        else Debug.LogError($"[SocketLockHelper] {name}: no hay XRSwitchToggle en la palanca.");

        Debug.Log($"[SocketLockHelper] {name}: palanca bloqueada y switch habilitado.");
    }

    private void Freeze()
    {
        if (objectRigidbody == null) return;
        objectRigidbody.isKinematic = true;
        objectRigidbody.useGravity = false;
        objectRigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }
}