using UnityEngine;

public class LightCombinationLock : MonoBehaviour
{
    public enum LevelVersion { V1 = 0, V2 = 1, V3 = 2 }

    [Header("Versión del puzzle")]
    [SerializeField] private LevelVersion version = LevelVersion.V1;
    public LevelVersion Version => version;

    [Header("Switches de la prueba (exactamente 4, en orden)")]
    [SerializeField] private XRSwitchToggle[] switches = new XRSwitchToggle[4];

    [Header("Componentes de la Bombilla")]
    [SerializeField] private Light bulbLight;
    [SerializeField] private Renderer bulbRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

    [ColorUsage(false, true)]
    [SerializeField] private Color onEmissionColor = Color.yellow * 2f;

    [ColorUsage(false, true)]
    [SerializeField] private Color offEmissionColor = Color.black;

    private Material targetMaterial;

    private void Awake()
    {
        if (bulbRenderer != null && materialIndex < bulbRenderer.materials.Length)
            targetMaterial = bulbRenderer.materials[materialIndex];
    }

    private void OnEnable()
    {
        foreach (var s in switches)
            if (s != null) s.onStateChanged.AddListener(OnSwitchStateChanged);
    }

    private void OnDisable()
    {
        foreach (var s in switches)
            if (s != null) s.onStateChanged.RemoveListener(OnSwitchStateChanged);
    }

    private void Start() => EvaluateCombination();

    private void OnSwitchStateChanged(bool isOn) => EvaluateCombination();

    /// <summary>Cambia la versión del puzzle (para cuando la defina el desempeño del jugador).</summary>
    public void SetVersion(LevelVersion newVersion)
    {
        version = newVersion;
        EvaluateCombination();
    }

    /// <summary>Lógica de la prueba según la versión. inputs[0..3] = switches 1..4.</summary>
    public bool EvaluateOutputForInputs(bool[] s)
    {
        if (s == null || s.Length < 4) return false;

        switch (version)
        {
            case LevelVersion.V1:
                return s[0] && s[1];                    // S1 AND S2

            case LevelVersion.V2:
                return (s[0] ^ s[1]) && s[2];           // (S1 XOR S2) AND S3

            case LevelVersion.V3:
                return s[2] && s[3] && (s[0] == s[1]);  // S3 AND S4 AND (S1 XNOR S2)

            default:
                return false;
        }
    }

    public void EvaluateCombination()
    {
        bool[] current = new bool[4];
        for (int i = 0; i < 4 && i < switches.Length; i++)
            current[i] = switches[i] != null && switches[i].IsOn;

        SetLightState(EvaluateOutputForInputs(current));
    }

    private void SetLightState(bool turnOn)
    {
        if (bulbLight != null) bulbLight.enabled = turnOn;

        if (targetMaterial != null)
        {
            if (turnOn)
            {
                targetMaterial.EnableKeyword("_EMISSION");
                targetMaterial.SetColor(emissionColorPropertyName, onEmissionColor);
            }
            else
            {
                targetMaterial.SetColor(emissionColorPropertyName, offEmissionColor);
                targetMaterial.DisableKeyword("_EMISSION");
            }
        }
    }
}