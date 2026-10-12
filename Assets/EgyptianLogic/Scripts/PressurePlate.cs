using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    [Header("Eventos")]
    [Tooltip("Se dispara una sola vez cuando el jugador pisa la baldosa.")]
    public UnityEvent onPressed;

    [Header("Configuración del Pergamino")]
    public GameObject scrollObject;
    public Rigidbody scrollRigidbody;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (scrollObject != null && scrollRigidbody != null)
        {
            scrollObject.SetActive(true);
            scrollRigidbody.isKinematic = false;
            scrollRigidbody.useGravity = true;
        }

        Debug.Log("¡Baldosa pisada!");
        onPressed?.Invoke();
    }
}