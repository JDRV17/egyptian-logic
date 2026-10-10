using UnityEngine;

public class RevealPart : MonoBehaviour
{
    [Tooltip("Desplazamiento desde la posición final hasta la escondida. Piso: (0,-2,0). Pared: el eje por el que sale, ej. (0,0,-2).")]
    public Vector3 hiddenOffset = new Vector3(0f, -2f, 0f);

    [Tooltip("Mundo: (0,-2,0) siempre es hacia abajo. Local: usa los ejes del objeto.")]
    public bool offsetInWorldSpace = true;

    [Tooltip("Retraso antes de empezar a moverse (segundos). Útil para escalonar las piezas.")]
    public float delay = 0f;

    [HideInInspector] public Vector3 finalPosition;   // posición en mundo
    [HideInInspector] public Vector3 hiddenPosition;  // posición en mundo

    public void Capture()
    {
        finalPosition = transform.position;
        Vector3 worldOffset = offsetInWorldSpace ? hiddenOffset : transform.TransformVector(hiddenOffset);
        hiddenPosition = finalPosition + worldOffset;
    }

    public void Hide() => transform.position = hiddenPosition;

    public void SetProgress(float k) =>
        transform.position = Vector3.LerpUnclamped(hiddenPosition, finalPosition, k);
}