using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class SocketLogicEvaluator : MonoBehaviour
{
    public enum SourceType { Switch, GateOutput }

    [System.Serializable]
    public struct InputSource
    {
        public SourceType type;
        [Tooltip("Si es Switch: índice 0-3. Si es GateOutput: índice del nodo en la lista Nodes.")]
        public int index;

        public static InputSource Sw(int i) => new InputSource { type = SourceType.Switch, index = i };
        public static InputSource Gate(int i) => new InputSource { type = SourceType.GateOutput, index = i };
    }

    [System.Serializable]
    public class GateNode
    {
        public string label = "Compuerta";
        public XRSocketInteractor socket;
        public InputSource inputA;
        [Tooltip("Ignorada si la compuerta es NOT.")]
        public InputSource inputB;

        [System.NonSerialized] public LogicGateItem currentGate;
        [System.NonSerialized] public UnityAction<SelectEnterEventArgs> onEnter;
        [System.NonSerialized] public UnityAction<SelectExitEventArgs> onExit;
    }

    [Header("Entradas del jugador (4 switches, en orden)")]
    [SerializeField] private XRSwitchToggle[] inputSwitches = new XRSwitchToggle[4];

    [Header("Estructura automática del circuito")]
    [SerializeField] private bool useAutomaticTopology = true;

    [Tooltip("La prueba (LightCombinationLock): de aquí se lee la versión activa.")]
    [SerializeField] private LightCombinationLock versionSource;

    [Tooltip("V1: [AND]")]
    [SerializeField] private XRSocketInteractor[] socketsV1;
    [Tooltip("V2: [XOR, AND]")]
    [SerializeField] private XRSocketInteractor[] socketsV2;
    [Tooltip("V3: [XOR, NOT, NOR, AND]")]
    [SerializeField] private XRSocketInteractor[] socketsV3;

    [Header("Estructura manual (solo si Use Automatic Topology está apagado)")]
    [SerializeField] private List<GateNode> nodes = new List<GateNode>();
    [SerializeField] private int outputNodeIndex = 0;

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

        if (useAutomaticTopology)
            BuildTopology();
    }

    /// <summary>Arma la lista de nodos según la versión. Los sockets deben venir en el orden del flujo de la señal.</summary>
    private void BuildTopology()
    {
        if (versionSource == null)
        {
            Debug.LogError($"[SocketLogicEvaluator] {name}: falta asignar Version Source.");
            return;
        }

        var version = versionSource.Version;

        XRSocketInteractor[] sockets;
        int required;
        switch (version)
        {
            case LightCombinationLock.LevelVersion.V1: sockets = socketsV1; required = 1; break;
            case LightCombinationLock.LevelVersion.V2: sockets = socketsV2; required = 2; break;
            default: sockets = socketsV3; required = 4; break;
        }

        if (sockets == null || sockets.Length < required)
        {
            Debug.LogError($"[SocketLogicEvaluator] {name}: la versión {version} necesita {required} sockets y hay {(sockets == null ? 0 : sockets.Length)}.");
            return;
        }

        nodes.Clear();

        switch (version)
        {
            case LightCombinationLock.LevelVersion.V1:
                AddNode("Compuerta 1", sockets[0], InputSource.Sw(0), InputSource.Sw(1));
                outputNodeIndex = 0;
                break;

            case LightCombinationLock.LevelVersion.V2:
                AddNode("Compuerta 1", sockets[0], InputSource.Sw(0), InputSource.Sw(1));
                AddNode("Compuerta 2", sockets[1], InputSource.Gate(0), InputSource.Sw(2));
                outputNodeIndex = 1;
                break;

            case LightCombinationLock.LevelVersion.V3:
                AddNode("Compuerta 1 (XOR)", sockets[0], InputSource.Sw(0), InputSource.Sw(1));
                AddNode("Compuerta 2 (NOT)", sockets[1], InputSource.Sw(2), InputSource.Sw(2));
                AddNode("Compuerta 3 (NOR)", sockets[2], InputSource.Gate(0), InputSource.Gate(1));
                AddNode("Compuerta 4 (AND)", sockets[3], InputSource.Gate(2), InputSource.Sw(3));
                outputNodeIndex = 3;
                break;
        }

        Debug.Log($"[SocketLogicEvaluator] {name}: topología construida para {version} con {nodes.Count} nodo(s).");
    }

    private void AddNode(string label, XRSocketInteractor socket, InputSource a, InputSource b)
    {
        nodes.Add(new GateNode { label = label, socket = socket, inputA = a, inputB = b });
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

        foreach (var s in inputSwitches)
            if (s != null) s.onStateChanged.AddListener(OnInputChanged);
    }

    private void OnDisable()
    {
        foreach (var node in nodes)
        {
            if (node.socket == null) continue;
            if (node.onEnter != null) node.socket.selectEntered.RemoveListener(node.onEnter);
            if (node.onExit != null) node.socket.selectExited.RemoveListener(node.onExit);
        }

        foreach (var s in inputSwitches)
            if (s != null) s.onStateChanged.RemoveListener(OnInputChanged);
    }

    private void Start() => EvaluateCircuit();

    private void OnInputChanged(bool state) => EvaluateCircuit();

    public void EvaluateCircuit()
    {
        bool[] current = new bool[4];
        for (int i = 0; i < 4 && i < inputSwitches.Length; i++)
            current[i] = inputSwitches[i] != null && inputSwitches[i].IsOn;

        SetLightState(EvaluateOutputForInputs(current));
    }

    public bool EvaluateOutputForInputs(bool[] inputs)
    {
        return TryEvaluateNode(outputNodeIndex, inputs, 0, out bool result) && result;
    }

    private bool TryEvaluateNode(int nodeIndex, bool[] inputs, int depth, out bool result)
    {
        result = false;

        if (nodeIndex < 0 || nodeIndex >= nodes.Count || depth > nodes.Count) return false;

        var node = nodes[nodeIndex];
        if (node.currentGate == null) return false;

        if (!TryResolve(node.inputA, inputs, depth, out bool a)) return false;

        bool b = false;
        if (node.currentGate.GateType != LogicGateType.NOT)
        {
            if (!TryResolve(node.inputB, inputs, depth, out b)) return false;
        }

        result = Compute(node.currentGate.GateType, a, b);
        return true;
    }

    private bool TryResolve(InputSource src, bool[] inputs, int depth, out bool value)
    {
        value = false;

        if (src.type == SourceType.Switch)
        {
            if (inputs == null || src.index < 0 || src.index >= inputs.Length) return false;
            value = inputs[src.index];
            return true;
        }

        return TryEvaluateNode(src.index, inputs, depth + 1, out value);
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
}