using UnityEngine;

/// <summary>
/// Pinta una pieza de un color fijo (los cuadros de la secuencia de 3.3) sin crear materiales nuevos.
/// Si el material tiene Emission activado, también brilla un poco en ese color.
/// </summary>
[ExecuteAlways]
public class ColorSwatch : MonoBehaviour
{
    [SerializeField] private Color color = Color.red;
    [Range(0f, 3f)] [SerializeField] private float glow = 0.6f;

    public Color Color => color;

    private void OnEnable() => Apply();
    private void OnValidate() => Apply();

    public void SetColor(Color c)
    {
        color = c;
        Apply();
    }

    private void Apply()
    {
        var r = GetComponent<Renderer>();
        if (r == null) return;
        var block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);   // URP
        block.SetColor("_Color", color);       // shaders clásicos
        block.SetColor("_EmissionColor", color * glow);
        r.SetPropertyBlock(block);
    }
}
