using UnityEngine;

/// <summary>
/// Un cubo que muestra un bit. Sus caras alrededor del eje de giro deben alternar 0, 1, 0, 1
/// (como los cubos de 1.3, cambiando los textos). Cada vez que el bit cambia, gira 90°.
/// Empieza mostrando 0 en la posición en que está en la escena.
/// </summary>
public class BitCube : MonoBehaviour
{
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [Tooltip("Grados por segundo del giro.")]
    [SerializeField] private float speed = 360f;
    [Tooltip("Opcional: suena al girar.")]
    [SerializeField] private AudioSource rotateSound;

    private Quaternion baseRotation;
    private float angle, targetAngle;
    private int bit;
    private bool initialized;

    public int Bit => bit;

    private void Awake() => Init();

    private void Init()
    {
        if (initialized) return;
        initialized = true;
        baseRotation = transform.localRotation;
    }

    public void SetBit(int value)
    {
        Init();
        value = value != 0 ? 1 : 0;
        if (value == bit) return;
        bit = value;
        targetAngle += 90f;
        if (rotateSound != null) rotateSound.Play();
    }

    /// <summary>Vuelve a 0 sin animación (al cambiar de versión).</summary>
    public void ResetToZero()
    {
        Init();
        bit = 0;
        angle = targetAngle = 0f;
        transform.localRotation = baseRotation;
    }

    private void Update()
    {
        if (angle == targetAngle) return;
        angle = Mathf.MoveTowards(angle, targetAngle, speed * Time.deltaTime);
        transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, rotationAxis);
    }
}
