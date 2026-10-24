using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// 2.1 El Detective de Verdades. Se muestra un circuito y el jugador llena su tabla de verdad
/// tocando las celdas de la columna de salida (vacía → 0 → 1 → 0 …). Las filas marcadas "X"
/// (no importa) se aceptan con cualquier valor. La tabla se genera sola al iniciar.
/// </summary>
public class TruthTablePuzzle : Level2Puzzle
{
    [Header("Circuito a analizar")]
    [Tooltip("Entradas en orden (la primera es el bit más significativo).")]
    public string[] inputNames = { "A", "B" };
    [Tooltip("Especificación del circuito oculto. Ej: S = (A | B) & !(A & B). Puede tener 'dc S = ...' para las filas X.")]
    [TextArea(2, 8)]
    public string spec = "S = (A | B) & !(A & B)";
    public string outputName = "S";
    [Tooltip("Cómo se le muestra el circuito al jugador. Vacío = se genera desde la especificación.")]
    [TextArea(2, 6)]
    public string circuitDescription;
    public TextMeshPro circuitText;

    [Header("Tabla")]
    [Tooltip("Padre de la tabla. El jugador la mira desde -Z local.")]
    public Transform tableRoot;
    [Tooltip("Prefab opcional para las celdas (Renderer + TextMeshPro). Vacío = cubos generados.")]
    public GameObject cellPrefab;
    public Vector2 cellSize = new Vector2(0.11f, 0.07f);
    public float gap = 0.012f;

    [Header("Colores")]
    public Color headerColor = new Color(0.35f, 0.25f, 0.12f);
    public Color inputCellColor = new Color(0.55f, 0.45f, 0.3f);
    public Color emptyColor = new Color(0.85f, 0.8f, 0.65f);
    public Color zeroColor = new Color(0.25f, 0.45f, 0.95f);
    public Color oneColor = new Color(1f, 0.75f, 0.2f);
    public Color dontCareColor = new Color(0.5f, 0.5f, 0.5f);
    public Color wrongColor = new Color(1f, 0.2f, 0.15f);

    private LogicSpec parsed;
    private bool[] expected;
    private bool[] isDontCare;
    private int[] answers; // -1 vacío, 0, 1
    private readonly List<Level2PressCell> outputCells = new List<Level2PressCell>();

    protected override void Start()
    {
        base.Start();

        if (!LogicSpec.TryParse(spec, false, out parsed, out string error))
        {
            Debug.LogError($"[{puzzleId}] Especificación inválida: {error}");
            return;
        }

        ComputeExpected();
        BuildTable();

        if (circuitText != null)
            circuitText.text = string.IsNullOrEmpty(circuitDescription) ? parsed.DescribeOutput(outputName) : circuitDescription;

        int dcRows = isDontCare.Count(d => d);
        SetStatus(dcRows > 0
            ? $"Completa la columna {outputName}. Las filas X no importan."
            : $"Completa la columna {outputName} y presiona el botón.");
    }

    private void ComputeExpected()
    {
        int rows = 1 << inputNames.Length;
        expected = new bool[rows];
        isDontCare = new bool[rows];
        answers = new int[rows];

        var env = new Dictionary<string, bool>();
        var dcs = new HashSet<string>();
        for (int r = 0; r < rows; r++)
        {
            env.Clear();
            TruthTableUtil.FillEnv(env, inputNames, r);
            parsed.Evaluate(env, dcs);
            expected[r] = env.TryGetValue(outputName, out bool v) && v;
            isDontCare[r] = dcs.Contains(outputName);
            answers[r] = -1;
        }
    }

    private void BuildTable()
    {
        if (tableRoot == null) tableRoot = transform;
        int n = inputNames.Length;
        int rows = 1 << n;
        float stepX = cellSize.x + gap;
        float stepY = cellSize.y + gap;
        float width = (n + 1) * stepX;
        float x0 = -width * 0.5f + stepX * 0.5f;
        float y0 = (rows + 1) * stepY * 0.5f - stepY * 0.5f;

        // Encabezado
        for (int c = 0; c <= n; c++)
        {
            string text = c < n ? inputNames[c] : outputName;
            var h = Level2PressCell.Create(tableRoot, "Header_" + text, new Vector3(x0 + c * stepX, y0, 0f), cellSize, text, headerColor, cellPrefab);
            h.Interactable = false;
        }

        for (int r = 0; r < rows; r++)
        {
            float y = y0 - (r + 1) * stepY;
            for (int c = 0; c < n; c++)
            {
                string bit = TruthTableUtil.Bit(r, c, n) ? "1" : "0";
                var cell = Level2PressCell.Create(tableRoot, $"R{r}_{inputNames[c]}", new Vector3(x0 + c * stepX, y, 0f), cellSize, bit, inputCellColor, cellPrefab);
                cell.Interactable = false;
            }

            var outCell = Level2PressCell.Create(tableRoot, $"R{r}_{outputName}", new Vector3(x0 + n * stepX, y, 0f), cellSize,
                isDontCare[r] ? "X" : "?", isDontCare[r] ? dontCareColor : emptyColor, cellPrefab);
            outCell.Index = r;
            outCell.Pressed += OnCellPressed;
            outputCells.Add(outCell);
        }
    }

    private void OnCellPressed(Level2PressCell cell)
    {
        if (IsSolved) return;
        int r = cell.Index;
        answers[r] = answers[r] == 1 ? 0 : 1;
        Paint(r, false);
        ReportInteraction();
    }

    private void Paint(int r, bool wrong)
    {
        var cell = outputCells[r];
        if (answers[r] < 0)
        {
            cell.SetText(isDontCare[r] ? "X" : "?");
            cell.SetColor(wrong ? wrongColor : (isDontCare[r] ? dontCareColor : emptyColor));
            return;
        }

        cell.SetText(isDontCare[r] ? $"{answers[r]} (X)" : answers[r].ToString());
        cell.SetColor(wrong ? wrongColor : (answers[r] == 1 ? oneColor : zeroColor), answers[r] == 1 && !wrong);
    }

    public override void Submit()
    {
        if (IsSolved || parsed == null) return;

        int wrong = 0, empty = 0;
        for (int r = 0; r < answers.Length; r++)
        {
            bool bad = false;
            if (!isDontCare[r])
            {
                if (answers[r] < 0) { empty++; bad = true; }
                else if ((answers[r] == 1) != expected[r]) { wrong++; bad = true; }
            }
            Paint(r, bad);
        }

        if (wrong == 0 && empty == 0)
        {
            MarkSolved("¡Tabla correcta!");
            return;
        }

        MarkIncorrect(empty > 0 && wrong == 0
            ? $"Faltan {empty} fila(s) por llenar."
            : $"Hay {wrong + empty} fila(s) incorrecta(s), marcadas en rojo.");
        Invoke(nameof(RepaintAll), 2f);
    }

    private void RepaintAll()
    {
        if (IsSolved) return;
        for (int r = 0; r < answers.Length; r++) Paint(r, false);
    }
}
