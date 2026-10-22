using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Ranura del protoboard: un socket donde se encaja una compuerta (LogicGateItem), puertos de
/// entrada para los cables y pines de salida de los que nacen cables nuevos.
///
/// Uso de los puertos según la compuerta:
///   AND/OR/XOR/NAND/NOR/XNOR: todos los puertos conectados (mínimo 2) → Y
///   NOT: el primer puerto conectado → Y
///   MUX2: I0, I1, S → Y           MUX4: I0, I1, I2, I3, S0, S1 → Y
///   HALF_ADDER: A, B → S, C       FULL_ADDER: A, B, Cin → S, Cout
/// </summary>
public class GateSlot : MonoBehaviour
{
    public Level2Socket gateSocket;
    public WirePort[] inputPorts = new WirePort[2];
    public GateOutputPin[] outputPins = new GateOutputPin[1];
    [Tooltip("Cartel con el nombre de la compuerta encajada (opcional).")]
    public TextMeshPro label;

    public UnityEvent onChanged = new UnityEvent();

    public LogicGateItem CurrentGate { get; private set; }

    private void Awake()
    {
        if (gateSocket != null) gateSocket.accept = Level2Socket.Accept.LogicGate;
        foreach (var p in outputPins) if (p != null) p.slot = this;
    }

    private void OnEnable()
    {
        if (gateSocket == null) return;
        gateSocket.selectEntered.AddListener(OnGateEntered);
        gateSocket.selectExited.AddListener(OnGateExited);
        foreach (var p in inputPorts) if (p != null) p.onChanged.AddListener(Changed);
        UpdateLabels();
    }

    private void OnDisable()
    {
        if (gateSocket == null) return;
        gateSocket.selectEntered.RemoveListener(OnGateEntered);
        gateSocket.selectExited.RemoveListener(OnGateExited);
        foreach (var p in inputPorts) if (p != null) p.onChanged.RemoveListener(Changed);
    }

    private void OnGateEntered(SelectEnterEventArgs args)
    {
        CurrentGate = args.interactableObject.transform.GetComponent<LogicGateItem>();
        UpdateLabels();
        Changed();
    }

    private void OnGateExited(SelectExitEventArgs args)
    {
        CurrentGate = null;
        UpdateLabels();
        Changed();
    }

    private void Changed() => onChanged.Invoke();

    // ---------------------------------------------------------------- Evaluación

    public bool TryEvaluate(CircuitContext ctx, int outIndex, out bool value)
    {
        value = false;
        if (CurrentGate == null) return false;
        if (ctx == null) ctx = new CircuitContext();
        if (!ctx.Enter(this)) return false; // bucle: la señal depende de sí misma

        try { return Compute(ctx, CurrentGate.GateType, outIndex, out value); }
        finally { ctx.Exit(this); }
    }

    private bool In(CircuitContext ctx, int i, out bool v)
    {
        v = false;
        return i < inputPorts.Length && inputPorts[i] != null && inputPorts[i].TryGetValue(ctx, out v);
    }

    private bool Compute(CircuitContext ctx, LogicGateType type, int outIndex, out bool value)
    {
        value = false;
        switch (type)
        {
            case LogicGateType.MUX2:
            {
                if (outIndex != 0) return false;
                if (!In(ctx, 0, out bool i0) || !In(ctx, 1, out bool i1) || !In(ctx, 2, out bool s)) return false;
                value = s ? i1 : i0;
                return true;
            }

            case LogicGateType.MUX4:
            {
                if (outIndex != 0) return false;
                var d = new bool[4];
                for (int k = 0; k < 4; k++) if (!In(ctx, k, out d[k])) return false;
                if (!In(ctx, 4, out bool s0) || !In(ctx, 5, out bool s1)) return false;
                value = d[(s1 ? 2 : 0) + (s0 ? 1 : 0)];
                return true;
            }

            case LogicGateType.HALF_ADDER:
            {
                if (!In(ctx, 0, out bool a) || !In(ctx, 1, out bool b)) return false;
                if (outIndex == 0) value = a ^ b;
                else if (outIndex == 1) value = a && b;
                else return false;
                return true;
            }

            case LogicGateType.FULL_ADDER:
            {
                if (!In(ctx, 0, out bool a) || !In(ctx, 1, out bool b) || !In(ctx, 2, out bool c)) return false;
                if (outIndex == 0) value = a ^ b ^ c;
                else if (outIndex == 1) value = (a && b) || (c && (a ^ b));
                else return false;
                return true;
            }

            case LogicGateType.NOT:
            {
                if (outIndex != 0) return false;
                foreach (var p in inputPorts)
                {
                    if (p == null || !p.IsConnected) continue;
                    if (!p.TryGetValue(ctx, out bool a)) return false;
                    value = !a;
                    return true;
                }
                return false;
            }

            default:
            {
                if (outIndex != 0) return false;
                int count = 0;
                bool and = true, or = false, xor = false;
                foreach (var p in inputPorts)
                {
                    if (p == null || !p.IsConnected) continue;
                    if (!p.TryGetValue(ctx, out bool a)) return false;
                    count++;
                    and &= a;
                    or |= a;
                    xor ^= a;
                }
                if (count < 2) return false;

                switch (type)
                {
                    case LogicGateType.AND: value = and; break;
                    case LogicGateType.OR: value = or; break;
                    case LogicGateType.XOR: value = xor; break;
                    case LogicGateType.NAND: value = !and; break;
                    case LogicGateType.NOR: value = !or; break;
                    case LogicGateType.XNOR: value = !xor; break;
                    default: return false;
                }
                return true;
            }
        }
    }

    // ---------------------------------------------------------------- Carteles

    private static readonly string[] Basic = { "A", "B", "C", "D", "E", "F" };

    private void UpdateLabels()
    {
        string[] ins, outs;
        string title;

        if (CurrentGate == null)
        {
            ins = new string[0];
            outs = new string[0];
            title = "vacío";
        }
        else
        {
            var t = CurrentGate.GateType;
            title = Pretty(t);
            switch (t)
            {
                case LogicGateType.MUX2: ins = new[] { "I0", "I1", "S" }; outs = new[] { "Y" }; break;
                case LogicGateType.MUX4: ins = new[] { "I0", "I1", "I2", "I3", "S0", "S1" }; outs = new[] { "Y" }; break;
                case LogicGateType.HALF_ADDER: ins = new[] { "A", "B" }; outs = new[] { "S", "C" }; break;
                case LogicGateType.FULL_ADDER: ins = new[] { "A", "B", "Cin" }; outs = new[] { "S", "Cout" }; break;
                case LogicGateType.NOT: ins = new[] { "A" }; outs = new[] { "Y" }; break;
                default: ins = Basic; outs = new[] { "Y" }; break;
            }
        }

        if (label != null) label.text = title;
        for (int i = 0; i < inputPorts.Length; i++)
            if (inputPorts[i] != null) inputPorts[i].SetLabel(i < ins.Length ? ins[i] : "·");
        for (int i = 0; i < outputPins.Length; i++)
            if (outputPins[i] != null && outputPins[i].label != null) outputPins[i].label.text = i < outs.Length ? outs[i] : "·";
    }

    public static string Pretty(LogicGateType t)
    {
        switch (t)
        {
            case LogicGateType.MUX2: return "MUX 2:1";
            case LogicGateType.MUX4: return "MUX 4:1";
            case LogicGateType.HALF_ADDER: return "Medio sumador";
            case LogicGateType.FULL_ADDER: return "Sumador completo";
            default: return t.ToString();
        }
    }
}
