using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Menú "Egyptian Logic > Nivel 2" que arma en la escena abierta los retos del Nivel 2
/// (6 juegos × 3 versiones) con piezas básicas: switches, ranuras, cables, compuertas, botones y carteles.
/// Cada reto queda con un PuzzleVersionManager que activa una versión al iniciar (aleatoria por defecto)
/// y que el AdaptationEngine puede controlar igual que en el Nivel 1.
/// Luego se pueden mover, reemplazar los modelos o cambiar materiales sin tocar la lógica.
/// </summary>
public static class Level2Builder
{
    private const string MaterialFolder = "Assets/EgyptianLogic/Materials/Level2";
    private const string Menu = "Egyptian Logic/Nivel 2/";

    // ================================================================ Menú

    [MenuItem(Menu + "Construir los 6 retos")]
    public static void BuildAll()
    {
        var root = new GameObject("Nivel2_Retos");
        Undo.RegisterCreatedObjectUndo(root, "Construir retos del Nivel 2");

        float x = 0f;
        foreach (var build in new System.Func<Transform, float>[] { Build21, Build22, Build23, Build24, Build25, Build26 })
        {
            var holder = new GameObject("tmp");
            holder.transform.SetParent(root.transform, false);
            float width = build(holder.transform);
            var challenge = holder.transform.GetChild(0);
            challenge.SetParent(root.transform, false);
            Object.DestroyImmediate(holder);

            challenge.localPosition = new Vector3(x + width * 0.5f, 0f, 0f);
            x += width + 1.5f;
        }

        Selection.activeGameObject = root;
        Debug.Log("[Level2Builder] Retos del Nivel 2 creados. Revisa Assets/EgyptianLogic/Scripts/Level2/LEEME_Nivel2.md");
    }

    [MenuItem(Menu + "Construir solo 2.1 El Detective de Verdades")] private static void Menu21() => BuildOne(Build21);
    [MenuItem(Menu + "Construir solo 2.2 Solo Tengo Esto (NAND)")] private static void Menu22() => BuildOne(Build22);
    [MenuItem(Menu + "Construir solo 2.3 El Ahorro de Compuertas")] private static void Menu23() => BuildOne(Build23);
    [MenuItem(Menu + "Construir solo 2.4 El Selector (MUX)")] private static void Menu24() => BuildOne(Build24);
    [MenuItem(Menu + "Construir solo 2.5 El Repartidor (Decodificador)")] private static void Menu25() => BuildOne(Build25);
    [MenuItem(Menu + "Construir solo 2.6 La Balanza Digital (Sumador)")] private static void Menu26() => BuildOne(Build26);

    private static void BuildOne(System.Func<Transform, float> build)
    {
        var holder = new GameObject("tmp");
        build(holder.transform);
        var challenge = holder.transform.GetChild(0);
        challenge.SetParent(null, true);
        Object.DestroyImmediate(holder);
        Undo.RegisterCreatedObjectUndo(challenge.gameObject, "Construir reto");
        Selection.activeGameObject = challenge.gameObject;
    }

    // ================================================================ Definiciones de cada reto

    private static float Build21(Transform parent)
    {
        var defs = new[]
        {
            new TableDef
            {
                title = "2.1.1 El Detective de Verdades (Fácil)", inputs = new[] { "A", "B" },
                spec = "S = (A | B) & !(A & B)",
                description = "Circuito:  S = AND( OR(A, B), NAND(A, B) )\nCompleta la tabla de verdad."
            },
            new TableDef
            {
                title = "2.1.2 El Detective de Verdades (Medio)", inputs = new[] { "A", "B", "C" },
                spec = "S = (!A & B) | C",
                description = "Circuito:  S = OR( AND( NOT(A), B ), C )\nResuélvelo etapa por etapa."
            },
            new TableDef
            {
                title = "2.1.3 El Detective de Verdades (Difícil)", inputs = new[] { "A", "B", "C", "D" },
                spec = "S = (A ^ B) & (C | !D)\ndc S = A & B",
                description = "Circuito:  S = AND( XOR(A, B), OR(C, NOT(D)) )\nA=1 y B=1 nunca ocurre a la vez: esas filas son X."
            },
        };
        return BuildChallenge(parent, "Reto 2.1 - El Detective de Verdades", defs.Select(d => (System.Func<Transform, float>)(p => BuildTruthTable(p, d))).ToArray());
    }

    private static float Build22(Transform parent)
    {
        var nand = new[] { LogicGateType.NAND };
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.2.1 Solo Tengo Esto (Fácil)", inputs = new[] { "A", "B" }, outputs = new[] { "S" },
                spec = "S = A & B", allowed = nand, slots = 3, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NAND, 3)),
                instructions = "Construye S = A AND B usando SOLO compuertas NAND.\nRecuerda: NOT(A) = NAND(A, A)   y   AND = NOT(NAND)."
            },
            new CircuitDef
            {
                title = "2.2.2 Solo Tengo Esto (Medio)", inputs = new[] { "A", "B", "C" }, outputs = new[] { "S" },
                spec = "S = (A & B) | C", allowed = nand, slots = 5, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NAND, 5)),
                instructions = "Construye S = (A AND B) OR C usando SOLO NAND.\nDe Morgan: A OR B = NAND( NAND(A,A), NAND(B,B) )."
            },
            new CircuitDef
            {
                title = "2.2.3 Solo Tengo Esto (Difícil)", inputs = new[] { "A", "B" }, outputs = new[] { "S", "C" },
                spec = "S = A ^ B\nC = A & B", allowed = nand, slots = 6, ports = 2, pins = 1, inputFanout = 3, pinFanout = 3,
                gates = Gates((LogicGateType.NAND, 6)),
                instructions = "Construye un medio sumador con SOLO NAND:\nSuma S = A XOR B      Acarreo C = A AND B"
            },
        };
        return BuildChallenge(parent, "Reto 2.2 - Solo Tengo Esto", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    private static float Build23(Transform parent)
    {
        var defs = new[]
        {
            new KMapDef
            {
                title = "2.3.1 El Ahorro de Compuertas (Fácil)", vars = new[] { "A", "B" },
                spec = "F = A&B | A&!B", given = "Simplifica:  F = A·B + A·B'"
            },
            new KMapDef
            {
                title = "2.3.2 El Ahorro de Compuertas (Medio)", vars = new[] { "A", "B", "C" },
                spec = "F = !A&!B&!C | !A&!B&C | A&!B&!C | A&!B&C | A&B&!C",
                given = "Simplifica:  F = A'B'C' + A'B'C + AB'C' + AB'C + ABC'"
            },
            new KMapDef
            {
                title = "2.3.3 El Ahorro de Compuertas (Difícil)", vars = new[] { "A", "B", "C", "D" },
                spec = "F = !A&!B&!C&D | !A&!B&C&D | !A&B&C&D | A&!B&C&D | A&B&C&D\n" +
                       "dc F = !A&!B&!C&!D | !A&!B&C&!D | !A&B&!C&D",
                given = "F = m(1, 3, 7, 11, 15) + X(0, 2, 5)\nAprovecha las X para agrupar más."
            },
        };
        return BuildChallenge(parent, "Reto 2.3 - El Ahorro de Compuertas", defs.Select(d => (System.Func<Transform, float>)(p => BuildKMap(p, d))).ToArray());
    }

    private static float Build24(Transform parent)
    {
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.4.1 El Selector (Fácil)", inputs = new[] { "I0", "I1", "S" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S" }, 2), slots = 4, ports = 2, pins = 1, inputFanout = 2, pinFanout = 2,
                gates = Gates((LogicGateType.NOT, 1), (LogicGateType.AND, 2), (LogicGateType.OR, 1), (LogicGateType.XOR, 1)),
                liveTest = new[] { "S" },
                instructions = "Arma un MUX 2:1:  Y = S'·I0 + S·I1\nPruébalo con S = 0 y S = 1."
            },
            new CircuitDef
            {
                title = "2.4.2 El Selector (Medio)", inputs = new[] { "I0", "I1", "I2", "I3", "S1", "S0" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S1", "S0" }, 4), slots = 3, ports = 3, pins = 1, inputFanout = 2, pinFanout = 1,
                gates = Gates((LogicGateType.MUX2, 3), (LogicGateType.OR, 1)),
                liveTest = new[] { "S1", "S0" },
                instructions = "Arma un MUX 4:1 con módulos MUX 2:1 (I0, I1, S).\nS1S0 = 00→I0, 01→I1, 10→I2, 11→I3. Prueba las 4."
            },
            new CircuitDef
            {
                title = "2.4.3 El Selector (Difícil)",
                inputs = new[] { "I0", "I1", "I2", "I3", "I4", "I5", "I6", "I7", "S2", "S1", "S0" }, outputs = new[] { "Y" },
                spec = MuxSpec(new[] { "S2", "S1", "S0" }, 8), slots = 3, ports = 6, pins = 1, inputFanout = 2, pinFanout = 1,
                gates = Gates((LogicGateType.MUX4, 2), (LogicGateType.MUX2, 2)),
                liveTest = new[] { "S2", "S1", "S0" },
                instructions = "Arma un MUX 8:1 en cascada: dos MUX 4:1 (I0-I3, S0, S1) y un MUX 2:1 final con S2.\nPrueba las 8 combinaciones."
            },
        };
        return BuildChallenge(parent, "Reto 2.4 - El Selector", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    private static float Build25(Transform parent)
    {
        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.5.1 El Repartidor (Fácil)", inputs = new[] { "A1", "A0" }, outputs = Names("Y", 4),
                spec = DecoderSpec(new[] { "A1", "A0" }, null), slots = 6, ports = 2, pins = 1, inputFanout = 3, pinFanout = 3,
                gates = Gates((LogicGateType.NOT, 2), (LogicGateType.AND, 4), (LogicGateType.OR, 1)),
                liveTest = new[] { "A1", "A0" },
                instructions = "Decodificador 2→4: para cada dirección A1A0 debe encenderse SOLO la puerta Y correspondiente.\nPrueba las 4 direcciones."
            },
            new CircuitDef
            {
                title = "2.5.2 El Repartidor (Medio)", inputs = new[] { "A2", "A1", "A0" }, outputs = Names("Y", 8),
                spec = DecoderSpec(new[] { "A2", "A1", "A0" }, null), slots = 11, ports = 3, pins = 1, inputFanout = 5, pinFanout = 4,
                gates = Gates((LogicGateType.NOT, 3), (LogicGateType.AND, 8)),
                liveTest = new[] { "A2", "A1", "A0" },
                instructions = "Decodificador 3→8: cada dirección A2A1A0 abre UNA sola bóveda (Y0-Y7).\nLas AND aceptan 3 cables. Prueba las 8."
            },
            new CircuitDef
            {
                title = "2.5.3 El Repartidor (Difícil)", inputs = new[] { "E", "A1", "A0" }, outputs = Names("Y", 4),
                spec = DecoderSpec(new[] { "A1", "A0" }, "E"), slots = 6, ports = 3, pins = 1, inputFanout = 4, pinFanout = 3,
                gates = Gates((LogicGateType.NOT, 2), (LogicGateType.AND, 4)),
                liveTest = new[] { "E", "A1", "A0" },
                instructions = "Decodificador 2→4 con Enable: si E = 0 ninguna salida se enciende; si E = 1 funciona normal.\nPrueba las 8 combinaciones de E, A1, A0."
            },
        };
        return BuildChallenge(parent, "Reto 2.5 - El Repartidor", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    private static float Build26(Transform parent)
    {
        var a = new[] { "A3", "A2", "A1", "A0" };
        var b = new[] { "B3", "B2", "B1", "B0" };
        var sums = new[] { "C4", "S3", "S2", "S1", "S0" };

        var defs = new[]
        {
            new CircuitDef
            {
                title = "2.6.1 La Balanza Digital (Fácil)", inputs = new[] { "A", "B" }, outputs = new[] { "S", "C" },
                spec = "S = A ^ B\nC = A & B", slots = 2, ports = 2, pins = 1, inputFanout = 2, pinFanout = 1,
                gates = Gates((LogicGateType.XOR, 1), (LogicGateType.AND, 1), (LogicGateType.OR, 1)),
                liveTest = new[] { "A", "B" },
                instructions = "Medio sumador de 1 bit: Suma S y Acarreo C.\nPrueba las 4 combinaciones de A y B."
            },
            new CircuitDef
            {
                title = "2.6.2 La Balanza Digital (Medio)", inputs = a.Concat(b).ToArray(), constants = new[] { "GND" },
                outputs = sums, spec = AdderSpec(false), slots = 4, ports = 3, pins = 2, inputFanout = 1, pinFanout = 1,
                gates = Gates((LogicGateType.FULL_ADDER, 4), (LogicGateType.HALF_ADDER, 1)),
                instructions = "Encadena 4 sumadores completos: A3A2A1A0 + B3B2B1B0 = C4 S3S2S1S0.\nEl Cout de cada etapa va al Cin de la siguiente; el primer Cin va a GND (0)."
            },
            new CircuitDef
            {
                title = "2.6.3 La Balanza Digital (Difícil)", inputs = a.Concat(b).Concat(new[] { "M" }).ToArray(),
                outputs = sums.Concat(new[] { "GT", "EQ", "LT" }).ToArray(), spec = AdderSpec(true),
                slots = 12, ports = 4, pins = 2, inputFanout = 1, pinFanout = 3, fanoutOverrides = new Dictionary<string, int> { { "M", 5 } },
                gates = Gates((LogicGateType.XOR, 4), (LogicGateType.FULL_ADDER, 4), (LogicGateType.NOR, 1), (LogicGateType.NOT, 2), (LogicGateType.AND, 1)),
                instructions = "Sumador/Restador con comparador. M = 0 suma, M = 1 resta (A − B en complemento a 2: B XOR M y Cin = M).\n" +
                               "Con M = 1:  EQ = el resultado es 0,  LT = no hubo acarreo final,  GT = hubo acarreo y no es igual."
            },
        };
        return BuildChallenge(parent, "Reto 2.6 - La Balanza Digital", defs.Select(d => (System.Func<Transform, float>)(p => BuildCircuit(p, d))).ToArray());
    }

    // ================================================================ Especificaciones generadas

    private static string MuxSpec(string[] selects, int count)
    {
        var terms = new List<string>();
        int k = selects.Length;
        for (int i = 0; i < count; i++)
        {
            var lits = new List<string>();
            for (int j = 0; j < k; j++)
            {
                bool bit = ((i >> (k - 1 - j)) & 1) == 1;
                lits.Add(bit ? selects[j] : "!" + selects[j]);
            }
            lits.Add("I" + i);
            terms.Add(string.Join(" & ", lits));
        }
        return "Y = " + string.Join(" | ", terms);
    }

    private static string DecoderSpec(string[] address, string enable)
    {
        var sb = new StringBuilder();
        int k = address.Length;
        for (int i = 0; i < (1 << k); i++)
        {
            var lits = new List<string>();
            if (enable != null) lits.Add(enable);
            for (int j = 0; j < k; j++)
            {
                bool bit = ((i >> (k - 1 - j)) & 1) == 1;
                lits.Add(bit ? address[j] : "!" + address[j]);
            }
            sb.Append("Y").Append(i).Append(" = ").Append(string.Join(" & ", lits)).Append('\n');
        }
        return sb.ToString();
    }

    private static string AdderSpec(bool subtract)
    {
        var sb = new StringBuilder();
        if (subtract)
        {
            sb.AppendLine("# M = 1 resta: se invierte B y se suma 1 con el acarreo de entrada");
            for (int i = 0; i < 4; i++) sb.AppendLine($"X{i} = B{i} ^ M");
            sb.AppendLine("c0 = M");
        }
        else
        {
            for (int i = 0; i < 4; i++) sb.AppendLine($"X{i} = B{i}");
            sb.AppendLine("c0 = 0");
        }

        for (int i = 0; i < 4; i++)
        {
            sb.AppendLine($"S{i} = A{i} ^ X{i} ^ c{i}");
            sb.AppendLine($"c{i + 1} = A{i} & X{i} | c{i} & (A{i} ^ X{i})");
        }
        sb.AppendLine("C4 = c4");

        if (subtract)
        {
            sb.AppendLine("EQ = !(S0 | S1 | S2 | S3)");
            sb.AppendLine("LT = !C4");
            sb.AppendLine("GT = C4 & !EQ");
            sb.AppendLine("# El comparador solo se revisa en modo resta");
            sb.AppendLine("dc GT = !M");
            sb.AppendLine("dc EQ = !M");
            sb.AppendLine("dc LT = !M");
        }
        return sb.ToString();
    }

    private static string[] Names(string prefix, int count) => Enumerable.Range(0, count).Select(i => prefix + i).ToArray();

    private static List<LogicGateType> Gates(params (LogicGateType type, int count)[] items)
    {
        var list = new List<LogicGateType>();
        foreach (var (type, count) in items)
            for (int i = 0; i < count; i++) list.Add(type);
        return list;
    }

    // ================================================================ Tipos de definición

    private class CircuitDef
    {
        public string title, spec, instructions;
        public string[] inputs, outputs;
        public string[] constants = new string[0];
        public string[] liveTest = new string[0];
        public LogicGateType[] allowed = new LogicGateType[0];
        public List<LogicGateType> gates;
        public int slots, ports, pins, inputFanout, pinFanout;
        public Dictionary<string, int> fanoutOverrides = new Dictionary<string, int>();
    }

    private class TableDef
    {
        public string title, spec, description;
        public string[] inputs;
    }

    private class KMapDef
    {
        public string title, spec, given;
        public string[] vars;
    }

    // ================================================================ Reto (3 versiones)

    private static float BuildChallenge(Transform parent, string name, System.Func<Transform, float>[] versions)
    {
        var challenge = new GameObject(name);
        challenge.transform.SetParent(parent, false);

        var roots = new GameObject[versions.Length];
        float width = 0f;
        for (int i = 0; i < versions.Length; i++)
        {
            var v = new GameObject("V" + (i + 1));
            v.transform.SetParent(challenge.transform, false);
            width = Mathf.Max(width, versions[i](v.transform));
            v.SetActive(i == 0);
            roots[i] = v;
        }

        var manager = challenge.AddComponent<PuzzleVersionManager>();
        var so = new SerializedObject(manager);
        var arr = so.FindProperty("puzzleRoots");
        arr.arraySize = roots.Length;
        for (int i = 0; i < roots.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = roots[i];
        so.FindProperty("randomizeOnStart").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        return width;
    }

    // ================================================================ Constructores por tipo

    private const float TableTop = 0.85f;
    private const float PanelBottom = 0.95f;
    private const float ContentBottom = 1.02f;

    private static float BuildCircuit(Transform root, CircuitDef d)
    {
        const float rowH = 0.11f, plugStep = 0.055f, portStep = 0.065f;

        int maxInFanout = Mathf.Max(d.inputFanout, d.fanoutOverrides.Count > 0 ? d.fanoutOverrides.Values.Max() : 0);
        float inputsW = 0.3f + maxInFanout * plugStep;
        float slotW = 0.4f + d.pinFanout * plugStep;
        float slotH = Mathf.Max(d.ports * portStep, d.pins * 0.1f) + 0.12f;
        int cols = Mathf.Min(d.slots, Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(d.slots * 1.6f))));
        int rows = Mathf.CeilToInt(d.slots / (float)cols);
        float outputsW = 0.34f;
        float panelW = inputsW + cols * slotW + outputsW + 0.1f;

        int allInputs = d.inputs.Length + d.constants.Length;
        float contentH = Mathf.Max(allInputs * rowH, rows * slotH, d.outputs.Length * rowH) + 0.04f;
        float top = ContentBottom + contentH;
        float left = -panelW * 0.5f;

        var puzzle = root.gameObject.AddComponent<CircuitSpecPuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.spec = d.spec;
        puzzle.allowedGates = d.allowed;
        puzzle.liveTestInputs = d.liveTest;
        puzzle.instructions = d.instructions;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.instructionsText, out puzzle.progressText);

        var boardGo = new GameObject("Protoboard");
        boardGo.transform.SetParent(root, false);
        var board = boardGo.AddComponent<Breadboard>();
        puzzle.board = board;

        // ---- Entradas
        var inputs = new List<CircuitInput>();
        var allNames = d.inputs.Concat(d.constants).ToArray();
        for (int i = 0; i < allNames.Length; i++)
        {
            bool constant = i >= d.inputs.Length;
            float y = top - 0.06f - i * rowH;
            int fanout = d.fanoutOverrides.TryGetValue(allNames[i], out int f) ? f : d.inputFanout;
            inputs.Add(MakeInput(board.transform, allNames[i], constant, new Vector3(left + 0.06f, y, 0f), fanout, plugStep));
        }
        board.inputs = inputs.ToArray();

        // ---- Ranuras
        for (int s = 0; s < d.slots; s++)
        {
            int c = s % cols, r = s / cols;
            var pos = new Vector3(left + inputsW + c * slotW + 0.06f, top - 0.04f - r * slotH, 0f);
            MakeSlot(board.transform, s, pos, d.ports, d.pins, d.pinFanout, d.allowed, plugStep, portStep, slotH);
        }

        // ---- Salidas
        var outputs = new List<CircuitOutput>();
        for (int o = 0; o < d.outputs.Length; o++)
        {
            float y = top - 0.06f - o * rowH;
            outputs.Add(MakeOutput(board.transform, d.outputs[o], new Vector3(left + panelW - outputsW + 0.04f, y, 0f)));
        }
        board.outputs = outputs.ToArray();

        // ---- Mesa con compuertas y botón
        var table = Table(root, panelW);
        float itemStep = 0.15f;
        float itemsW = (d.gates.Count - 1) * itemStep;
        for (int g = 0; g < d.gates.Count; g++)
            MakeGate(table, d.gates[g], new Vector3(-itemsW * 0.5f - 0.1f + g * itemStep, 0.05f, -0.02f));

        if (d.liveTest.Length == 0)
            puzzle.submitButton = MakeSubmit(table, new Vector3(panelW * 0.5f - 0.15f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(table, "LuzResultado", new Vector3(panelW * 0.5f - 0.32f, 0.04f, 0f), 0.05f);

        return panelW;
    }

    private static float BuildTruthTable(Transform root, TableDef d)
    {
        int rows = 1 << d.inputs.Length;
        var cell = new Vector2(0.11f, 0.07f);
        float tableH = (rows + 1) * (cell.y + 0.012f);
        float panelW = Mathf.Max(1.6f, (d.inputs.Length + 1) * 0.122f + 1.0f);
        float top = ContentBottom + tableH;

        var puzzle = root.gameObject.AddComponent<TruthTablePuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.inputNames = d.inputs;
        puzzle.spec = d.spec;
        puzzle.outputName = "S";
        puzzle.circuitDescription = d.description;
        puzzle.cellSize = cell;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.circuitText, out _);

        var tableRoot = new GameObject("Tabla");
        tableRoot.transform.SetParent(root, false);
        tableRoot.transform.localPosition = new Vector3(0f, ContentBottom + tableH * 0.5f, -0.01f);
        puzzle.tableRoot = tableRoot.transform;

        var desk = Table(root, panelW);
        puzzle.submitButton = MakeSubmit(desk, new Vector3(0.2f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(desk, "LuzResultado", new Vector3(-0.05f, 0.04f, 0f), 0.05f);
        return panelW;
    }

    private static float BuildKMap(Transform root, KMapDef d)
    {
        int n = d.vars.Length;
        int mapRows = 1 << (n / 2);
        var cell = new Vector2(0.12f, 0.12f);
        float mapH = (mapRows + 2) * (cell.y + 0.012f) + 0.05f;
        float panelW = 2.4f;
        float top = ContentBottom + Mathf.Max(mapH, 0.75f);

        var puzzle = root.gameObject.AddComponent<KarnaughMapPuzzle>();
        puzzle.puzzleId = d.title.Split(' ')[0];
        puzzle.variables = d.vars;
        puzzle.spec = d.spec;
        puzzle.outputName = "F";
        puzzle.givenExpression = d.given;
        puzzle.cellSize = cell;

        Backdrop(root, panelW, PanelBottom, top + 0.5f);
        Texts(root, puzzle, d.title, panelW, top, out puzzle.expressionText, out _);

        var mapRoot = new GameObject("MapaK");
        mapRoot.transform.SetParent(root, false);
        mapRoot.transform.localPosition = new Vector3(-0.35f, top - mapH * 0.5f + 0.05f, -0.01f);
        puzzle.mapRoot = mapRoot.transform;

        puzzle.groupInfoText = Level2Visuals.CreateLabel(root, "InfoGrupos", "", new Vector3(-1.0f, top - 0.35f, -0.01f), new Vector2(0.38f, 0.6f), 0.3f);
        puzzle.groupInfoText.alignment = TextAlignmentOptions.TopLeft;

        var keypadGo = new GameObject("Teclado");
        keypadGo.transform.SetParent(root, false);
        keypadGo.transform.localPosition = new Vector3(0.65f, top - 0.45f, -0.01f);
        var keypad = keypadGo.AddComponent<ExpressionKeypad>();
        keypad.keys = d.vars.Concat(new[] { "'", "+", "(", ")" }).ToArray();
        keypad.display = Level2Visuals.CreateLabel(root, "Pantalla", "_", new Vector3(0.65f, top - 0.1f, -0.01f), new Vector2(0.55f, 0.12f), 0.6f);
        Prim(PrimitiveType.Cube, root, "FondoPantalla", new Vector3(0.65f, top - 0.1f, 0.005f), new Vector3(0.6f, 0.14f, 0.01f), Mat("L2_Pantalla", new Color(0.08f, 0.07f, 0.06f)));
        puzzle.keypad = keypad;

        var desk = Table(root, panelW);
        puzzle.submitButton = MakeSubmit(desk, new Vector3(0.65f, 0.02f, 0f));
        puzzle.resultLight = MakeLamp(desk, "LuzResultado", new Vector3(0.4f, 0.04f, 0f), 0.05f);
        return panelW;
    }

    // ================================================================ Piezas

    private static void Backdrop(Transform root, float width, float bottom, float top)
    {
        Prim(PrimitiveType.Cube, root, "Panel", new Vector3(0f, (bottom + top) * 0.5f, 0.04f),
            new Vector3(width, top - bottom, 0.04f), Mat("L2_Panel", new Color(0.42f, 0.34f, 0.22f)), true);
    }

    private static void Texts(Transform root, Level2Puzzle puzzle, string title, float width, float top,
                              out TextMeshPro info, out TextMeshPro progress)
    {
        Level2Visuals.CreateLabel(root, "Titulo", title, new Vector3(0f, top + 0.42f, -0.01f), new Vector2(width * 0.9f, 0.09f), 0.9f);
        info = Level2Visuals.CreateLabel(root, "Enunciado", "", new Vector3(0f, top + 0.27f, -0.01f), new Vector2(width * 0.9f, 0.18f), 0.45f);
        progress = Level2Visuals.CreateLabel(root, "Progreso", "", new Vector3(0f, top + 0.14f, -0.01f), new Vector2(width * 0.9f, 0.06f), 0.4f);
        puzzle.statusText = Level2Visuals.CreateLabel(root, "Estado", "", new Vector3(0f, top + 0.07f, -0.01f), new Vector2(width * 0.9f, 0.06f), 0.4f);
    }

    private static Transform Table(Transform root, float width)
    {
        var table = new GameObject("Mesa").transform;
        table.SetParent(root, false);
        table.localPosition = new Vector3(0f, TableTop, -0.42f);
        Prim(PrimitiveType.Cube, table, "Tablero", new Vector3(0f, -0.025f, 0f), new Vector3(Mathf.Max(1.2f, width), 0.05f, 0.4f),
            Mat("L2_Mesa", new Color(0.33f, 0.24f, 0.14f)), true);
        Prim(PrimitiveType.Cube, table, "Base", new Vector3(0f, -TableTop * 0.5f - 0.025f, 0f), new Vector3(Mathf.Max(1.0f, width - 0.2f), TableTop - 0.05f, 0.3f),
            Mat("L2_Mesa", new Color(0.33f, 0.24f, 0.14f)), true);
        return table;
    }

    private static CircuitInput MakeInput(Transform parent, string name, bool constant, Vector3 pos, int fanout, float plugStep)
    {
        var go = new GameObject("Entrada_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var input = go.AddComponent<CircuitInput>();
        input.displayName = name;
        input.isConstant = constant;
        input.fixedValue = false;

        Level2Visuals.CreateLabel(go.transform, "Nombre", constant ? name + " = 0" : name, new Vector3(0.02f, 0f, -0.01f), new Vector2(0.09f, 0.06f), 0.5f);

        if (!constant)
            input.sourceSwitch = MakeSwitch(go.transform, new Vector3(0.11f, 0f, -0.01f));

        var lamp = MakeLamp(go.transform, "Indicador", new Vector3(0.19f, 0f, -0.01f), 0.035f);
        input.indicator = lamp;
        input.cableAnchor = lamp.transform;

        for (int k = 0; k < fanout; k++)
            MakePlug(go.transform, input, new Vector3(0.25f + k * plugStep, 0f, -0.035f));

        return input;
    }

    private static void MakeSlot(Transform parent, int index, Vector3 pos, int ports, int pins, int pinFanout,
                                 LogicGateType[] allowed, float plugStep, float portStep, float slotH)
    {
        var go = new GameObject("Ranura_" + (index + 1));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var slot = go.AddComponent<GateSlot>();

        float midY = -slotH * 0.5f + 0.02f;
        Prim(PrimitiveType.Cube, go.transform, "Marco", new Vector3(0.17f, midY, 0.01f), new Vector3(0.34f, slotH - 0.04f, 0.01f),
            Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));

        slot.label = Level2Visuals.CreateLabel(go.transform, "Nombre", "vacío", new Vector3(0.17f, -0.02f, -0.01f), new Vector2(0.3f, 0.04f), 0.3f);

        // Socket de la compuerta
        var sockGo = new GameObject("SocketCompuerta");
        sockGo.transform.SetParent(go.transform, false);
        sockGo.transform.localPosition = new Vector3(0.17f, midY, -0.03f);
        var col = sockGo.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.05f;
        var sock = sockGo.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.LogicGate;
        sock.allowedGates = allowed;
        slot.gateSocket = sock;

        // Puertos de entrada (columna izquierda)
        slot.inputPorts = new WirePort[ports];
        float py0 = midY + (ports - 1) * portStep * 0.5f;
        for (int p = 0; p < ports; p++)
            slot.inputPorts[p] = MakePort(go.transform, "Entrada" + (p + 1), new Vector3(0.03f, py0 - p * portStep, -0.02f), "·");

        // Pines de salida (columna derecha) con sus cables
        slot.outputPins = new GateOutputPin[pins];
        float oy0 = midY + (pins - 1) * 0.1f * 0.5f;
        for (int o = 0; o < pins; o++)
        {
            var pinGo = new GameObject("Salida" + (o + 1));
            pinGo.transform.SetParent(go.transform, false);
            pinGo.transform.localPosition = new Vector3(0.31f, oy0 - o * 0.1f, -0.02f);
            var pin = pinGo.AddComponent<GateOutputPin>();
            pin.slot = slot;
            pin.outputIndex = o;
            pin.displayName = $"R{index + 1}.{o}";
            Prim(PrimitiveType.Sphere, pinGo.transform, "Pin", Vector3.zero, Vector3.one * 0.025f, Mat("L2_Pin", new Color(0.8f, 0.6f, 0.2f)));
            pin.label = Level2Visuals.CreateLabel(pinGo.transform, "Etiqueta", "·", new Vector3(0f, 0.03f, -0.01f), new Vector2(0.06f, 0.03f), 0.25f);

            for (int k = 0; k < pinFanout; k++)
                MakePlug(go.transform, pin, new Vector3(0.37f + k * plugStep, oy0 - o * 0.1f, -0.035f));
            slot.outputPins[o] = pin;
        }
    }

    private static WirePort MakePort(Transform parent, string name, Vector3 pos, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.022f;
        var sock = go.AddComponent<Level2Socket>();
        sock.accept = Level2Socket.Accept.WirePlug;
        var port = go.AddComponent<WirePort>();

        Prim(PrimitiveType.Cylinder, go.transform, "Conector", new Vector3(0f, 0f, 0.012f), new Vector3(0.03f, 0.008f, 0.03f),
            Mat("L2_Conector", new Color(0.1f, 0.09f, 0.08f)), false, Quaternion.Euler(90f, 0f, 0f));
        port.label = Level2Visuals.CreateLabel(go.transform, "Etiqueta", label, new Vector3(-0.045f, 0f, -0.005f), new Vector2(0.05f, 0.03f), 0.25f);
        port.portName = label;
        return port;
    }

    private static CircuitOutput MakeOutput(Transform parent, string name, Vector3 pos)
    {
        var go = new GameObject("Salida_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var output = go.AddComponent<CircuitOutput>();
        output.displayName = name;
        output.port = MakePort(go.transform, "Puerto", Vector3.zero, "");
        output.bulb = MakeLamp(go.transform, "Bombilla", new Vector3(0.09f, 0f, -0.01f), 0.06f);

        var lightGo = new GameObject("Luz");
        lightGo.transform.SetParent(go.transform, false);
        lightGo.transform.localPosition = new Vector3(0.09f, 0f, -0.08f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 0.35f;
        light.intensity = 1.5f;
        light.color = new Color(1f, 0.8f, 0.4f);
        light.enabled = false;
        output.bulbLight = light;

        Level2Visuals.CreateLabel(go.transform, "Nombre", name, new Vector3(0.2f, 0f, -0.01f), new Vector2(0.12f, 0.06f), 0.5f);
        return output;
    }

    private static XRSwitchToggle MakeSwitch(Transform parent, Vector3 pos)
    {
        var go = new GameObject("Switch");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.06f, 0.09f, 0.05f);
        go.AddComponent<XRSimpleInteractable>();

        Prim(PrimitiveType.Cube, go.transform, "Base", new Vector3(0f, -0.03f, 0f), new Vector3(0.05f, 0.02f, 0.04f), Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));
        var pivot = new GameObject("Pivote");
        pivot.transform.SetParent(go.transform, false);
        pivot.transform.localPosition = new Vector3(0f, -0.025f, 0f);
        Prim(PrimitiveType.Cube, pivot.transform, "Palanca", new Vector3(0f, 0.035f, 0f), new Vector3(0.012f, 0.07f, 0.012f), Mat("L2_Palanca", new Color(0.75f, 0.6f, 0.3f)));

        var toggle = go.AddComponent<XRSwitchToggle>();
        var so = new SerializedObject(toggle);
        so.FindProperty("switchMesh").objectReferenceValue = pivot.transform;
        so.FindProperty("offRotation").vector3Value = new Vector3(0f, 0f, 30f);
        so.FindProperty("onRotation").vector3Value = new Vector3(0f, 0f, -30f);
        so.ApplyModifiedPropertiesWithoutUndo();
        return toggle;
    }

    private static WirePlug MakePlug(Transform parent, SignalNode source, Vector3 pos)
    {
        var go = new GameObject("Cable_" + source.displayName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.018f;
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        var tip = Prim(PrimitiveType.Cylinder, go.transform, "Punta", Vector3.zero, new Vector3(0.022f, 0.012f, 0.022f),
            Mat("L2_Enchufe", new Color(0.4f, 0.4f, 0.4f)), false, Quaternion.Euler(90f, 0f, 0f));

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;

        var plug = go.AddComponent<WirePlug>();
        plug.source = source;
        plug.tip = tip.GetComponent<Renderer>();
        plug.lineMaterial = CableMaterial();
        go.AddComponent<ReturnHome>();
        return plug;
    }

    private static void MakeGate(Transform table, LogicGateType type, Vector3 pos)
    {
        var go = new GameObject("Compuerta_" + type);
        go.transform.SetParent(table, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.11f, 0.07f, 0.04f);
        var rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Prim(PrimitiveType.Cube, go.transform, "Cuerpo", Vector3.zero, new Vector3(0.11f, 0.07f, 0.04f), GateMaterial(type));
        Level2Visuals.CreateLabel(go.transform, "Nombre", GateSlot.Pretty(type), new Vector3(0f, 0f, -0.022f), new Vector2(0.1f, 0.06f), 0.35f);

        var item = go.AddComponent<LogicGateItem>();
        var so = new SerializedObject(item);
        so.FindProperty("gateType").intValue = (int)type;
        so.ApplyModifiedPropertiesWithoutUndo();

        var grab = go.AddComponent<XRGrabInteractable>();
        grab.throwOnDetach = false;
        go.AddComponent<ReturnHome>();
    }

    private static XRPhysicalButton MakeSubmit(Transform parent, Vector3 pos)
    {
        var go = new GameObject("BotonComprobar");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.05f, 0.1f);
        col.center = new Vector3(0f, 0.02f, 0f);
        go.AddComponent<XRSimpleInteractable>();

        Prim(PrimitiveType.Cylinder, go.transform, "Base", new Vector3(0f, 0.005f, 0f), new Vector3(0.13f, 0.01f, 0.13f), Mat("L2_Marco", new Color(0.22f, 0.17f, 0.11f)));
        var cap = Prim(PrimitiveType.Cylinder, go.transform, "Tapa", new Vector3(0f, 0.022f, 0f), new Vector3(0.09f, 0.012f, 0.09f), Mat("L2_Boton", new Color(0.75f, 0.15f, 0.1f)));
        var label = Level2Visuals.CreateLabel(go.transform, "Texto", "Comprobar", new Vector3(0f, 0.09f, 0.08f), new Vector2(0.2f, 0.05f), 0.35f);
        label.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

        var button = go.AddComponent<XRPhysicalButton>();
        var so = new SerializedObject(button);
        so.FindProperty("buttonMesh").objectReferenceValue = cap.transform;
        so.FindProperty("pressDistance").floatValue = 0.01f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return button;
    }

    private static Renderer MakeLamp(Transform parent, string name, Vector3 pos, float size)
    {
        return Prim(PrimitiveType.Sphere, parent, name, pos, Vector3.one * size, Mat("L2_Lampara", new Color(0.15f, 0.12f, 0.1f))).GetComponent<Renderer>();
    }

    // ================================================================ Utilidades

    private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material mat,
                                   bool keepCollider = false, Quaternion? rot = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (rot.HasValue) go.transform.localRotation = rot.Value;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    private static Material Mat(string name, Color color)
    {
        EnsureFolder();
        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CableMaterial()
    {
        EnsureFolder();
        string path = $"{MaterialFolder}/L2_Cable.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Sprites/Default")) { name = "L2_Cable" };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material GateMaterial(LogicGateType type)
    {
        // Reutiliza los materiales de compuertas del Nivel 1 cuando existen
        var existing = AssetDatabase.LoadAssetAtPath<Material>($"Assets/EgyptianLogic/Materials/Gates/{type}.mat");
        if (existing != null) return existing;

        switch (type)
        {
            case LogicGateType.NAND: return Mat("L2_NAND", new Color(0.55f, 0.2f, 0.6f));
            case LogicGateType.NOR: return Mat("L2_NOR", new Color(0.2f, 0.5f, 0.55f));
            case LogicGateType.XNOR: return Mat("L2_XNOR", new Color(0.5f, 0.5f, 0.2f));
            case LogicGateType.MUX2: return Mat("L2_MUX2", new Color(0.2f, 0.35f, 0.7f));
            case LogicGateType.MUX4: return Mat("L2_MUX4", new Color(0.15f, 0.25f, 0.55f));
            case LogicGateType.HALF_ADDER: return Mat("L2_HalfAdder", new Color(0.6f, 0.45f, 0.15f));
            case LogicGateType.FULL_ADDER: return Mat("L2_FullAdder", new Color(0.7f, 0.35f, 0.1f));
            default: return Mat("L2_Gate_" + type, new Color(0.5f, 0.5f, 0.5f));
        }
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(MaterialFolder)) return;
        if (!AssetDatabase.IsValidFolder("Assets/EgyptianLogic/Materials"))
            AssetDatabase.CreateFolder("Assets/EgyptianLogic", "Materials");
        AssetDatabase.CreateFolder("Assets/EgyptianLogic/Materials", "Level2");
    }
}
