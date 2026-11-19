using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

/// <summary>
/// Subtítulos que siguen la cámara activa (la del First Person en PC o la del XR Origin en VR), en la parte baja
/// de la vista. El texto largo se divide en frases que van cambiando al ritmo del audio. Con un material
/// "Distance Field Overlay" se dibuja encima de todo, así que no lo tapan las paredes.
/// El canvas se crea solo al iniciar: no hay que armar nada en la escena.
/// </summary>
public class SubtitleDisplay : MonoBehaviour
{
    [Header("Posición frente a la cámara")]
    [Tooltip("Distancia frente a los ojos, en metros.")]
    public float distance = 1.2f;
    [Tooltip("Cuánto más abajo del centro de la vista, en metros (negativo = abajo).")]
    public float verticalOffset = -0.38f;
    [Tooltip("Ancho máximo del texto, en metros.")]
    public float width = 1.1f;
    [Tooltip("0 = pegado a la cámara (como hijo). Mayor = sigue la cabeza con suavidad, más cómodo en VR.")]
    public float followSmoothing = 8f;

    [Header("Texto")]
    public TMP_FontAsset font;
    [Tooltip("Material con shader TextMeshPro/Distance Field Overlay (se dibuja encima de todo). Vacío = el de la fuente.")]
    public Material textMaterial;
    public float fontSize = 46f;
    public Color color = new Color(1f, 0.95f, 0.85f);
    [Tooltip("Máximo de caracteres por subtítulo antes de pasar al siguiente.")]
    public int maxChars = 110;

    private const float PixelsPerMeter = 1000f;
    private Transform root;
    private TextMeshProUGUI label;
    private Camera cam;
    private Coroutine running;

    private void Awake() => Build();

    private void Build()
    {
        var go = new GameObject("Subtitulos", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        root = go.transform;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 1000;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(width * PixelsPerMeter, 300f);
        rt.localScale = Vector3.one / PixelsPerMeter;

        var textGo = new GameObject("Texto", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)textGo.transform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;

        label = textGo.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        if (textMaterial != null) label.fontSharedMaterial = textMaterial;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.Bottom;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        label.text = "";
        go.SetActive(false);
    }

    /// <summary>Muestra el texto repartido en frases durante 'duration' segundos.</summary>
    public void Show(string text, float duration)
    {
        if (running != null) StopCoroutine(running);
        if (string.IsNullOrWhiteSpace(text) || label == null) { Hide(); return; }
        running = StartCoroutine(Run(Chunks(text, maxChars), Mathf.Max(duration, 1f)));
    }

    public void Hide()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        if (root != null) root.gameObject.SetActive(false);
    }

    private IEnumerator Run(List<string> chunks, float duration)
    {
        var times = Durations(chunks, duration);
        root.gameObject.SetActive(true);
        Follow(true);
        for (int i = 0; i < chunks.Count; i++)
        {
            label.text = chunks[i];
            yield return new WaitForSecondsRealtime(times[i]);
        }
        root.gameObject.SetActive(false);
        running = null;
    }

    private void LateUpdate()
    {
        if (root != null && root.gameObject.activeSelf) Follow(false);
    }

    private void Follow(bool snap)
    {
        if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
        if (cam == null) return;

        var t = cam.transform;
        Vector3 pos = t.position + t.forward * distance + t.up * verticalOffset;
        Quaternion rot = Quaternion.LookRotation(t.forward, t.up);

        if (snap || followSmoothing <= 0f)
        {
            root.SetPositionAndRotation(pos, rot);
            return;
        }
        float k = 1f - Mathf.Exp(-followSmoothing * Time.unscaledDeltaTime);
        root.SetPositionAndRotation(Vector3.Lerp(root.position, pos, k), Quaternion.Slerp(root.rotation, rot, k));
    }

    // ------------------------------------------------------------ División del texto (pública para pruebas)

    // Fin de frase: . ! ? … seguido de espacio y de una mayúscula (o ¿ ¡ "), así "Ah... un arqueólogo" no se corta.
    private static readonly Regex SentenceEnd = new Regex(@"(?<=[\.\!\?…])\s+(?=[A-ZÁÉÍÓÚÑ¿¡""])");

    /// <summary>
    /// Divide en frases y las junta mientras quepan en maxChars. Una frase que no cabe se corta en sus comas
    /// (o dos puntos), y solo si aún así no cabe, por palabras.
    /// </summary>
    public static List<string> Chunks(string text, int maxChars)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        maxChars = Mathf.Max(20, maxChars);
        text = Regex.Replace(text.Trim(), @"\s+", " ");

        var pieces = new List<string>();
        foreach (var sentence in SentenceEnd.Split(text))
        {
            if (sentence.Length <= maxChars) { pieces.Add(sentence); continue; }
            foreach (var clause in Regex.Split(sentence, @"(?<=[,;:])\s+"))
            {
                if (clause.Length <= maxChars) { pieces.Add(clause); continue; }
                string cur = "";
                foreach (var w in clause.Split(' '))
                {
                    if (cur.Length > 0 && cur.Length + 1 + w.Length > maxChars) { pieces.Add(cur); cur = ""; }
                    cur = cur.Length == 0 ? w : cur + " " + w;
                }
                if (cur.Length > 0) pieces.Add(cur);
            }
        }

        string current = "";
        foreach (var piece in pieces)
        {
            if (piece.Length == 0) continue;
            if (current.Length > 0 && current.Length + 1 + piece.Length <= maxChars) { current += " " + piece; continue; }
            if (current.Length > 0) result.Add(current);
            current = piece;
        }
        if (current.Length > 0) result.Add(current);
        return result;
    }

    /// <summary>Reparte la duración según el largo de cada parte (mínimo 1.2 s por parte).</summary>
    public static float[] Durations(List<string> chunks, float total)
    {
        var d = new float[chunks.Count];
        if (chunks.Count == 0) return d;
        float chars = 0f;
        foreach (var c in chunks) chars += c.Length;
        for (int i = 0; i < chunks.Count; i++) d[i] = Mathf.Max(1.2f, total * chunks[i].Length / Mathf.Max(1f, chars));
        return d;
    }
}
