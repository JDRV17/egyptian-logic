using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[DefaultExecutionOrder(10)] // después del PuzzleVersionManager (-100)
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
        public InputSource inputB;
        public WireVisual outputWire;

        [System.NonSerialized] public LogicGateItem currentGate;
        [System.NonSerialized] public UnityAction<SelectEnterEventArgs> onEnter;
        [System.NonSerialized] public UnityAction<SelectExitEventArgs> onExit;
    }

    [System.Serializable]
    public class OutputTerminal
    {
        public string label = "Salida";
        public int nodeIndex = 0;
        public Light bulbLight;
        public Renderer bulbRenderer;
        public int materialIndex = 0;

        [System.NonSerialized] public Material material;
        [System.NonSerialized] public bool isOn;
    }

    [System.Serializable]
    public class VersionConfig
    {
        public string name = "V1";
        [Tooltip("true = energizada. V1: {T,T}. V2: {F,T,T}. V3: {T,T,F,F,T}.")]
        public bool[] fixedInputs = { true, true };
        public WireVisual[] fixedInputWires;
        public List<GateNode> nodes = new List<GateNode>();
        public List<OutputTerminal> outputs = new List<OutputTerminal>();
    }

    [Header("Versión")]
    [Tooltip("Manager que escoge la versión. Si está vacío, se usa siempre la configuración 0.")]
    [SerializeField] private PuzzleVersionManager versionManager;

    [Tooltip("Posición 0 = V1, 1 = V2, 2 = V3.")]
    [SerializeField] private VersionConfig[] versions = new VersionConfig[3];

    [Header("Bombillas")]
    [SerializeField] private string emissionColorPropertyName = "_EmissionColor";
    [ColorUsage(false, true)]
    [SerializeField] private Color onEmissionColor = Color.yellow * 2f;
    [ColorUsage(false, true)]
    [SerializeField] private Color offEmissionColor = Color.black;

    [Header("Eventos")]
    public UnityEvent onSolved;
    public UnityEvent<bool> onSolvedChanged;

    public bool IsSolved { get; private set; }

    private VersionConfig active;
    private bool solvedFired;

    private void Awake() => ChooseConfig();

    private void ChooseConfig()
    {
        if (active != null) return;

        int index = versionManager != null ? (int)versionManager.CurrentVersion : 0;
        if (versions == null || index < 0 || index >= versions.Length || versions[index] == null)
        {
            Debug.LogError($"[FixedInputCircuit] {name}: no hay configuración para el índice {index}.");
            return;
        }

        active = versions[index];

        foreach (var o in active.outputs)
            if (o.bulbRenderer != null && o.materialIndex < o.bulbRenderer.materials.Length)
                o.material = o.bulbRenderer.materials[o.materialIndex];

        Debug.Log($"[FixedInputCircuit] {name}: usando configuración '{active.name}' ({active.nodes.Count} nodo(s), {active.outputs.Count} salida(s), {active.fixedInputs.Length} entrada(s)).");
    }

    private void OnEnable()
    {
        ChooseConfig();
        if (active == null) return;

        foreach (var node in active.nodes)
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
        if (active == null) return;

        foreach (var node in active.nodes)
        {
            if (node.socket == null) continue;
            if (node.onEnter != null) node.socket.selectEntered.RemoveListener(node.onEnter);
            if (node.onExit != null) node.socket.selectExited.RemoveListener(node.onExit);
        }
    }

    private void Start() => EvaluateCircuit();

    public void EvaluateCircuit()
    {
        if (active == null) return;

        bool allOn = active.outputs.Count > 0;

        foreach (var o in active.outputs)
        {
            o.isOn = TryEvaluateNode(o.nodeIndex, 0, out bool v) && v;
            SetBulb(o, o.isOn);
            if (!o.isOn) allOn = false;
        }

        UpdateWires();

        if (allOn != IsSolved)
        {
            IsSolved = allOn;
            onSolvedChanged?.Invoke(allOn);
            if (allOn && !solvedFired)
            {
                solvedFired = true;
                onSolved?.Invoke();
            }
        }
    }

    private void UpdateWires()
    {
        if (active.fixedInputWires != null)
        {
            for (int i = 0; i < active.fixedInputWires.Length; i++)
            {
                if (active.fixedInputWires[i] == null) continue;
                bool on = i < active.fixedInputs.Length && active.fixedInputs[i];
                active.fixedInputWires[i].SetState(on);
            }
        }

        for (int i = 0; i < active.nodes.Count; i++)
        {
            if (active.nodes[i].outputWire == null) continue;
            bool on = TryEvaluateNode(i, 0, out bool v) && v;
            active.nodes[i].outputWire.SetState(on);
        }
    }

    private bool TryEvaluateNode(int nodeIndex, int depth, out bool result)
    {
        result = false;
        var nodes = active.nodes;
        if (nodeIndex < 0 || nodeIndex >= nodes.Count || depth > nodes.Count) return false;

        var node = nodes[nodeIndex];
        if (node.currentGate == null) return false;

        if (!TryResolve(node.inputA, depth, out bool a)) return false;

        bool b = false;
        if (node.currentGate.GateType != LogicGateType.NOT)
            if (!TryResolve(node.inputB, depth, out b)) return false;

        result = Compute(node.currentGate.GateType, a, b);
        return true;
    }

    private bool TryResolve(InputSource src, int depth, out bool value)
    {
        value = false;

        if (src.type == SourceType.FixedInput)
        {
            if (src.index < 0 || src.index >= active.fixedInputs.Length) return false;
            value = active.fixedInputs[src.index];
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

    private void SetBulb(OutputTerminal o, bool turnOn)
    {
        if (o.bulbLight != null) o.bulbLight.enabled = turnOn;
        if (o.material == null) return;

        if (turnOn)
        {
            o.material.EnableKeyword("_EMISSION");
            o.material.SetColor(emissionColorPropertyName, onEmissionColor);
        }
        else
        {
            o.material.SetColor(emissionColorPropertyName, offEmissionColor);
            o.material.DisableKeyword("_EMISSION");
        }
    }
}