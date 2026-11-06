using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Animaciones pequeñas y compartidas del Nivel 3: piedras que suben o se hunden, piezas que se retraen,
/// textos que se desvanecen y paneles que giran. Todas son corrutinas: el que las usa las inicia con StartCoroutine.
/// </summary>
public static class Motion
{
    /// <summary>Curva suave (entra y sale despacio).</summary>
    public static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    /// <summary>
    /// "Altura" de una pieza apoyada en el suelo: k = 1 es su tamaño normal, k ≈ 0 queda aplastada contra el suelo.
    /// La base no se mueve, así que parece que sube desde el suelo o se hunde en él.
    /// </summary>
    public static void SetRise(Transform t, Vector3 restPos, Vector3 restScale, float bottomY, float k)
    {
        k = Mathf.Max(0.001f, k);
        t.localScale = new Vector3(restScale.x, restScale.y * k, restScale.z);
        t.localPosition = new Vector3(restPos.x, bottomY + (restPos.y - bottomY) * k, restPos.z);
    }

    /// <summary>Mueve una pieza (en coordenadas locales) de un punto a otro.</summary>
    public static IEnumerator Slide(Transform t, Vector3 to, float duration)
    {
        Vector3 from = t.localPosition;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            t.localPosition = Vector3.Lerp(from, to, Ease(e / duration));
            yield return null;
        }
        t.localPosition = to;
    }

    public static void SetAlpha(TMP_Text text, float a)
    {
        var c = text.color;
        c.a = a;
        text.color = c;
    }

    public static IEnumerator Fade(TMP_Text text, float to, float duration)
    {
        float from = text.color.a;
        for (float e = 0f; e < duration; e += Time.deltaTime)
        {
            SetAlpha(text, Mathf.Lerp(from, to, e / duration));
            yield return null;
        }
        SetAlpha(text, to);
    }

    /// <summary>Desvanece el texto, lo cambia y lo vuelve a mostrar. Si ya decía eso, no hace nada.</summary>
    public static IEnumerator SwapText(TMP_Text text, string value, float duration)
    {
        if (text.text == value && text.color.a >= 0.99f) yield break;
        if (text.color.a > 0.01f) yield return Run(Fade(text, 0f, duration * 0.5f));
        text.text = value;
        yield return Run(Fade(text, 1f, duration * 0.5f));
    }

    /// <summary>
    /// Panel giratorio: gira las piezas un cuarto de vuelta alrededor de "axis" (en el mundo), por "center",
    /// llama a "atEdge" cuando quedan de canto (ahí se cambia lo que muestran), salta al otro lado del canto
    /// y termina el giro en el mismo sentido. Como de canto no se ve la cara, parece una vuelta completa.
    /// </summary>
    public static IEnumerator SpinSwap(Transform[] parts, Vector3 center, Vector3 axis, float duration, Action atEdge)
    {
        float half = duration * 0.5f, done = 0f;
        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            float target = 90f * Ease(e / half);
            Rotate(parts, center, axis, target - done);
            done = target;
            yield return null;
        }
        Rotate(parts, center, axis, 90f - done);
        atEdge?.Invoke();
        Rotate(parts, center, axis, -180f);   // de canto por el otro lado: el salto no se nota
        done = -90f;
        for (float e = 0f; e < half; e += Time.deltaTime)
        {
            float target = -90f + 90f * Ease(e / half);
            Rotate(parts, center, axis, target - done);
            done = target;
            yield return null;
        }
        Rotate(parts, center, axis, -done);
    }

    private static void Rotate(Transform[] parts, Vector3 center, Vector3 axis, float degrees)
    {
        foreach (var p in parts) if (p != null) p.RotateAround(center, axis, degrees);
    }

    /// <summary>Ejecuta una corrutina anidada sin necesitar un MonoBehaviour (avanza en los mismos frames).</summary>
    public static IEnumerator Run(IEnumerator inner)
    {
        while (inner.MoveNext()) yield return inner.Current;
    }
}
