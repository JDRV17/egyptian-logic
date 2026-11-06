using UnityEngine;

/// <summary>Una bombilla que muestra un bit: encendida = 1.</summary>
public class BitBulb : MonoBehaviour
{
    [SerializeField] private Light bulbLight;
    [SerializeField] private Renderer bulbRenderer;
    [ColorUsage(false, true)] [SerializeField] private Color onEmission = new Color(1f, 0.85f, 0.4f) * 2f;
    [ColorUsage(false, true)] [SerializeField] private Color offEmission = Color.black;

    private MaterialPropertyBlock block;
    private bool on;
    private bool applied;

    public bool IsOn => on;

    private void Awake() => Apply(false, true);

    public void SetOn(bool value) => Apply(value, false);

    private void Apply(bool value, bool force)
    {
        if (applied && !force && value == on) return;
        applied = true;
        on = value;

        if (bulbLight != null) bulbLight.enabled = on;
        if (bulbRenderer != null)
        {
            if (block == null) block = new MaterialPropertyBlock();
            bulbRenderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", on ? onEmission : offEmission);
            bulbRenderer.SetPropertyBlock(block);
        }
    }
}
