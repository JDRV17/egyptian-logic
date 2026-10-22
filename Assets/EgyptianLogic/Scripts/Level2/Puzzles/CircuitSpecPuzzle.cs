using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Reto de construir un circuito en el protoboard que cumpla una especificación (LogicSpec).
/// Se usa en 2.2 (solo NAND/NOR), 2.4 (multiplexor), 2.5 (decodificador) y 2.6 (sumadores).
///
/// Formas de aprobar:
///  • Con botón de comprobar: se recorren TODAS las combinaciones de entradas y cada salida debe
///    coincidir con la especificación (las filas "dc" no se revisan).
///  • Con "Live Test Inputs": el jugador debe probar en vivo cada combinación de esas entradas
///    (ej. S=0 y S=1 en el MUX) mientras el circuito es correcto; al cubrirlas todas se aprueba solo.
/// </summary>
public class CircuitSpecPuzzle : Level2Puzzle
{
    [Header("Circuito")]
    public Breadboard board;

    [Tooltip("Una línea por salida. Puede usar nombres intermedios y 'dc' para filas que no importan.")]
    [TextArea(4, 14)]
    public string spec = "S = A & B";

    [Tooltip("Compuertas permitidas (vacío = cualquiera). Ej. solo NAND en 2.2.")]
    public LogicGateType[] allowedGates = new LogicGateType[0];

    [Header("Prueba en vivo (opcional)")]
    [Tooltip("Entradas que el jugador debe recorrer con los switches, ej. S (2.4.1) o S1,S0 (2.4.2).")]
    public string[] liveTestInputs = new string[0];

    [Header("Carteles")]
    [Tooltip("Enunciado del reto (si está vacío se muestra la especificación).")]
    [TextArea(2, 6)]
    public string instructions;
    public TextMeshPro instructionsText;
    public TextMeshPro progressText;

    private LogicSpec parsed;
    private string[] inputNames;
    private string[] outputNames;
    private readonly HashSet<int> testedCombos = new HashSet<int>();
    private bool circuitCorrect;
    private string lastStructure;

    private readonly Dictionary<string, bool> env = new Dictionary<string, bool>();
    private readonly Dictionary<string, bool> actual = new Dictionary<string, bool>();
    private readonly HashSet<string> dontCares = new HashSet<string>();

    private bool UsesLiveTest => liveTestInputs != null && liveTestInputs.Length > 0;

    private void Awake()
    {
        if (!LogicSpec.TryParse(spec, false, out parsed, out string error))
        {
            Debug.LogError($"[{puzzleId}] Especificación inválida: {error}");
            enabled = false;
            return;
        }

        if (board == null) board = GetComponentInChildren<Breadboard>();
        if (board == null)
        {
            Debug.LogError($"[{puzzleId}] Falta el Breadboard.");
            enabled = false;
        }
    }

    protected override void Start()
    {
        base.Start();
        if (board == null || parsed == null) return;

        inputNames = board.VariableInputs.Select(i => i.displayName).ToArray();
        outputNames = board.outputs.Where(o => o != null).Select(o => o.displayName).ToArray();

        foreach (var v in parsed.FreeVariables)
            if (!inputNames.Contains(v) && !board.inputs.Any(i => i != null && i.displayName == v))
                Debug.LogError($"[{puzzleId}] La especificación usa '{v}' pero el protoboard no tiene esa entrada.");
        foreach (var o in outputNames)
            if (!parsed.Defines(o))
                Debug.LogError($"[{puzzleId}] El protoboard tiene la salida '{o}' pero la especificación no la define.");

        if (instructionsText != null)
            instructionsText.text = string.IsNullOrEmpty(instructions)
                ? string.Join("\n", outputNames.Select(o => parsed.DescribeOutput(o)))
                : instructions;

        HookStandardInteractions(board.transform);
        UpdateProgress();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (board != null) board.onCircuitChanged.AddListener(OnCircuitChanged);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (board != null) board.onCircuitChanged.RemoveListener(OnCircuitChanged);
    }

    // ---------------------------------------------------------------- Validación completa

    public override void Submit()
    {
        if (IsSolved || parsed == null) return;

        if (!CheckAll(out string problem))
        {
            MarkIncorrect(problem);
            return;
        }

        if (UsesLiveTest && testedCombos.Count < (1 << liveTestInputs.Length))
        {
            MarkIncorrect($"El circuito es correcto. Ahora pruébalo con los switches de {string.Join(", ", liveTestInputs)}.");
            return;
        }

        MarkSolved("¡Circuito correcto!");
    }

    /// <summary>Compara el circuito contra la especificación en todas las combinaciones.</summary>
    private bool CheckAll(out string problem)
    {
        problem = null;

        string forbidden = ForbiddenGate();
        if (forbidden != null)
        {
            problem = $"Aquí solo puedes usar {string.Join(", ", allowedGates.Select(GateSlot.Pretty))}. Quita la compuerta {forbidden}.";
            return false;
        }

        int n = inputNames.Length;
        int rows = 1 << n;
        for (int r = 0; r < rows; r++)
        {
            env.Clear();
            actual.Clear();
            TruthTableUtil.FillEnv(env, inputNames, r);
            FillConstants(env);

            bool complete = board.TryEvaluate(env, actual);
            parsed.Evaluate(env, dontCares);

            foreach (var o in outputNames)
            {
                if (dontCares.Contains(o)) continue;

                if (!complete && !actual.ContainsKey(o))
                {
                    problem = $"La salida {o} no recibe señal. Revisa que todas las compuertas y cables estén conectados.";
                    return false;
                }

                if (actual[o] != env[o])
                {
                    problem = $"Con {DescribeRow(r)} la salida {o} da {B(actual[o])} y debería dar {B(env[o])}.";
                    return false;
                }
            }
        }

        return true;
    }

    private void FillConstants(Dictionary<string, bool> e)
    {
        foreach (var i in board.inputs)
            if (i != null && i.isConstant) e[i.displayName] = i.fixedValue;
    }

    private string ForbiddenGate()
    {
        if (allowedGates == null || allowedGates.Length == 0) return null;
        foreach (var g in board.PlacedGates())
            if (!allowedGates.Contains(g.GateType)) return GateSlot.Pretty(g.GateType);
        return null;
    }

    private string DescribeRow(int r)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < inputNames.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(inputNames[i]).Append('=').Append(TruthTableUtil.Bit(r, i, inputNames.Length) ? '1' : '0');
        }
        return sb.ToString();
    }

    private static string B(bool v) => v ? "1" : "0";

    // ---------------------------------------------------------------- Prueba en vivo

    private void OnCircuitChanged()
    {
        if (IsSolved || parsed == null || inputNames == null) return;

        // Solo se recalcula la tabla completa cuando cambian compuertas o cables, no al mover switches
        string structure = StructureKey();
        if (structure != lastStructure)
        {
            lastStructure = structure;
            circuitCorrect = CheckAll(out _);
            testedCombos.Clear();
        }

        if (UsesLiveTest && circuitCorrect)
        {
            int combo = 0;
            foreach (var name in liveTestInputs)
            {
                board.TryGetLiveInput(name, out bool v);
                combo = (combo << 1) | (v ? 1 : 0);
            }
            testedCombos.Add(combo);

            if (testedCombos.Count >= (1 << liveTestInputs.Length))
            {
                MarkSolved("¡Circuito correcto y probado!");
                return;
            }
        }

        UpdateProgress();
    }

    private string StructureKey()
    {
        var sb = new StringBuilder();
        foreach (var s in board.Slots)
        {
            sb.Append(s.CurrentGate != null ? Id(s.CurrentGate) : "-").Append(':');
            foreach (var p in s.inputPorts)
                sb.Append(p != null && p.Source != null ? Id(p.Source) : "-").Append(',');
            sb.Append('|');
        }
        foreach (var o in board.outputs)
            if (o != null) sb.Append(o.port != null && o.port.Source != null ? Id(o.port.Source) : "-").Append(';');
        return sb.ToString();
    }

    private static string Id(Object o) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o).ToString();

    private void UpdateProgress()
    {
        if (progressText == null) return;

        if (!UsesLiveTest)
        {
            progressText.text = "Arma el circuito y presiona el botón para comprobar.";
            return;
        }

        int total = 1 << liveTestInputs.Length;
        progressText.text = circuitCorrect
            ? $"Pruebas con {string.Join(", ", liveTestInputs)}: {testedCombos.Count}/{total}"
            : $"Arma el circuito y prueba las {total} combinaciones de {string.Join(", ", liveTestInputs)}.";
    }
}
