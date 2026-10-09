using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketLogicEvaluator : MonoBehaviour
{
    [Header("Entradas y Socket")]
    [Tooltip("El socket donde se inserta la compuerta lógica.")]
    [SerializeField] private XRSocketInteractor gateSocket;

    [Tooltip("Primer switch de entrada (A).")]
    [SerializeField] private XRSwitchToggle switchA;

    [Tooltip("Segundo switch de entrada (B).")]
    [SerializeField] private XRSwitchToggle switchB;

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
    private LogicGateItem currentGate;

    private void Awake()
    {
        if (bulbRenderer != null && materialIndex < bulbRenderer.materials.Length)
        {
            targetMaterial = bulbRenderer.materials[materialIndex];
        }
    }

    private void OnEnable()
    {
        // Escuchar cuando entra o sale un objeto del socket
        if (gateSocket != null)
        {
            gateSocket.selectEntered.AddListener(OnGateInserted);
            gateSocket.selectExited.AddListener(OnGateRemoved);
        }

        // Escuchar cambios en los switches de entrada
        if (switchA != null) switchA.onStateChanged.AddListener(OnInputChanged);
        if (switchB != null) switchB.onStateChanged.AddListener(OnInputChanged);
    }

    private void OnDisable()
    {
        if (gateSocket != null)
        {
            gateSocket.selectEntered.RemoveListener(OnGateInserted);
            gateSocket.selectExited.RemoveListener(OnGateRemoved);
        }

        if (switchA != null) switchA.onStateChanged.RemoveListener(OnInputChanged);
        if (switchB != null) switchB.onStateChanged.RemoveListener(OnInputChanged);
    }

    private void Start()
    {
        EvaluateCircuit();
    }

    private void OnGateInserted(SelectEnterEventArgs args)
    {
        // Intentar obtener el componente LogicGateItem de la compuerta insertada
        if (args.interactableObject.transform.TryGetComponent<LogicGateItem>(out var gate))
        {
            currentGate = gate;
        }
        else
        {
            currentGate = null;
        }

        EvaluateCircuit();
    }

    private void OnGateRemoved(SelectExitEventArgs args)
    {
        currentGate = null;
        EvaluateCircuit();
    }

    private void OnInputChanged(bool state)
    {
        EvaluateCircuit();
    }

    /// <summary>
    /// Revisa las entradas A y B junto con la compuerta en el socket para determinar la salida.
    /// </summary>
    public void EvaluateCircuit()
    {
        // Si no hay compuerta en el socket, el circuito está abierto -> Luz Apagada
        if (currentGate == null || switchA == null || switchB == null)
        {
            SetLightState(false);
            return;
        }

        bool a = switchA.IsOn;
        bool b = switchB.IsOn;
        bool result = false;

        // Evaluación de la tabla de verdad según el tipo de compuerta
        switch (currentGate.GateType)
        {
            case LogicGateType.AND:
                result = a && b;
                break;

            case LogicGateType.OR:
                result = a || b;
                break;

            case LogicGateType.XOR:
                result = a ^ b; // Operador XOR directo en C#
                break;

            case LogicGateType.NAND:
                result = !(a && b);
                break;

            case LogicGateType.NOR:
                result = !(a || b);
                break;

            case LogicGateType.XNOR:
                result = !(a ^ b);
                break;

            case LogicGateType.NOT:
                // No aplicable directamente a 2 entradas
                result = false;
                break;
        }

        SetLightState(result);
    }

    private void SetLightState(bool turnOn)
    {
        if (bulbLight != null)
        {
            bulbLight.enabled = turnOn;
        }

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

    /// <summary>
    /// Evalúa qué salida daría la compuerta insertada para dos entradas booleanas 'a' y 'b'.
    /// </summary>
    public bool EvaluateOutputForInputs(bool a, bool b)
    {
        if (currentGate == null) return false;

        switch (currentGate.GateType)
        {
            case LogicGateType.AND: return a && b;
            case LogicGateType.OR: return a || b;
            case LogicGateType.XOR: return a ^ b;
            case LogicGateType.NAND: return !(a && b);
            case LogicGateType.NOR: return !(a || b);
            case LogicGateType.XNOR: return !(a ^ b);
            default: return false;
        }
    }
}