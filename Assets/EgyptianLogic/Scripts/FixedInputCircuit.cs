using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class FixedInputCircuit : MonoBehaviour
{
    public enum SourceType { FixedInput, GateOutput }

    [System.Serializable]
    public struct InputSource
    {
        public SourceType type;
        [Tooltip("FixedInput: índice en Fixed Inputs. GateOutput: índice del nodo en Nodes.")]
        public int index;
    }

    [System.Serializable]
    public class GateNode
    {
        public string label = "Compuerta";
        public XRSocketInteractor socket;
        public InputSource inputA;
        [Tooltip("Se ignora si la compuerta es NOT.")]
        public InputSource inputB;
        public WireVisual outputWire;   // cable que sale de esta compuerta

        [System.NonSerialized] public LogicGateItem currentGate;
        [System.NonSerialized] public UnityAction<SelectEnterEventArgs> onEnter;
        [System.NonSerialized] public UnityAction<SelectExitEventArgs> onExit;
    }

    [Header("Entradas con corriente fija")]
    [Tooltip("true = la entrada está energizada. V1: 2 entradas, ambas en true.")]
    [SerializeField] private bool[] fixedInputs = { true, true };

    [Tooltip("Un cable por cada entrada fija, en el mismo orden que Fixed Inputs.")]
    [SerializeField] private WireVisual[] fixedInputWires;

    [Header("Estructura del circuito")]
    [SerializeField] private List<GateNode> nodes = new List<GateNode>();
    [Tooltip("Nodo cuya salida enciende la bombilla.")]
    [SerializeField] private int outputNodeIndex = 0;

    [Header("Bombilla")]
    [SerializeField] private Light bulbLight;
    [SerializeField] private Renderer bulbRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

    [ColorUsage(false, true)]
    [SerializeField] private Color onEmissionColor = Color.yellow * 2f;
    [ColorUsage(false, true)]
    [SerializeField] private Color offEmissionColor = Color.black;

    [Header("Eventos")]
    public UnityEvent<bool> onOutputChanged;

    public bool IsOutputOn { get; private set; }

    private Material targetMaterial;

    private void Awake()
    {
        if (bulbRenderer != null && materialIndex < bulbRenderer.materials.Length)
            targetMaterial = bulbRenderer.materials[materialIndex];
    }

    private void OnEnable()
    {
        foreach (var node in nodes)
        {
            if (node.socket == null) continue;

            var n = node;
            n.onEnter = args =>
            {
                n.currentGate = args.interactableObject.transform.TryGetComponent<LogicGateItem>(out var g) ? g : null;
                EvaluateCircuit();
            };
            n.onExit = args =>
            {
                n.currentGate = null;
                EvaluateCircuit();
            };

            n.socket.selectEntered.AddListener(n.onEnter);
            n.socket.selectExited.AddListener(n.onExit);
        }
    }

    private void OnDisable()
    {
        foreach (var node in nodes)
        {
            if (node.socket == null) continue;
            if (node.onEnter != null) node.socket.selectEntered.RemoveListener(node.onEnter);
            if (node.onExit != null) node.socket.selectExited.RemoveListener(node.onExit);
        }
    }

    private void Start() => EvaluateCircuit();

    public void EvaluateCircuit()
    {
        bool result = TryEvaluateNode(outputNodeIndex, 0, out bool value) && value;

        bool changed = result != IsOutputOn;
        IsOutputOn = result;
        SetLightState(result);
        UpdateWires();

        if (changed) onOutputChanged?.Invoke(result);
    }

    private bool TryEvaluateNode(int nodeIndex, int depth, out bool result)
    {
        result = false;

        // depth > nodes.Count = ciclo mal configurado
        if (nodeIndex < 0 || nodeIndex >= nodes.Count || depth > nodes.Count) return false;

        var node = nodes[nodeIndex];
        if (node.currentGate == null) return false; // socket vacío = circuito abierto

        if (!TryResolve(node.inputA, depth, out bool a)) return false;

        bool b = false;
        if (node.currentGate.GateType != LogicGateType.NOT)
        {
            if (!TryResolve(node.inputB, depth, out b)) return false;
        }

        result = Compute(node.currentGate.GateType, a, b);
        return true;
    }

    private bool TryResolve(InputSource src, int depth, out bool value)
    {
        value = false;

        if (src.type == SourceType.FixedInput)
        {
            if (fixedInputs == null || src.index < 0 || src.index >= fixedInputs.Length) return false;
            value = fixedInputs[src.index];
            return true;
        }

        return TryEvaluateNode(src.index, depth + 1, out value);
    }

    private static bool Compute(LogicGateType type, bool a, bool b)
    {
        switch (type)
        {
            case LogicGateType.AND: return a && b;
            case LogicGateType.OR: return a || b;
            case LogicGateType.XOR: return a ^ b;
            case LogicGateType.NAND: return !(a && b);
            case LogicGateType.NOR: return !(a || b);
            case LogicGateType.XNOR: return !(a ^ b);
            case LogicGateType.NOT: return !a;
            default: return false;
        }
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

    private void UpdateWires()
    {
        // Cables de entrada: encendidos si su entrada fija está energizada
        if (fixedInputWires != null)
        {
            for (int i = 0; i < fixedInputWires.Length; i++)
            {
                if (fixedInputWires[i] == null) continue;
                bool on = fixedInputs != null && i < fixedInputs.Length && fixedInputs[i];
                fixedInputWires[i].SetState(on);
            }
        }

        // Cables de salida: encendidos solo si la compuerta existe y su resultado es true
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].outputWire == null) continue;
            bool on = TryEvaluateNode(i, 0, out bool v) && v;
            nodes[i].outputWire.SetState(on);
        }
    }
}