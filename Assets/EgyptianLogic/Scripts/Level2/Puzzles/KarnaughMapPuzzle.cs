using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// 2.3 El Ahorro de Compuertas. Se muestra un mapa de Karnaugh (2, 3 o 4 variables, con celdas X).
/// El jugador puede marcar grupos tocando celdas (el cartel dice si cada grupo es válido y qué término da)
/// y escribe la expresión mínima con el teclado. Se aprueba si la expresión es equivalente en todas las
/// filas que no son X y usa el mínimo de literales (calculado con LogicMinimizer).
/// </summary>
public class KarnaughMapPuzzle : Level2Puzzle
{
    [Header("Función")]
    [Tooltip("2 a 4 variables de una letra, la primera es el bit más significativo.")]
    public string[] variables = { "A", "B" };
    [TextArea(2, 8)]
    public string spec = "F = A&B | A&!B";
    public string outputName = "F";
    [Tooltip("Enunciado mostrado al jugador (la expresión a simplificar). Vacío = solo el mapa.")]
    [TextArea(1, 4)]
    public string givenExpression = "F = AB + AB'";
    public TextMeshPro expressionText;

    [Header("Mapa")]
    [Tooltip("Padre del mapa. El jugador lo mira desde -Z local.")]
    public Transform mapRoot;
    public GameObject cellPrefab;
    public Vector2 cellSize = new Vector2(0.12f, 0.12f);
    public float gap = 0.012f;

    [Header("Grupos")]
    public TextMeshPro groupInfoText;
    public Color[] groupColors =
    {
        new Color(0.95f, 0.3f, 0.3f), new Color(0.3f, 0.75f, 0.35f), new Color(0.3f, 0.55f, 1f),
        new Color(0.95f, 0.6f, 0.15f), new Color(0.7f, 0.35f, 0.9f), new Color(0.2f, 0.85f, 0.85f)
    };

    [Header("Respuesta")]
    public ExpressionKeypad keypad;

    [Header("Colores")]
    public Color headerColor = new Color(0.35f, 0.25f, 0.12f);
    public Color oneColor = new Color(0.95f, 0.88f, 0.65f);
    public Color zeroColor = new Color(0.6f, 0.55f, 0.45f);
    public Color dontCareColor = new Color(0.55f, 0.55f, 0.55f);

    private LogicSpec parsed;
    private bool[] ones;
    private bool[] dcs;
    private LogicMinimizer.Result optimum;
    private readonly Dictionary<int, Level2PressCell> cellByMinterm = new Dictionary<int, Level2PressCell>();
    private readonly List<HashSet<int>> groups = new List<HashSet<int>>();
    private int currentGroup = -1;

    protected override void Start()
    {
        base.Start();

        if (variables.Length < 2 || variables.Length > 4 || variables.Any(v => v.Length != 1))
        {
            Debug.LogError($"[{puzzleId}] El mapa necesita entre 2 y 4 variables de una sola letra.");
            return;
        }
        if (!LogicSpec.TryParse(spec, false, out parsed, out string error))
        {
            Debug.LogError($"[{puzzleId}] Especificación inválida: {error}");
            return;
        }

        ComputeFunction();
        optimum = LogicMinimizer.Minimize(variables, ones, dcs);
        Debug.Log($"[{puzzleId}] Solución mínima: {outputName} = {optimum.expression} ({optimum.literals} literales)");

        BuildMap();

        if (expressionText != null) expressionText.text = givenExpression;
        if (keypad != null) keypad.KeyPressed += ReportInteraction;
        RefreshGroupInfo();
        SetStatus("Agrupa los 1 y escribe la expresión mínima.");
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (keypad != null) keypad.KeyPressed -= ReportInteraction;
    }

    private void ComputeFunction()
    {
        int rows = 1 << variables.Length;
        ones = new bool[rows];
        dcs = new bool[rows];
        var env = new Dictionary<string, bool>();
        var dcSet = new HashSet<string>();
        for (int r = 0; r < rows; r++)
        {
            env.Clear();
            TruthTableUtil.FillEnv(env, variables, r);
            parsed.Evaluate(env, dcSet);
            ones[r] = env.TryGetValue(outputName, out bool v) && v;
            dcs[r] = dcSet.Contains(outputName);
        }
    }

    // ---------------------------------------------------------------- Mapa

    private static int[] Gray(int bits) => bits == 1 ? new[] { 0, 1 } : new[] { 0, 1, 3, 2 };

    private static string Bits(int value, int count)
    {
        var sb = new StringBuilder();
        for (int i = count - 1; i >= 0; i--) sb.Append(((value >> i) & 1) == 1 ? '1' : '0');
        return sb.ToString();
    }

    private void BuildMap()
    {
        if (mapRoot == null) mapRoot = transform;

        int n = variables.Length;
        int rowBits = n / 2;
        int colBits = n - rowBits;
        int[] rowsGray = Gray(rowBits);
        int[] colsGray = Gray(colBits);

        float stepX = cellSize.x + gap, stepY = cellSize.y + gap;
        float x0 = -(colsGray.Length) * stepX * 0.5f;
        float y0 = (rowsGray.Length) * stepY * 0.5f;

        string rowVars = string.Concat(variables.Take(rowBits));
        string colVars = string.Concat(variables.Skip(rowBits));

        var corner = Level2PressCell.Create(mapRoot, "Corner", new Vector3(x0, y0, 0f), cellSize, $"{rowVars}\\{colVars}", headerColor, cellPrefab);
        corner.Interactable = false;

        for (int c = 0; c < colsGray.Length; c++)
        {
            var h = Level2PressCell.Create(mapRoot, "Col_" + c, new Vector3(x0 + (c + 1) * stepX, y0, 0f), cellSize, Bits(colsGray[c], colBits), headerColor, cellPrefab);
            h.Interactable = false;
        }

        for (int r = 0; r < rowsGray.Length; r++)
        {
            float y = y0 - (r + 1) * stepY;
            var h = Level2PressCell.Create(mapRoot, "Row_" + r, new Vector3(x0, y, 0f), cellSize, Bits(rowsGray[r], rowBits), headerColor, cellPrefab);
            h.Interactable = false;

            for (int c = 0; c < colsGray.Length; c++)
            {
                int minterm = (rowsGray[r] << colBits) | colsGray[c];
                var cell = Level2PressCell.Create(mapRoot, "m" + minterm, new Vector3(x0 + (c + 1) * stepX, y, 0f), cellSize,
                    ValueText(minterm), BaseColor(minterm), cellPrefab);
                cell.Index = minterm;
                cell.Pressed += OnCellPressed;
                cellByMinterm[minterm] = cell;
            }
        }

        // Botones de grupos debajo del mapa
        float yb = y0 - (rowsGray.Length + 1) * stepY - 0.02f;
        var size = new Vector2(cellSize.x * 2f + gap, cellSize.y * 0.8f);
        var newGroup = Level2PressCell.Create(mapRoot, "NuevoGrupo", new Vector3(x0 + stepX * 0.5f, yb, 0f), size, "Nuevo grupo", headerColor, cellPrefab);
        newGroup.Pressed += _ => { StartNewGroup(); ReportInteraction(); };
        var clear = Level2PressCell.Create(mapRoot, "BorrarGrupos", new Vector3(x0 + stepX * 2.5f + gap, yb, 0f), size, "Borrar grupos", headerColor, cellPrefab);
        clear.Pressed += _ => { ClearGroups(); ReportInteraction(); };
    }

    private string ValueText(int m) => dcs[m] ? "X" : (ones[m] ? "1" : "0");
    private Color BaseColor(int m) => dcs[m] ? dontCareColor : (ones[m] ? oneColor : zeroColor);

    // ---------------------------------------------------------------- Grupos

    private void StartNewGroup()
    {
        if (currentGroup >= 0 && groups[currentGroup].Count == 0) return; // ya hay uno vacío listo
        groups.Add(new HashSet<int>());
        currentGroup = groups.Count - 1;
        RefreshGroupInfo();
    }

    private void ClearGroups()
    {
        groups.Clear();
        currentGroup = -1;
        foreach (var m in cellByMinterm.Keys.ToList()) PaintCell(m);
        RefreshGroupInfo();
    }

    private void OnCellPressed(Level2PressCell cell)
    {
        if (IsSolved) return;
        if (currentGroup < 0) StartNewGroup();

        var g = groups[currentGroup];
        if (!g.Remove(cell.Index)) g.Add(cell.Index);

        PaintCell(cell.Index);
        RefreshGroupInfo();
        ReportInteraction();
    }

    private void PaintCell(int m)
    {
        var cell = cellByMinterm[m];
        int owner = -1;
        if (currentGroup >= 0 && groups[currentGroup].Contains(m)) owner = currentGroup;
        else
            for (int i = groups.Count - 1; i >= 0; i--)
                if (groups[i].Contains(m)) { owner = i; break; }

        if (owner < 0) cell.SetColor(BaseColor(m));
        else cell.SetColor(groupColors[owner % groupColors.Length], owner == currentGroup);
    }

    private void RefreshGroupInfo()
    {
        if (groupInfoText == null) return;
        if (groups.Count == 0)
        {
            groupInfoText.text = "Toca las celdas para formar un grupo.\n\"Nuevo grupo\" empieza otro.";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < groups.Count; i++)
        {
            string hex = ColorUtility.ToHtmlStringRGB(groupColors[i % groupColors.Length]);
            sb.Append($"<color=#{hex}>Grupo {i + 1}</color>");
            if (i == currentGroup) sb.Append(" (actual)");
            sb.Append(": ").Append(DescribeGroup(groups[i])).Append('\n');
        }
        groupInfoText.text = sb.ToString();
    }

    private string DescribeGroup(HashSet<int> g)
    {
        if (g.Count == 0) return "vacío";
        if (g.Any(m => !ones[m] && !dcs[m])) return "tiene un 0, no es válido";

        int n = variables.Length;
        int mask = (1 << n) - 1;
        int allAnd = mask, allOr = 0;
        foreach (var m in g) { allAnd &= m; allOr |= m; }
        int care = allAnd | (~allOr & mask);
        int free = n - PopCount(care);

        if (g.Count != (1 << free))
            return $"{g.Count} celdas: debe ser un rectángulo de 1, 2, 4 u 8 celdas";

        return $"{g.Count} celda(s) → {Term(care, allAnd)}";
    }

    private string Term(int care, int value)
    {
        int n = variables.Length;
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            int bit = 1 << (n - 1 - i);
            if ((care & bit) == 0) continue;
            sb.Append(variables[i]);
            if ((value & bit) == 0) sb.Append('\'');
        }
        return sb.Length == 0 ? "1" : sb.ToString();
    }

    private static int PopCount(int x)
    {
        int c = 0;
        while (x != 0) { c += x & 1; x >>= 1; }
        return c;
    }

    // ---------------------------------------------------------------- Respuesta

    public override void Submit()
    {
        if (IsSolved || parsed == null) return;

        string text = keypad != null ? keypad.Text : "";
        if (string.IsNullOrWhiteSpace(text))
        {
            MarkIncorrect("Escribe la expresión simplificada con el teclado.");
            return;
        }

        if (!BoolExpr.TryParse(text, true, out var expr, out string error))
        {
            MarkIncorrect("La expresión no se entiende: " + error);
            return;
        }

        foreach (var v in expr.Variables)
            if (!variables.Contains(v))
            {
                MarkIncorrect($"La variable {v} no existe en este mapa.");
                return;
            }

        var env = new Dictionary<string, bool>();
        for (int r = 0; r < ones.Length; r++)
        {
            if (dcs[r]) continue;
            env.Clear();
            TruthTableUtil.FillEnv(env, variables, r);
            if (expr.Evaluate(env) != ones[r])
            {
                MarkIncorrect($"No es equivalente: con {TruthTableUtil.RowLabel(r, variables.Length)} ({string.Concat(variables)}) debería dar {(ones[r] ? 1 : 0)}.");
                return;
            }
        }

        if (expr.LiteralCount > optimum.literals)
        {
            MarkIncorrect($"Es equivalente, pero usa {expr.LiteralCount} literales. Se puede con {optimum.literals}: agrupa más.");
            return;
        }

        if (keypad != null) keypad.Locked = true;
        MarkSolved($"¡Mínima! {outputName} = {text}");
    }
}
