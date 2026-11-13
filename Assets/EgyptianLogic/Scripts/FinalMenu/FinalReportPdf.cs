using System.Collections.Generic;
using System.Text;

/// <summary>
/// Arma el PDF del informe: sobrio, todo en negro, en Times (o Helvetica) y A4.
/// Solo usa caracteres de Latin-1: las rayas y comillas tipográficas se cambian por sus versiones simples.
/// </summary>
public static class FinalReportPdf
{
    private const string Black = "000000";
    private const float Left = 60f, Right = 535f, Top = 790f, Bottom = 45f;
    private const float ValueX = 190f;   // columna de valores en el resumen y por nivel

    public static string FileName(ReportModel m) =>
        $"informe_egyptian_logic_{m.date:yyyyMMdd_HHmm}.pdf";

    public static byte[] Build(ReportModel m, string email, PdfFont font = PdfFont.Times)
    {
        var pdf = new FinalPdfWriter(font);
        float y = Top;

        void Need(float h)
        {
            if (y - h >= Bottom) return;
            pdf.NewPage();
            y = Top;
        }

        void Line(string text, float size = 11f, bool bold = false, float x = Left)
        {
            Need(size + 4f);
            pdf.Text(x, y, Clean(text), size, bold);
            y -= size + 5f;
        }

        void Pair(string label, string value)
        {
            Need(16f);
            pdf.Text(Left, y, Clean(label), 11f);
            pdf.Text(ValueX, y, Clean(value), 11f);
            y -= 16f;
        }

        void Wrap(string text, float x, float size, bool bold = false)
        {
            int perLine = (int)((Right - x) / (size * pdf.AverageCharWidth));
            foreach (var line in WrapLines(Clean(text), perLine)) Line(line, size, bold, x);
        }

        void Section(string title)
        {
            Need(44f);
            y -= 6f;
            pdf.Text(Left, y, Clean(title), 13f, true);
            y -= 6f;
            pdf.Line(Left, y, Right, 0.5f);
            y -= 16f;
        }

        // ---------- Encabezado
        pdf.Text(Left, y, Clean("Informe de desempeño"), 20f, true); y -= 20f;
        pdf.Text(Left, y, "Egyptian Logic", 12f); y -= 26f;

        Pair("Jugador:", string.IsNullOrWhiteSpace(m.playerName) ? "-" : m.playerName);
        if (!string.IsNullOrWhiteSpace(email)) Pair("Correo:", email);
        Pair("Fecha:", m.date.ToString("dd/MM/yyyy HH:mm"));

        if (!m.hasData)
        {
            Section("Resumen");
            Line("No se registraron retos en esta partida.");
            return pdf.GetBytes();
        }

        // ---------- Resumen
        Section("Resumen");
        Pair("Desempeño general:", $"{FinalReport.Score100(m.overallP)}/100 ({FinalReport.MasteryLabel(m.overallMastery)})");
        Pair("Nivel sugerido:", m.suggestedLevel);
        Pair("Retos jugados:", m.stagesPlayed.ToString());
        Pair("Tiempo total:", FinalReport.FormatTime(m.totalTime));

        // ---------- Por nivel
        Section("Desempeño por nivel");
        foreach (var l in m.levels)
        {
            Need(16f);
            pdf.Text(Left, y, Clean(l.name), 11f);
            pdf.Text(Right - 70f, y, $"{FinalReport.Score100(l.meanP)}/100", 11f);
            y -= 16f;
        }

        // ---------- Lo que más costó
        Section("Lo que más te costó");
        if (m.hard.Count == 0)
        {
            Wrap("Ningún reto se te resistió. ¡Buen trabajo!", Left, 11f);
        }
        else
        {
            foreach (var h in m.hard)
            {
                Need(50f);
                Line($"{h.name} ({FinalReport.Score100(h.result.P)}/100)", 11f, true);
                if (!string.IsNullOrWhiteSpace(h.topic)) Wrap("Tema: " + h.topic, Left + 12f, 10.5f);
                Wrap(h.reasons, Left + 12f, 10.5f);
                y -= 3f;
            }
        }

        // ---------- Temas
        Section("Temas para repasar");
        if (m.topicsToReview.Count == 0)
            Wrap("No quedan temas pendientes. Puedes volver a jugar para intentar las versiones más difíciles.", Left, 11f);
        else
            foreach (var t in m.topicsToReview) Wrap("- " + t, Left, 11f);

        // ---------- Tabla de retos
        Section("Detalle por reto");
        Wrap("El puntaje de cada reto combina los intentos, las pistas, el tiempo y los movimientos. " +
             "La versión (V1 a V3) es la dificultad que el juego te asignó según tu desempeño. " +
             "Este informe es un apoyo para tu estudio y no reemplaza la evaluación del curso.", Left, 9.5f);
        y -= 6f;

        float[] col = { Left, Left + 40, Left + 285, Left + 335, Left + 385, Left + 430 };
        string[] head = { "Reto", "Tema", "Versión", "Puntaje", "Intentos", "Pistas" };
        void Header()
        {
            Need(36f);
            for (int i = 0; i < head.Length; i++) pdf.Text(col[i], y, Clean(head[i]), 10f, true);
            y -= 6f;
            pdf.Line(Left, y, Right, 0.5f);
            y -= 13f;
        }
        Header();
        foreach (var s in m.stages)
        {
            if (y - 14f < Bottom) { pdf.NewPage(); y = Top; Header(); }
            var r = s.result;
            string code = s.topic != null ? s.topic.code : $"{r.level}.{r.stageIndex + 1}";
            string topic = s.topic != null ? s.topic.topic : r.stageName;
            pdf.Text(col[0], y, Clean(code), 10f);
            pdf.Text(col[1], y, Clean(Trim(topic, 50)), 10f);
            pdf.Text(col[2], y, $"V{r.version + 1}", 10f);
            pdf.Text(col[3], y, $"{FinalReport.Score100(r.P)}", 10f);
            pdf.Text(col[4], y, $"{r.attempts}", 10f);
            pdf.Text(col[5], y, r.cappedBySolution ? Clean($"{r.hints} (solución)") : $"{r.hints}", 10f);
            y -= 14f;
        }
        pdf.Line(Left, y + 9f, Right, 0.5f);

        return pdf.GetBytes();
    }

    public static List<string> WrapLines(string text, int perLine)
    {
        var lines = new List<string>();
        if (perLine < 10) perLine = 10;
        var sb = new StringBuilder();
        foreach (var word in (text ?? "").Split(' '))
        {
            if (sb.Length > 0 && sb.Length + 1 + word.Length > perLine)
            {
                lines.Add(sb.ToString());
                sb.Clear();
            }
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(word);
        }
        if (sb.Length > 0) lines.Add(sb.ToString());
        return lines;
    }

    /// <summary>Deja solo caracteres que Helvetica/WinAnsi puede dibujar desde Latin-1.</summary>
    public static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            switch (c)
            {
                case '—': case '–': sb.Append('-'); break;
                case '“': case '”': sb.Append('"'); break;
                case '‘': case '’': sb.Append('\''); break;
                case '…': sb.Append("..."); break;
                case '→': sb.Append("->"); break;
                case '•': sb.Append('-'); break;
                case 'θ': sb.Append("theta"); break;
                default: sb.Append(c <= 'ÿ' ? c : '?'); break;
            }
        }
        return sb.ToString();
    }

    private static string Trim(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 3) + "...";

}
