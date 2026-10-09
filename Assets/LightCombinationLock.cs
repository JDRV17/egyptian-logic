using System.Collections.Generic;
using UnityEngine;

public class LightCombinationLock : MonoBehaviour
{
    [System.Serializable]
    public struct SwitchCondition
    {
        [Tooltip("Referencia al interruptor.")]
        public XRSwitchToggle switchToggle;

        [Tooltip("El estado necesario para este interruptor (true = encendido, false = apagado).")]
        public bool requiredState;
    }

    [Header("Configuración de Interruptores (Exactamente 4)")]
    [Tooltip("Lista con los 4 switches y el estado esperado para cada uno.")]
    [SerializeField] private List<SwitchCondition> switches = new List<SwitchCondition>();

    [Header("Componentes de la Bombilla")]
    [Tooltip("Luz física de la bombilla (Point Light).")]
    [SerializeField] private Light bulbLight;

    [Tooltip("Renderer del objeto bombilla que contiene el material con Emisión.")]
    [SerializeField] private Renderer bulbRenderer;

    [Tooltip("Índice del material en el MeshRenderer que tiene la emisión (por defecto 0).")]
    [SerializeField] private int materialIndex = 0;

    [Header("Propiedades del Material de Emisión")]
    [Tooltip("Nombre de la propiedad de color de emisión del Shader (por defecto '_EmissionColor').")]
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

    [ColorUsage(false, true)]
    [Tooltip("Color de emisión cuando la bombilla está ENCENDIDA (HDR).")]
    [SerializeField] private Color onEmissionColor = Color.yellow * 2f;

    [ColorUsage(false, true)]
    [Tooltip("Color de emisión cuando la bombilla está APAGADA (HDR).")]
    [SerializeField] private Color offEmissionColor = Color.black;

    private Material targetMaterial;

    private void Awake()
    {
        // Obtener la instancia del material para modificar sus propiedades dinámicamente
        if (bulbRenderer != null && materialIndex < bulbRenderer.materials.Length)
        {
            targetMaterial = bulbRenderer.materials[materialIndex];
        }
    }

    private void OnEnable()
    {
        // Suscribirse al evento de cada switch para detectar cambios
        foreach (var condition in switches)
        {
            if (condition.switchToggle != null)
            {
                condition.switchToggle.onStateChanged.AddListener(OnSwitchStateChanged);
            }
        }
    }

    private void OnDisable()
    {
        // Desuscribirse al desactivar el objeto
        foreach (var condition in switches)
        {
            if (condition.switchToggle != null)
            {
                condition.switchToggle.onStateChanged.RemoveListener(OnSwitchStateChanged);
            }
        }
    }

    private void Start()
    {
        // Comprobar la combinación inicial al arrancar el nivel
        EvaluateCombination();
    }

    private void OnSwitchStateChanged(bool isOn)
    {
        EvaluateCombination();
    }

    /// <summary>
    /// Revisa si todos los switches cumplen con la condición requerida.
    /// </summary>
    public void EvaluateCombination()
    {
        bool isCombinationCorrect = true;

        foreach (var condition in switches)
        {
            if (condition.switchToggle == null)
            {
                isCombinationCorrect = false;
                break;
            }

            // Si el estado actual del switch no coincide con el estado requerido, la combinación es incorrecta
            if (condition.switchToggle.IsOn != condition.requiredState)
            {
                isCombinationCorrect = false;
                break;
            }
        }

        SetLightState(isCombinationCorrect);
    }

    /// <summary>
    /// Enciende o apaga la luz y el material de emisión.
    /// </summary>
    private void SetLightState(bool turnOn)
    {
        // 1. Controlar el Point Light
        if (bulbLight != null)
        {
            bulbLight.enabled = turnOn;
        }

        // 2. Controlar la Emisión del Material
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