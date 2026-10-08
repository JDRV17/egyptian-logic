using System.Collections;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Configuración de las Puertas")]
    [Tooltip("Arrastra aquí la o las puertas tipo garaje")]
    public Transform[] doors;

    [Header("Ajustes de Movimiento")]
    [Tooltip("Distancia en el eje Y que bajará la puerta al cerrarse (en metros/unidades de Unity)")]
    public float dropDistance = 4.0f;

    [Tooltip("Velocidad a la que bajan las puertas")]
    public float speed = 3.0f;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Verifica que sea el jugador y que la baldosa no se haya activado antes
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            // Inicia el movimiento para cada puerta asignada
            foreach (Transform door in doors)
            {
                if (door != null)
                {
                    StartCoroutine(LowerDoor(door));
                }
            }

            Debug.Log("¡Baldosa pisada! Cerrando puertas de garaje.");
        }
    }

    private IEnumerator LowerDoor(Transform door)
    {
        // Posición inicial de la puerta (abierta)
        Vector3 startPosition = door.position;
        // Posición final (bajando solo en Y)
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y - dropDistance, startPosition.z);

        // Mueve la puerta frame por frame hasta alcanzar la posición destino
        while (Vector3.Distance(door.position, targetPosition) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, targetPosition, speed * Time.deltaTime);
            yield return null; // Espera al siguiente frame
        }

        // Asegura la posición exacta final
        door.position = targetPosition;
    }
}