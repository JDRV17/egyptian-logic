using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Protoboard: junta entradas, ranuras de compuertas, cables y salidas. Recalcula en vivo cuando
/// algo cambia (switch, compuerta o cable) y permite evaluar cualquier combinación de entradas
/// sin tocar la escena (para comparar contra la tabla de verdad).
/// </summary>
public class Breadboard : MonoBehaviour
{
    [Tooltip("En orden: el primero es el bit más significativo de la tabla. Vacío = los hijos.")]
    public CircuitInput[] inputs = new CircuitInput[0];
    [Tooltip("Vacío = los hijos.")]
    public CircuitOutput[] outputs = new CircuitOutput[0];

    [Tooltip("Se dispara después de recalcular el circuito en vivo.")]
    public UnityEvent onCircuitChanged = new UnityEvent();

    private GateSlot[] slots;
    private WirePort[] ports;
    private SignalNode[] nodes;
    private readonly List<CircuitInput> variables = new List<CircuitInput>();
    private readonly Dictionary<CircuitInput, bool> overrides = new Dictionary<CircuitInput, bool>();
    private readonly CircuitContext liveContext = new CircuitContext();
    private bool dirty = true;

    public IReadOnlyList<CircuitInput> VariableInputs => variables;
    public GateSlot[] Slots => slots;

    private void Awake()
    {
        if (inputs == null || inputs.Length == 0) inputs = GetComponentsInChildren<CircuitInput>(true);
        if (outputs == null || outputs.Length == 0) outputs = GetComponentsInChildren<CircuitOutput>(true);
        slots = GetComponentsInChildren<GateSlot>(true);
        ports = GetComponentsInChildren<WirePort>(true);
        nodes = GetComponentsInChildren<SignalNode>(true);

        foreach (var i in inputs)
            if (i != null && !i.isConstant) variables.Add(i);
    }

    private void OnEnable()
    {
        foreach (var i in inputs)
            if (i != null && i.sourceSwitch != null) i.sourceSwitch.onStateChanged.AddListener(OnSwitch);
        foreach (var s in slots) s.onChanged.AddListener(MarkDirty);
        foreach (var p in ports) p.onChanged.AddListener(MarkDirty);
        dirty = true;
    }

    private void OnDisable()
    {
        foreach (var i in inputs)
            if (i != null && i.sourceSwitch != null) i.sourceSwitch.onStateChanged.RemoveListener(OnSwitch);
        foreach (var s in slots) s.onChanged.RemoveListener(MarkDirty);
        foreach (var p in ports) p.onChanged.RemoveListener(MarkDirty);
    }

    private void OnSwitch(bool _) => MarkDirty();

    public void MarkDirty() => dirty = true;

    private void LateUpdate()
    {
        if (!dirty) return;
        dirty = false;
        RefreshLive();
    }

    public void RefreshLive()
    {
        foreach (var n in nodes)
        {
            n.LiveValid = n.TryGetValue(liveContext, out bool v);
            n.LiveValue = v;
        }
        foreach (var i in inputs) if (i != null) i.RefreshIndicator();
        foreach (var o in outputs) if (o != null) o.RefreshLive(liveContext);

        onCircuitChanged.Invoke();
    }

    /// <summary>Valor actual (switches reales) de una entrada por nombre.</summary>
    public bool TryGetLiveInput(string name, out bool value)
    {
        foreach (var i in inputs)
            if (i != null && i.displayName == name) { value = i.CurrentValue; return true; }
        value = false;
        return false;
    }

    public CircuitOutput FindOutput(string name)
    {
        foreach (var o in outputs) if (o != null && o.displayName == name) return o;
        return null;
    }

    /// <summary>
    /// Evalúa el circuito con los valores de <paramref name="inputValues"/> (por nombre).
    /// Devuelve false si alguna salida no está definida (circuito incompleto o con bucle).
    /// </summary>
    public bool TryEvaluate(IDictionary<string, bool> inputValues, IDictionary<string, bool> outputValues)
    {
        overrides.Clear();
        foreach (var i in variables)
            if (inputValues.TryGetValue(i.displayName, out bool v)) overrides[i] = v;

        var ctx = new CircuitContext { Overrides = overrides };
        bool complete = true;
        foreach (var o in outputs)
        {
            if (o == null) continue;
            if (o.TryGetValue(ctx, out bool v)) outputValues[o.displayName] = v;
            else complete = false;
        }
        return complete;
    }

    /// <summary>Compuertas encajadas en este momento.</summary>
    public IEnumerable<LogicGateItem> PlacedGates()
    {
        foreach (var s in slots)
            if (s.CurrentGate != null) yield return s.CurrentGate;
    }
}
