using UnityEngine;
using UnityEngine.Events;

public class PressurePlate : MonoBehaviour
{
    [Header("Eventos")]
    [Tooltip("Se dispara una sola vez cuando el jugador pisa la baldosa.")]
    public UnityEvent onPressed;

    [Header("Pergamino de pista")]
    [SerializeField] private HintScrollSpawner spawner;
    [TextArea(2, 6)]
    [SerializeField] private string hintText = "Ve cerca de la puerta de entrada.";

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (spawner != null && !string.IsNullOrEmpty(hintText))
            spawner.Spawn(hintText, false);

        Debug.Log("¡Baldosa pisada!");
        onPressed?.Invoke();
    }
}