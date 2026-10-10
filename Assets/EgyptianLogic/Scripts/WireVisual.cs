using System.Collections.Generic;
using UnityEngine;

public class WireVisual : MonoBehaviour
{
    [Tooltip("Renderers que forman este cable. Si se deja vacío, usa los de este objeto y sus hijos.")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

    [ColorUsage(false, true)]
    [SerializeField] private Color onEmissionColor = Color.cyan * 2f;
    [ColorUsage(false, true)]
    [SerializeField] private Color offEmissionColor = Color.black;

    private readonly List<Material> materials = new List<Material>();
    private bool initialized;

    private void Awake() => Init();

    private void Init()
    {
        if (initialized) return;
        initialized = true;

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mats = r.materials; // instancia los materiales para no afectar a otros objetos
            if (materialIndex < mats.Length) materials.Add(mats[materialIndex]);
        }
    }

    public void SetState(bool on)
    {
        Init();

        foreach (var m in materials)
        {
            if (m == null) continue;

            if (on)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor(emissionColorPropertyName, onEmissionColor);
            }
            else
            {
                m.SetColor(emissionColorPropertyName, offEmissionColor);
                m.DisableKeyword("_EMISSION");
            }
        }
    }
}