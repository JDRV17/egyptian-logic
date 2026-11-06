using UnityEngine;

/// <summary>
/// Un grupo de piezas que brillan juntas (los números 0/1 y las flechas de 3.2).
/// El material debe tener Emission activado; empieza apagado.
/// </summary>
public class GlowGroup : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers = new Renderer[0];
    [ColorUsage(false, true)] [SerializeField] private Color onEmission = new Color(1f, 0.62f, 0.08f) * 2f;
    [ColorUsage(false, true)] [SerializeField] private Color offEmission = Color.black;

    private MaterialPropertyBlock block;
    private bool on;
    private bool applied;

    public bool IsOn => on;

    private void Awake()
    {
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>(true);
        Apply(false, true);
    }

    public void SetOn(bool value) => Apply(value, false);

    private void Apply(bool value, bool force)
    {
        if (applied && !force && value == on) return;
        applied = true;
        on = value;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", on ? onEmission : offEmission);
            r.SetPropertyBlock(block);
        }
    }
}
