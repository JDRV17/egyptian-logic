using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if !SCHEM_TEST
using TMPro;
#endif

/// <summary>
/// Dibujo de "explicación" de cada enunciado del Nivel 2, distinto en cada reto y en cada dificultad.
/// Se describe con un texto (campo <c>drawing</c> de cada versión) y se dibuja en un papiro junto al panel:
/// <list type="bullet">
/// <item>Circuito con símbolos de compuertas: <c>"S = AND(OR(A, B), NAND(A, B))"</c>. Varias salidas se separan
/// con ';' (<c>"S = XOR(A, B); C = AND(A, B)"</c>). Compuertas de 1 a 5 entradas; <c>A'</c> es una entrada negada.</item>
/// <item>Bloque: <c>"[MUX] MUX 4:1 | I0, I1, I2, I3 | S1, S0 | Y"</c> → forma, título | entradas a la izquierda |
/// entradas abajo (selección, enable…) | salidas a la derecha. Formas: MUX (trapecio) o BOX (rectángulo).</item>
/// </list>
/// Las líneas van en una textura y las letras son textos encima, para que se lean nítidas en el visor.
/// </summary>
public static partial class Level2Builder
{
    // Medidas del esquema en metros (antes de escalarlo para que quepa)
    private const float GateH = 0.1f, GateW = 0.11f, NotW = 0.08f, GateGapX = 0.09f, RowStep = 0.085f, PinStep = 0.045f;
    private const float SchemMargin = 0.075f, LeafX = 0.075f, OutWire = 0.07f, Bubble = 0.01f, XorGap = 0.022f;
    private const float BlockPinPitch = 0.06f, BlockStub = 0.06f;
    private const float SchemPixelsPerMeter = 1300f, SchemStroke = 0.0021f;
    /// <summary>Alto máximo del papiro en el panel; los dibujos más altos se achican.</summary>
    private const float MaxDrawingH = 0.85f;

    private static readonly Color Ink = new Color(0.16f, 0.1f, 0.06f);
    private static readonly Color Papyrus = new Color(0.93f, 0.87f, 0.72f);
    private static readonly Color InputBlue = new Color(0.12f, 0.27f, 0.55f);
    private static readonly Color OutputOrange = new Color(0.6f, 0.36f, 0.05f);
    private static readonly Color GateGrey = new Color(0.42f, 0.38f, 0.33f);

    private sealed class SchemNode
    {
        public string type;        // AND, OR, …; null = entrada
        public string variable;    // solo en las entradas
        public readonly List<SchemNode> inputs = new List<SchemNode>();
        public int depth;
        public float x0, y, outX, h, w;  // y crece hacia abajo
    }

    /// <summary>Dibujo ya distribuido: tamaño, trazos (distancia a cada línea) y textos.</summary>
    private sealed class Schematic
    {
        public Vector2 size;
        public readonly List<System.Func<Vector2, float>> strokes = new List<System.Func<Vector2, float>>();
        public readonly List<(string text, Vector2 pos, Vector2 box, Color color, float font)> labels =
            new List<(string, Vector2, Vector2, Color, float)>();
    }

    private static Schematic LayoutDrawing(string drawing) =>
        drawing.TrimStart().StartsWith("[") ? LayoutBlock(drawing) : LayoutCircuit(drawing);

    // ================================================================ Circuitos con compuertas

    private sealed class RowCursor
    {
        public float y = SchemMargin;
        public SchemNode lastParent;
        public int count;
    }

    /// <summary>Compuertas en columnas según su profundidad; cada uso de una entrada tiene su propia fila.</summary>
    private static Schematic LayoutCircuit(string text)
    {
        var s = new Schematic();
        var outputs = new List<(string name, SchemNode root)>();
        foreach (var statement in text.Split(';', '\n').Select(t => t.Trim()).Where(t => t.Length > 0))
        {
            int eq = statement.IndexOf('=');
            string name = eq > 0 ? statement.Substring(0, eq).Trim() : "S";
            string expr = eq > 0 ? statement.Substring(eq + 1) : statement;
            int pos = 0;
            outputs.Add((name, ParseSchem(expr, ref pos)));
        }

        var rows = new RowCursor();
        foreach (var o in outputs) Place(o.root, null, rows);

        var all = outputs.SelectMany(o => Walk(o.root)).ToList();
        foreach (var leaf in all.Where(n => n.type == null))
        {
            leaf.outX = LeafX;
            s.labels.Add((leaf.variable, new Vector2(LeafX * 0.45f, leaf.y), new Vector2(0.06f, 0.05f), InputBlue, 0.5f));
        }

        // Columnas con el ancho de su compuerta más ancha
        var gates = all.Where(n => n.type != null).ToList();
        int maxDepth = gates.Count > 0 ? gates.Max(g => g.depth) : 0;
        var colX = new float[maxDepth + 2];
        colX[1] = LeafX + GateGapX;
        for (int d = 2; d <= maxDepth; d++)
            colX[d] = colX[d - 1] + gates.Where(g => g.depth == d - 1).Select(g => g.w + 2f * Bubble).DefaultIfEmpty(GateW).Max() + GateGapX;

        // De izquierda a derecha: cada compuerta se dibuja después de las que la alimentan (ya tienen su salida)
        foreach (var g in gates.OrderBy(n => n.depth))
        {
            g.x0 = colX[g.depth];
            AddGate(s, g);
            for (int i = 0; i < g.inputs.Count; i++)
            {
                var child = g.inputs[i];
                AddWire(s, new Vector2(child.outX, child.y), InputPin(g, i), g.x0 - GateGapX * 0.45f);
            }
        }

        // Salidas alineadas a la derecha
        float endX = outputs.Max(o => o.root.outX) + OutWire;
        foreach (var o in outputs)
        {
            AddSegment(s, new Vector2(o.root.outX, o.root.y), new Vector2(endX, o.root.y));
            s.labels.Add((o.name, new Vector2(endX + 0.03f, o.root.y), new Vector2(0.06f, 0.05f), OutputOrange, 0.5f));
        }

        s.size = new Vector2(endX + 0.075f, rows.y + SchemMargin);
        return s;
    }

    private static void Place(SchemNode n, SchemNode parent, RowCursor rows)
    {
        if (n.type == null)
        {
            // Las entradas de una misma compuerta grande van juntas, alineadas con sus patas
            if (rows.count > 0)
                rows.y += parent != null && parent == rows.lastParent && parent.inputs.Count >= 3 ? PinStep : RowStep;
            n.y = rows.y;
            rows.lastParent = parent;
            rows.count++;
            return;
        }

        int d = 0;
        foreach (var c in n.inputs)
        {
            Place(c, n, rows);
            d = Mathf.Max(d, c.depth);
        }
        n.depth = d + 1;
        n.y = n.inputs.Average(c => c.y);
        int pins = n.inputs.Count;
        n.h = pins <= 2 ? GateH : Mathf.Max(GateH, pins * PinStep + 0.02f);
        // La AND necesita un tramo recto antes del semicírculo; la OR se alarga con su altura
        n.w = n.type == "NOT" ? NotW : IsOrShape(n.type) ? Mathf.Max(GateW, n.h * 0.8f) : Mathf.Max(GateW, n.h * 0.5f + 0.07f);
    }

    private static IEnumerable<SchemNode> Walk(SchemNode n)
    {
        yield return n;
        foreach (var c in n.inputs)
            foreach (var x in Walk(c)) yield return x;
    }

    private static bool IsOrShape(string t) => t == "OR" || t == "NOR" || t == "XOR" || t == "XNOR";
    private static bool HasBubble(string t) => t == "NOT" || t == "NAND" || t == "NOR" || t == "XNOR";

    // Arco trasero cóncavo de las compuertas tipo OR: círculo que pasa por las dos esquinas y entra "bulge" al centro
    private static void OrBack(SchemNode g, out Vector2 center, out float radius)
    {
        float bulge = g.w * 0.2f, half = g.h * 0.5f;
        float e = (half * half - bulge * bulge) / (2f * bulge);
        center = new Vector2(g.x0 - e, g.y);
        radius = e + bulge;
    }

    private static Vector2 InputPin(SchemNode g, int index)
    {
        int n = g.inputs.Count;
        float y = n == 1 ? g.y
                : n == 2 ? g.y + (index == 0 ? -0.25f : 0.25f) * g.h
                : g.y + (index - (n - 1) * 0.5f) * PinStep;
        if (!IsOrShape(g.type)) return new Vector2(g.x0, y);
        OrBack(g, out var c, out float r);
        if (g.type == "XOR" || g.type == "XNOR") c.x -= XorGap;   // en la XOR el cable llega al arco de atrás
        return new Vector2(c.x + Mathf.Sqrt(Mathf.Max(0f, r * r - (y - c.y) * (y - c.y))), y);
    }

    private static void AddGate(Schematic s, SchemNode g)
    {
        float half = g.h * 0.5f, x0 = g.x0, cy = g.y, body = g.w;
        float tip = x0 + body;

        if (g.type == "NOT")
        {
            // Triángulo: lado izquierdo y dos lados que llegan a la punta
            Vector2 a = new Vector2(x0, cy - half), b = new Vector2(x0, cy + half), t = new Vector2(tip, cy);
            s.strokes.Add(p => Mathf.Abs(Mathf.Max(x0 - p.x, Mathf.Max(SideDist(p, a, t), SideDist(p, t, b)))));
        }
        else if (IsOrShape(g.type))
        {
            // Dos arcos delanteros que se juntan en la punta y el arco trasero cóncavo
            float R = (body * body + half * half) / g.h;
            Vector2 top = new Vector2(x0, cy + half - R), bottom = new Vector2(x0, cy - half + R);
            OrBack(g, out var bc, out float br);
            s.strokes.Add(p => Mathf.Abs(Mathf.Max(Mathf.Max((p - top).magnitude - R, (p - bottom).magnitude - R),
                                                   Mathf.Max(br - (p - bc).magnitude, x0 - p.x))));
            if (g.type == "XOR" || g.type == "XNOR")
            {
                // Segundo arco trasero (solo la parte derecha del círculo, entre las esquinas)
                var bc2 = bc - new Vector2(XorGap, 0f);
                s.strokes.Add(p => Mathf.Abs(p.y - cy) <= half && p.x > bc2.x ? Mathf.Abs((p - bc2).magnitude - br) : 1f);
            }
        }
        else
        {
            // AND / NAND: lado izquierdo, dos lados rectos y un semicírculo a la derecha
            var arcC = new Vector2(x0 + body - half, cy);
            Vector2 tl = new Vector2(x0, cy - half), bl = new Vector2(x0, cy + half);
            AddSegment(s, tl, bl);
            AddSegment(s, tl, new Vector2(arcC.x, cy - half));
            AddSegment(s, bl, new Vector2(arcC.x, cy + half));
            s.strokes.Add(p => p.x >= arcC.x ? Mathf.Abs((p - arcC).magnitude - half) : 1f);
        }

        g.outX = tip;
        if (HasBubble(g.type))
        {
            var bc = new Vector2(tip + Bubble, cy);
            s.strokes.Add(p => Mathf.Abs((p - bc).magnitude - Bubble));
            g.outX = tip + 2f * Bubble;
        }

        // Nombre dentro de la compuerta, en gris como en los diagramas escolares
        bool xor = g.type == "XOR" || g.type == "XNOR";
        float labelX = g.type == "NOT" ? x0 + body * 0.36f : x0 + body * (xor ? 0.56f : IsOrShape(g.type) ? 0.5f : 0.42f);
        float labelW = g.type == "NOT" ? body * 0.6f : body * (xor ? 0.58f : 0.72f);
        s.labels.Add((g.type, new Vector2(labelX, cy), new Vector2(labelW, GateH * 0.4f), GateGrey, 0.3f));
    }

    /// <summary>Cable en ángulo recto: horizontal, vertical en <paramref name="bendX"/> y horizontal hasta la entrada.</summary>
    private static void AddWire(Schematic s, Vector2 from, Vector2 to, float bendX)
    {
        if (Mathf.Abs(from.y - to.y) < 1e-4f)
        {
            AddSegment(s, from, to);
            return;
        }
        bendX = Mathf.Clamp(bendX, from.x, to.x);
        AddSegment(s, from, new Vector2(bendX, from.y));
        AddSegment(s, new Vector2(bendX, from.y), new Vector2(bendX, to.y));
        AddSegment(s, new Vector2(bendX, to.y), to);
    }

    // "AND(OR(A,B), NOT(C'))" → árbol
    private static SchemNode ParseSchem(string text, ref int pos)
    {
        SkipSpaces(text, ref pos);
        int start = pos;
        while (pos < text.Length && char.IsLetterOrDigit(text[pos])) pos++;
        string name = text.Substring(start, pos - start);
        if (name.Length == 0) throw new System.FormatException($"Dibujo inválido cerca de la posición {start}: «{text}»");
        SkipSpaces(text, ref pos);
        if (pos < text.Length && text[pos] == '\'')
        {
            pos++;
            return new SchemNode { variable = name + "'" };
        }
        if (pos >= text.Length || text[pos] != '(') return new SchemNode { variable = name };

        var node = new SchemNode { type = name.ToUpperInvariant() };
        pos++;
        while (true)
        {
            node.inputs.Add(ParseSchem(text, ref pos));
            SkipSpaces(text, ref pos);
            if (pos < text.Length && text[pos] == ',') { pos++; continue; }
            if (pos < text.Length && text[pos] == ')') { pos++; break; }
            throw new System.FormatException($"Falta ')' en «{text}»");
        }
        if (node.inputs.Count > 5 || (node.type == "NOT" && node.inputs.Count != 1))
            throw new System.FormatException($"{node.type} con {node.inputs.Count} entradas no se puede dibujar: «{text}»");
        return node;
    }

    private static void SkipSpaces(string t, ref int pos)
    {
        while (pos < t.Length && char.IsWhiteSpace(t[pos])) pos++;
    }

    // ================================================================ Bloques (MUX, decodificador, sumador…)

    /// <summary>"[MUX] MUX 4:1 | I0, I1 | S1, S0 | Y": forma, título | izquierda | abajo | derecha.</summary>
    private static Schematic LayoutBlock(string text)
    {
        var s = new Schematic();
        int open = text.IndexOf('['), close = text.IndexOf(']');
        string shape = text.Substring(open + 1, close - open - 1).Trim().ToUpperInvariant();
        var parts = text.Substring(close + 1).Split('|');
        string[] Pins(int i) => i < parts.Length
            ? parts[i].Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray()
            : new string[0];
        string title = parts[0].Trim();
        string[] left = Pins(1), bottom = Pins(2), right = Pins(3);
        float LabelW(string[] pins) => pins.Select(p => p.Length).DefaultIfEmpty(1).Max() * 0.022f + 0.03f;

        bool mux = shape == "MUX";
        float bodyH = Mathf.Max(Mathf.Max(left.Length, right.Length) * BlockPinPitch + 0.08f, 0.22f);
        float bodyW = Mathf.Max(mux ? 0.24f : 0.34f, bottom.Length * 0.09f + 0.08f);
        float leftW = LabelW(left), rightW = LabelW(right);
        float bx = SchemMargin * 0.5f + leftW + BlockStub, top = SchemMargin;
        float inset = mux ? bodyH * 0.18f : 0f;

        // Contorno (en el MUX el lado derecho es más corto: trapecio)
        var tl = new Vector2(bx, top);
        var bl = new Vector2(bx, top + bodyH);
        var tr = new Vector2(bx + bodyW, top + inset);
        var br = new Vector2(bx + bodyW, top + bodyH - inset);
        AddSegment(s, tl, tr);
        AddSegment(s, tr, br);
        AddSegment(s, br, bl);
        AddSegment(s, bl, tl);

        for (int i = 0; i < left.Length; i++)
        {
            float y = top + bodyH * (i + 0.5f) / left.Length;
            AddSegment(s, new Vector2(bx - BlockStub, y), new Vector2(bx, y));
            s.labels.Add((left[i], new Vector2(bx - BlockStub - leftW * 0.5f, y), new Vector2(leftW, 0.045f), InputBlue, 0.45f));
        }

        float rx = bx + bodyW;
        for (int i = 0; i < right.Length; i++)
        {
            float y = top + inset + (bodyH - 2f * inset) * (i + 0.5f) / right.Length;
            AddSegment(s, new Vector2(rx, y), new Vector2(rx + BlockStub, y));
            s.labels.Add((right[i], new Vector2(rx + BlockStub + rightW * 0.5f, y), new Vector2(rightW, 0.045f), OutputOrange, 0.45f));
        }

        float bottomExtra = 0f;
        for (int i = 0; i < bottom.Length; i++)
        {
            float x = bx + bodyW * (i + 0.5f) / bottom.Length;
            float edge = top + bodyH - inset * (x - bx) / bodyW;   // borde de abajo (inclinado en el MUX)
            AddSegment(s, new Vector2(x, edge), new Vector2(x, edge + BlockStub));
            float w = bottom[i].Length * 0.022f + 0.03f;
            s.labels.Add((bottom[i], new Vector2(x, edge + BlockStub + 0.03f), new Vector2(w, 0.045f), InputBlue, 0.45f));
            bottomExtra = Mathf.Max(bottomExtra, edge + BlockStub + 0.06f - (top + bodyH));
        }

        s.labels.Add((title, new Vector2(bx + bodyW * 0.5f, top + bodyH * 0.5f),
            new Vector2(bodyW * 0.85f, Mathf.Min(0.16f, bodyH * 0.5f)), Ink, 0.5f));
        s.size = new Vector2(rx + BlockStub + rightW + SchemMargin * 0.5f, top + bodyH + bottomExtra + SchemMargin);
        return s;
    }

    // ================================================================ Dibujo

    private static void AddSegment(Schematic s, Vector2 a, Vector2 b) => s.strokes.Add(p => SegmentDist(p, a, b));

    private static float SegmentDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-8f, ab.sqrMagnitude));
        return (p - (a + ab * t)).magnitude;
    }

    /// <summary>Distancia con signo a la recta a→b; positiva hacia afuera del triángulo de la NOT.</summary>
    private static float SideDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(d.y, -d.x);
        return Vector2.Dot(p - a, n);
    }

    private static float SdBox(Vector2 p, Vector2 half)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x) - half.x, Mathf.Abs(p.y) - half.y);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
    }

    /// <summary>Color del papiro en (u, v) (0..1, v hacia arriba): tinta donde pasa una línea y un marco fino.</summary>
    private static Color SchematicPixel(Schematic s, float u, float v)
    {
        var p = new Vector2(u * s.size.x, (1f - v) * s.size.y);   // y hacia abajo, como en el esquema
        float d = float.MaxValue;
        foreach (var f in s.strokes) d = Mathf.Min(d, f(p));

        float frame = Mathf.Min(Mathf.Min(p.x, s.size.x - p.x), Mathf.Min(p.y, s.size.y - p.y));
        d = Mathf.Min(d, Mathf.Abs(frame - 0.008f));

        Color paper = Papyrus * (0.95f + 0.07f * Hash(Mathf.Floor(u * 160f), Mathf.Floor(v * 90f)));
        return Color.Lerp(Ink, paper, Mathf.Clamp01((d - SchemStroke) * SchemPixelsPerMeter + 0.5f));
    }

#if !SCHEM_TEST
    /// <summary>
    /// Agrega el dibujo de explicación a la izquierda del panel: corre el contenido a la derecha, ensancha el fondo
    /// (y lo alarga si el dibujo no cabe) y pone el papiro a la altura de los ojos. Devuelve el nuevo ancho del panel.
    /// </summary>
    private static float AddExplanation(Transform root, string drawing, string id, float panelW, float top)
    {
        var s = LayoutDrawing(drawing);
        float scale = Mathf.Min(1f, MaxDrawingH / s.size.y);
        Vector2 shown = s.size * scale;
        float extra = shown.x + 0.15f;

        // Todo lo del panel se corre a la derecha (las piezas del suelo se quedan donde están)
        foreach (Transform child in root)
            if (child.name != "PiezasSuelo") child.localPosition += new Vector3(extra * 0.5f, 0f, 0f);

        float panelTop = top + 0.5f, half = shown.y * 0.5f;
        float cy = Mathf.Clamp(1.55f, ContentBottom + half, Mathf.Max(ContentBottom + half, panelTop - 0.05f - half));
        float newTop = Mathf.Max(panelTop, cy + half + 0.05f);
        var panel = root.Find("Panel");
        if (panel != null)
        {
            panel.localPosition = new Vector3(0f, (PanelBottom + newTop) * 0.5f, panel.localPosition.z);
            panel.localScale = new Vector3(panelW + extra, newTop - PanelBottom, panel.localScale.z);
        }

        PlaceSchematic(root, s, id, new Vector3(-panelW * 0.5f, cy, -0.006f), scale);
        return panelW + extra;
    }

    /// <summary>Pone el papiro con las líneas y los textos encima. <paramref name="center"/> es su centro en el panel.</summary>
    private static void PlaceSchematic(Transform root, Schematic s, string id, Vector3 center, float scale)
    {
        int w = Mathf.CeilToInt(s.size.x * SchemPixelsPerMeter), h = Mathf.CeilToInt(s.size.y * SchemPixelsPerMeter);
        var tex = GeneratedTexture("L2_Explicacion_" + id, w, h, (u, v) => SchematicPixel(s, u, v));

        Vector2 shown = s.size * scale;
        Prim(PrimitiveType.Quad, root, "Explicacion", center, new Vector3(shown.x, shown.y, 1f),
            TexturedMat("L2_Explicacion_" + id, tex, Vector2.one, Color.white, 0.05f, 0f));
        var labels = new GameObject("TextosExplicacion").transform;
        labels.SetParent(root, false);
        labels.localPosition = center;

        // Del esquema (origen arriba a la izquierda, y hacia abajo) al panel (origen al centro, y hacia arriba)
        foreach (var (text, pos, box, color, font) in s.labels)
        {
            var local = new Vector3((pos.x - s.size.x * 0.5f) * scale, (s.size.y * 0.5f - pos.y) * scale, -0.004f);
            var label = Level2Visuals.CreateLabel(labels, "Texto_" + text, text, local, box * scale, font * scale);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.enableWordWrapping = text.Contains(" ");
        }
    }
#endif
}
