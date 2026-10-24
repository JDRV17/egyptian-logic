using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Enchufe de cable. Nace en una señal (entrada o salida de compuerta) y el jugador lo agarra y lo
/// encaja en un WirePort. El cable se dibuja con un LineRenderer y se colorea con el valor en vivo.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class WirePlug : MonoBehaviour
{
    [Tooltip("Señal que transporta este cable.")]
    public SignalNode source;

    [Header("Cable")]
    [Tooltip("Vacío = se crea uno automáticamente.")]
    public LineRenderer line;
    public Material lineMaterial;
    [SerializeField] private float lineWidth = 0.006f;
    [SerializeField] private int segments = 14;
    [Tooltip("Cuánto cuelga el cable, en proporción a su largo.")]
    [SerializeField] private float sag = 0.12f;
    [Tooltip("Oculta el cable mientras el enchufe está en su lugar de origen.")]
    [SerializeField] private bool hideWhenHome = true;
    [SerializeField] private float homeRadius = 0.03f;

    [Header("Colores")]
    public Color highColor = new Color(1f, 0.8f, 0.2f);
    public Color lowColor = new Color(0.25f, 0.45f, 1f);
    public Color undefinedColor = new Color(0.4f, 0.4f, 0.4f);
    [Tooltip("Punta del enchufe (opcional), toma el mismo color del cable.")]
    public Renderer tip;

    public WirePort CurrentPort { get; set; }

    private XRGrabInteractable grab;
    private Transform homeParent;
    private Vector3 homeLocalPos;
    private Color lastColor = Color.clear;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        homeParent = transform.parent;
        homeLocalPos = transform.localPosition;

        if (line == null)
        {
            var go = new GameObject("Cable");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.widthMultiplier = lineWidth;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
        }

        line.positionCount = segments + 1;
    }

    private bool AtHome()
    {
        if (CurrentPort != null || grab.isSelected) return false;
        Vector3 home = homeParent != null ? homeParent.TransformPoint(homeLocalPos) : homeLocalPos;
        return (transform.position - home).sqrMagnitude < homeRadius * homeRadius;
    }

    private void LateUpdate()
    {
        if (source == null || line == null) return;

        bool show = !(hideWhenHome && AtHome());
        if (line.enabled != show) line.enabled = show;

        Color c = source.LiveValid ? (source.LiveValue ? highColor : lowColor) : undefinedColor;
        if (c != lastColor)
        {
            lastColor = c;
            line.startColor = c;
            line.endColor = c;
            Level2Visuals.SetColor(tip, c, source.LiveValid && source.LiveValue, 0.8f);
        }

        if (!show) return;

        Vector3 a = source.Anchor.position;
        Vector3 b = transform.position;
        float drop = Vector3.Distance(a, b) * sag;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y -= drop * 4f * t * (1f - t);
            line.SetPosition(i, p);
        }
    }
}
