using System.Collections;
using UnityEngine;

/// <summary>
/// Antorcha (o brasero) que empieza apagada y se enciende con <see cref="Ignite"/>.
/// Level2Flow enciende una fila de ellas para marcar el camino al reto siguiente.
/// </summary>
public class Level2Torch : MonoBehaviour
{
    public Light flameLight;
    [Tooltip("Parte visible del fuego (se muestra al encender).")]
    public GameObject flame;
    [Tooltip("Sonido del fuego (en bucle).")]
    public AudioSource fireSound;
    [Tooltip("Sonido al prenderse (una vez).")]
    public AudioSource igniteSound;
    [Tooltip("Encendida desde el inicio (p. ej., las de la entrada).")]
    public bool litOnStart;

    public bool IsLit { get; private set; }

    private void Awake()
    {
        Set(litOnStart);
    }

    private void Set(bool on)
    {
        IsLit = on;
        if (flameLight != null) flameLight.enabled = on;
        if (flame != null) flame.SetActive(on);
        if (fireSound != null)
        {
            if (on && !fireSound.isPlaying) fireSound.Play();
            else if (!on) fireSound.Stop();
        }
    }

    public void Ignite()
    {
        if (IsLit) return;
        Set(true);
        if (igniteSound != null) igniteSound.Play();
        if (flame != null && isActiveAndEnabled) StartCoroutine(Flare());
    }

    // El fuego crece al prenderse
    private IEnumerator Flare()
    {
        var t = flame.transform;
        Vector3 size = t.localScale;
        for (float k = 0f; k < 1f; k += Time.deltaTime * 3f)
        {
            t.localScale = size * Mathf.SmoothStep(0.1f, 1f, k);
            yield return null;
        }
        t.localScale = size;
    }

    [ContextMenu("Encender (prueba)")]
    private void TestIgnite() => Ignite();
}
