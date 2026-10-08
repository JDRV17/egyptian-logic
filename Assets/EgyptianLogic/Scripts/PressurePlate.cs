using System.Collections;
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Configuración de las Puertas")]
    [Tooltip("Arrastra aquí la o las puertas tipo garaje")]
    public Transform[] doors;

    [Header("Ajustes de Movimiento de Puertas")]
    [Tooltip("Distancia en el eje Y que bajará la puerta al cerrarse")]
    public float dropDistance = 4.0f;

    [Tooltip("Velocidad a la que bajan las puertas")]
    public float speed = 3.0f;

    [Header("Configuración del Pergamino")]
    [Tooltip("Arrastra aquí el GameObject del pergamino (puede estar desactivado inicialmente)")]
    public GameObject scrollObject;
    [Tooltip("El Rigidbody del pergamino")]
    public Rigidbody scrollRigidbody;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!triggered && other.CompareTag("Player"))
        {
            triggered = true;

            // 1. Iniciar bajada de puertas
            foreach (Transform door in doors)
            {
                if (door != null)
                {
                    StartCoroutine(LowerDoor(door));
                }
            }

            // 2. Soltar el pergamino activando su física
            if (scrollObject != null && scrollRigidbody != null)
            {
                scrollObject.SetActive(true); // Activa el pergamino en la escena
                scrollRigidbody.isKinematic = false; // Libera la física para que caiga
                scrollRigidbody.useGravity = true;
            }

            Debug.Log("¡Baldosa pisada! Puertas cerrándose y pergamino cayendo.");
        }
    }

    private IEnumerator LowerDoor(Transform door)
    {
        Vector3 startPosition = door.position;
        Vector3 targetPosition = new Vector3(startPosition.x, startPosition.y - dropDistance, startPosition.z);

        while (Vector3.Distance(door.position, targetPosition) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        door.position = targetPosition;
    }
}