using UnityEngine;

public class CharacterPushObjects : MonoBehaviour
{
    [Header("Configuración de Empuje")]
    [Tooltip("Fuerza con la que el personaje empujará los objetos")]
    public float pushPower = 2.0f;

    // Esta función se ejecuta automáticamente cuando el CharacterController choca con algo
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // 1. Si no hay Rigidbody o es cinemático, no hacemos nada
        if (body == null || body.isKinematic)
        {
            return;
        }

        // 2. Si estamos pisando el objeto desde arriba, no queremos empujarlo hacia abajo
        // hit.moveDirection.y < -0.3f significa que el personaje se mueve hacia abajo sobre el objeto
        if (hit.moveDirection.y < -0.3f)
        {
            return;
        }

        // 3. Calcular la dirección del empuje (solo en los ejes horizontales X y Z)
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // 4. Aplicar la fuerza al Rigidbody en el punto de contacto
        body.AddForceAtPosition(pushDir * pushPower, hit.point, ForceMode.Impulse);
    }
}
