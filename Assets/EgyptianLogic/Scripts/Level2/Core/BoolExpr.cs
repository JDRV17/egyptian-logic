using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Expresión booleana compilada. Sintaxis aceptada:
///   NOT : !A   ~A   A'          (la comilla es posfija, como en los mapas de Karnaugh)
///   AND : A&B  A*B  A·B  A.B  AB (yuxtaposición, ej. A'B' o A(B+C))
///   OR  : A|B  A+B
///   XOR : A^B
///   Constantes 0 y 1, paréntesis, y funciones NOT(..) AND(..) OR(..) XOR(..) NAND(..) NOR(..) XNOR(..)
/// Precedencia (de menor a mayor): OR, XOR, AND, NOT.
/// Con singleLetterVariables = true cada letra es una variable (AB = A AND B); si no, los nombres
/// pueden tener varias letras y dígitos (A0, Cin, S1) y la yuxtaposición necesita espacio o paréntesis.
/// </summary>
public sealed class BoolExpr
{
    private enum Op { Var, Const, Not, And, Or, Xor, Nand, Nor, Xnor }

    private sealed class Node
    {
        public Op op;
        public string name;
        public bool value;
        public List<Node> args;
    }

    private readonly Node root;
    private readonly HashSet<string> variables = new HashSet<string>();

    /// <summary>Texto original.</summary>
    public string Source { get; }

    /// <summary>Variables que aparecen en la expresión.</summary>
    public IEnumerable<string> Variables => variables;

    /// <summary>Número de apariciones de variables (literales). A'B' + CD = 4.</summary>
    public int LiteralCount { get; }

    private BoolExpr(string source, Node root)
    {
        Source = source;
        this.root = root;
        LiteralCount = CountLiterals(root, variables);
    }

    public static BoolExpr Parse(string text, bool singleLetterVariables = false)
    {
        var tokens = Tokenize(text, singleLetterVariables);
        var p = new Parser(tokens, text);
        var node = p.ParseExpression();
        p.ExpectEnd();
        return new BoolExpr(text, node);
    }

    public static bool TryParse(string text, bool singleLetterVariables, out BoolExpr expr, out string error)
    {
        try
        {
            expr = Parse(text, singleLetterVariables);
            error = null;
            return true;
        }
        catch (FormatException e)
        {
            expr = null;
            error = e.Message;
            return false;
        }
    }

    /// <summary>Evalúa con los valores de <paramref name="env"/>. Lanza KeyNotFoundException si falta una variable.</summary>
    public bool Evaluate(IDictionary<string, bool> env) => Eval(root, env);

    private static bool Eval(Node n, IDictionary<string, bool> env)
    {
        switch (n.op)
        {
            case Op.Var:
                if (!env.TryGetValue(n.name, out bool v))
                    throw new KeyNotFoundException($"La variable '{n.name}' no tiene valor.");
                return v;
            case Op.Const: return n.value;
            case Op.Not: return !Eval(n.args[0], env);
            case Op.And:
                foreach (var a in n.args) if (!Eval(a, env)) return false;
                return true;
            case Op.Or:
                foreach (var a in n.args) if (Eval(a, env)) return true;
                return false;
            case Op.Xor:
            {
                bool r = false;
                foreach (var a in n.args) r ^= Eval(a, env);
                return r;
            }
            case Op.Nand: return !Eval(new Node { op = Op.And, args = n.args }, env);
            case Op.Nor: return !Eval(new Node { op = Op.Or, args = n.args }, env);
            case Op.Xnor: return !Eval(new Node { op = Op.Xor, args = n.args }, env);
            default: return false;
        }
    }

    private static int CountLiterals(Node n, HashSet<string> vars)
    {
        if (n.op == Op.Var) { vars.Add(n.name); return 1; }
        if (n.args == null) return 0;
        int c = 0;
        foreach (var a in n.args) c += CountLiterals(a, vars);
        return c;
    }

    public override string ToString() => Source;

    // ---------------------------------------------------------------- Tokenizer

    private enum Tok { Ident, Const, Not, Prime, And, Or, Xor, LParen, RParen, Comma, End }

    private struct Token
    {
        public Tok type;
        public string text;
        public int pos;
    }

    private static List<Token> Tokenize(string s, bool singleLetter)
    {
        var list = new List<Token>();
        if (s == null) s = "";
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            Tok t;
            switch (c)
            {
                case '!': case '~': case '¬': t = Tok.Not; break;
                case '\'': case '’': case '´': t = Tok.Prime; break;
                case '&': case '*': case '·': case '.': case '∧': t = Tok.And; break;
                case '|': case '+': case '∨': t = Tok.Or; break;
                case '^': case '⊕': t = Tok.Xor; break;
                case '(': t = Tok.LParen; break;
                case ')': t = Tok.RParen; break;
                case ',': t = Tok.Comma; break;
                case '0': case '1':
                    if (singleLetter || i + 1 >= s.Length || !char.IsLetterOrDigit(s[i + 1]))
                    {
                        list.Add(new Token { type = Tok.Const, text = c.ToString(), pos = i });
                        i++;
                        continue;
                    }
                    throw new FormatException($"Símbolo inesperado '{c}' en la posición {i + 1}.");
                default:
                    if (char.IsLetter(c) || c == '_')
                    {
                        int start = i;
                        i++;
                        if (!singleLetter)
                            while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                        list.Add(new Token { type = Tok.Ident, text = s.Substring(start, i - start), pos = start });
                        continue;
                    }
                    throw new FormatException($"Símbolo inesperado '{c}' en la posición {i + 1}.");
            }

            list.Add(new Token { type = t, text = c.ToString(), pos = i });
            i++;
        }
        list.Add(new Token { type = Tok.End, text = "", pos = s.Length });
        return list;
    }

    // ---------------------------------------------------------------- Parser

    private sealed class Parser
    {
        private readonly List<Token> tokens;
        private readonly string source;
        private int index;

        public Parser(List<Token> tokens, string source)
        {
            this.tokens = tokens;
            this.source = source;
        }

        private Token Peek => tokens[index];
        private Token Next() => tokens[index++];

        private FormatException Error(string msg) =>
            new FormatException($"{msg} (posición {Peek.pos + 1} en \"{source}\").");

        public void ExpectEnd()
        {
            if (Peek.type != Tok.End) throw Error($"Sobra '{Peek.text}'");
        }

        public Node ParseExpression() => ParseOr();

        private Node ParseOr()
        {
            var left = ParseXor();
            if (Peek.type != Tok.Or) return left;
            var n = new Node { op = Op.Or, args = new List<Node> { left } };
            while (Peek.type == Tok.Or) { Next(); n.args.Add(ParseXor()); }
            return n;
        }

        private Node ParseXor()
        {
            var left = ParseAnd();
            if (Peek.type != Tok.Xor) return left;
            var n = new Node { op = Op.Xor, args = new List<Node> { left } };
            while (Peek.type == Tok.Xor) { Next(); n.args.Add(ParseAnd()); }
            return n;
        }

        private bool StartsPrimary(Tok t) => t == Tok.Ident || t == Tok.Const || t == Tok.LParen || t == Tok.Not;

        private Node ParseAnd()
        {
            var left = ParseUnary();
            if (Peek.type != Tok.And && !StartsPrimary(Peek.type)) return left;

            var n = new Node { op = Op.And, args = new List<Node> { left } };
            while (true)
            {
                if (Peek.type == Tok.And) { Next(); n.args.Add(ParseUnary()); }
                else if (StartsPrimary(Peek.type)) n.args.Add(ParseUnary()); // yuxtaposición
                else break;
            }
            return n;
        }

        private Node ParseUnary()
        {
            if (Peek.type == Tok.Not)
            {
                Next();
                return new Node { op = Op.Not, args = new List<Node> { ParseUnary() } };
            }

            var n = ParsePrimary();
            while (Peek.type == Tok.Prime)
            {
                Next();
                n = new Node { op = Op.Not, args = new List<Node> { n } };
            }
            return n;
        }

        private Node ParsePrimary()
        {
            var t = Peek;
            switch (t.type)
            {
                case Tok.Const:
                    Next();
                    return new Node { op = Op.Const, value = t.text == "1" };

                case Tok.LParen:
                {
                    Next();
                    var inner = ParseOr();
                    if (Peek.type != Tok.RParen) throw Error("Falta ')'");
                    Next();
                    return inner;
                }

                case Tok.Ident:
                {
                    Next();
                    if (Peek.type == Tok.LParen && TryFunction(t.text, out Op fop))
                    {
                        Next();
                        var args = new List<Node> { ParseOr() };
                        while (Peek.type == Tok.Comma) { Next(); args.Add(ParseOr()); }
                        if (Peek.type != Tok.RParen) throw Error("Falta ')'");
                        Next();
                        if (fop == Op.Not && args.Count != 1) throw Error("NOT recibe un solo argumento");
                        return new Node { op = fop, args = args };
                    }
                    return new Node { op = Op.Var, name = t.text };
                }

                case Tok.End:
                    throw Error("La expresión está incompleta");

                default:
                    throw Error($"Se esperaba una variable y llegó '{t.text}'");
            }
        }

        private static bool TryFunction(string name, out Op op)
        {
            switch (name.ToUpperInvariant())
            {
                case "NOT": op = Op.Not; return true;
                case "AND": op = Op.And; return true;
                case "OR": op = Op.Or; return true;
                case "XOR": op = Op.Xor; return true;
                case "NAND": op = Op.Nand; return true;
                case "NOR": op = Op.Nor; return true;
                case "XNOR": op = Op.Xnor; return true;
                default: op = Op.Var; return false;
            }
        }
    }
}

/// <summary>
/// Especificación de un circuito como lista de asignaciones, una por línea (o separadas por ';'):
/// <code>
/// c1 = A0 &amp; B0          # intermedio
/// S1 = A1 ^ B1 ^ c1     # salida
/// dc GT = !M            # GT no importa cuando M = 0 ("X" / don't care)
/// </code>
/// Las líneas se evalúan en orden, así que un nombre puede usarse en las líneas siguientes.
/// '#' o '//' inician un comentario.
/// </summary>
public sealed class LogicSpec
{
    private sealed class Statement
    {
        public string target;
        public BoolExpr expr;
        public bool dontCare;
    }

    private readonly List<Statement> statements = new List<Statement>();
    private readonly HashSet<string> assigned = new HashSet<string>();
    private readonly HashSet<string> freeVariables = new HashSet<string>();

    /// <summary>Variables que la especificación usa sin definirlas: deben ser entradas.</summary>
    public IEnumerable<string> FreeVariables => freeVariables;

    public bool Defines(string name) => assigned.Contains(name);

    public static LogicSpec Parse(string text, bool singleLetterVariables = false)
    {
        var spec = new LogicSpec();
        var lines = (text ?? "").Replace("\r", "").Split('\n', ';');

        for (int li = 0; li < lines.Length; li++)
        {
            string line = StripComment(lines[li]).Trim();
            if (line.Length == 0) continue;

            bool dc = false;
            if (line.StartsWith("dc ", StringComparison.OrdinalIgnoreCase))
            {
                dc = true;
                line = line.Substring(3).Trim();
            }

            int eq = line.IndexOf('=');
            if (eq <= 0) throw new FormatException($"Línea '{lines[li].Trim()}': se esperaba 'NOMBRE = expresión'.");

            string target = line.Substring(0, eq).Trim();
            string body = line.Substring(eq + 1).Trim();
            if (target.Length == 0) throw new FormatException($"Línea '{lines[li].Trim()}': falta el nombre antes de '='.");

            var expr = BoolExpr.Parse(body, singleLetterVariables);
            foreach (var v in expr.Variables)
                if (!spec.assigned.Contains(v)) spec.freeVariables.Add(v);

            spec.statements.Add(new Statement { target = target, expr = expr, dontCare = dc });
            if (!dc) spec.assigned.Add(target);
        }

        return spec;
    }

    public static bool TryParse(string text, bool singleLetterVariables, out LogicSpec spec, out string error)
    {
        try
        {
            spec = Parse(text, singleLetterVariables);
            error = null;
            return true;
        }
        catch (FormatException e)
        {
            spec = null;
            error = e.Message;
            return false;
        }
    }

    private static string StripComment(string line)
    {
        int h = line.IndexOf('#');
        if (h >= 0) line = line.Substring(0, h);
        int s = line.IndexOf("//", StringComparison.Ordinal);
        if (s >= 0) line = line.Substring(0, s);
        return line;
    }

    /// <summary>
    /// Evalúa todas las asignaciones. <paramref name="env"/> debe traer las entradas y se completa con
    /// los valores calculados. <paramref name="dontCares"/> (opcional) recibe los nombres que no importan en esta fila.
    /// </summary>
    public void Evaluate(Dictionary<string, bool> env, HashSet<string> dontCares = null)
    {
        dontCares?.Clear();
        foreach (var st in statements)
        {
            if (st.dontCare)
            {
                if (dontCares != null && st.expr.Evaluate(env)) dontCares.Add(st.target);
            }
            else
            {
                env[st.target] = st.expr.Evaluate(env);
            }
        }
    }

    /// <summary>Texto legible de la asignación de <paramref name="target"/> (para carteles).</summary>
    public string DescribeOutput(string target)
    {
        var sb = new StringBuilder();
        foreach (var st in statements)
            if (!st.dontCare && st.target == target) sb.Append(st.target).Append(" = ").Append(st.expr.Source);
        return sb.ToString();
    }
}

/// <summary>Utilidades para recorrer tablas de verdad. La primera variable es el bit más significativo.</summary>
public static class TruthTableUtil
{
    public static bool Bit(int row, int index, int count) => ((row >> (count - 1 - index)) & 1) == 1;

    public static void FillEnv(Dictionary<string, bool> env, IList<string> names, int row)
    {
        for (int i = 0; i < names.Count; i++)
            env[names[i]] = Bit(row, i, names.Count);
    }

    public static string RowLabel(int row, int count)
    {
        var sb = new StringBuilder(count);
        for (int i = 0; i < count; i++) sb.Append(Bit(row, i, count) ? '1' : '0');
        return sb.ToString();
    }
}
