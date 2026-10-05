using UnityEngine;

public static class ReportPdfGenerator
{
    // Paleta tomada de tu plantilla LaTeX
    private const string ColorTextPrimary = "222222";
    private const string ColorCardBg = "F8F9FA";
    private const string ColorCardBorder = "D1D5DB";
    private const string ColorAccentBlue = "1D4ED8";
    private const string ColorSubtext = "6B7280";

    public static byte[] GenerateBytes(ReportData report)
    {
        var pdf = new MinimalPdfWriter();
        float pageWidth = 595f;
        float leftMargin = 50f;
        float rightMargin = 545f;
        float y = 780f;

        // ---------- Encabezado ----------
        DrawCentered(pdf, pageWidth, y, "INFORME DE RETROALIMENTACIÓN", 18, bold: true, color: ColorTextPrimary); y -= 24;
        DrawCentered(pdf, pageWidth, y, $"[{report.nombreJuego}]", 14, bold: true, color: ColorAccentBlue); y -= 34;

        pdf.DrawText(leftMargin, y, $"Nombre: {report.nombreJugador}", 11, colorHex: ColorTextPrimary); y -= 16;
        pdf.DrawText(leftMargin, y, $"Correo: {report.correoJugador}", 11, colorHex: ColorTextPrimary); y -= 16;
        pdf.DrawText(leftMargin, y, $"Fecha: {report.fecha}", 11, colorHex: ColorTextPrimary); y -= 16;
        pdf.DrawText(leftMargin, y, $"Hora: {report.hora}", 11, colorHex: ColorTextPrimary); y -= 26;

        string intro = $"Este informe presenta indicadores relacionados al uso del software {report.nombreJuego}, " +
                        $"una experiencia en realidad virtual que tiene como propósito el entrenamiento y evaluación " +
                        $"de habilidades de {report.habilidadEvaluada} valiosas para desempeñarse en contextos de industrias " +
                        $"4.0 y empresas de tecnología.";
        y = DrawWrappedText(pdf, intro, leftMargin, y, 9, 110, 60, ColorSubtext);
        y -= 24;

        // ---------- DESEMPEÑO EN EL JUEGO ----------
        pdf.DrawText(leftMargin, y, "DESEMPEÑO EN EL JUEGO", 14, bold: true, colorHex: ColorTextPrimary); y -= 20;

        float tiempoTotalSeg = 0f;
        float sumaEfectividad = 0f;
        float sumaEficiencia = 0f;
        foreach (var p in report.pruebas)
        {
            sumaEfectividad += p.efectividadPercent;
            sumaEficiencia += p.eficienciaPercent;
        }
        int nPruebas = report.pruebas.Count > 0 ? report.pruebas.Count : 1;
        float efectividadGeneral = sumaEfectividad / nPruebas;
        float eficienciaGeneral = sumaEficiencia / nPruebas;

        foreach (var p in report.pruebas)
        {
            var parts = p.tiempoDisplay.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int mm) && int.TryParse(parts[1], out int ss))
                tiempoTotalSeg += mm * 60 + ss;
        }
        int totalMin = Mathf.FloorToInt(tiempoTotalSeg / 60f);
        int totalSec = Mathf.FloorToInt(tiempoTotalSeg % 60f);
        string tiempoTotalDisplay = $"{totalMin}:{totalSec:00}";

        // 3 tarjetas KPI
        float cardW = 150f, cardH = 55f, cardGap = 15f;
        float cardsStartX = leftMargin;
        DrawKpiCard(pdf, cardsStartX, y, cardW, cardH, "TIEMPO GENERAL", tiempoTotalDisplay);
        DrawKpiCard(pdf, cardsStartX + cardW + cardGap, y, cardW, cardH, "EFECTIVIDAD GENERAL", $"{efectividadGeneral:0}%");
        DrawKpiCard(pdf, cardsStartX + (cardW + cardGap) * 2, y, cardW, cardH, "EFICIENCIA GENERAL", $"{eficienciaGeneral:0}%");
        y -= (cardH + 26);

        // Tabla de pruebas
        float col1 = leftMargin, col2 = leftMargin + 150, col3 = leftMargin + 260, col4 = leftMargin + 380;
        float rowH = 20f;

        pdf.DrawRect(leftMargin, y - 2, rightMargin - leftMargin, rowH, fillHex: ColorCardBg, borderHex: ColorCardBorder);
        pdf.DrawText(col1 + 4, y + 3, "PRUEBA", 9, bold: true, colorHex: ColorTextPrimary);
        pdf.DrawText(col2 + 4, y + 3, "TIEMPO", 9, bold: true, colorHex: ColorTextPrimary);
        pdf.DrawText(col3 + 4, y + 3, "EFECTIVIDAD", 9, bold: true, colorHex: ColorTextPrimary);
        pdf.DrawText(col4 + 4, y + 3, "EFICIENCIA", 9, bold: true, colorHex: ColorTextPrimary);
        y -= rowH;

        foreach (var p in report.pruebas)
        {
            pdf.DrawRect(leftMargin, y - 2, rightMargin - leftMargin, rowH, borderHex: ColorCardBorder);
            pdf.DrawText(col1 + 4, y + 3, p.nombrePuzzle, 9, colorHex: ColorTextPrimary);
            pdf.DrawText(col2 + 4, y + 3, p.tiempoDisplay, 9, colorHex: ColorTextPrimary);
            pdf.DrawText(col3 + 4, y + 3, $"{p.efectividadPercent:0}%", 9, colorHex: ColorTextPrimary);
            pdf.DrawText(col4 + 4, y + 3, $"{p.eficienciaPercent:0}%", 9, colorHex: ColorTextPrimary);
            y -= rowH;

            if (y < 100) { pdf.NewPage(); y = 780f; }
        }

        DrawFooterNote(pdf, leftMargin, 60f);

        // ---------- PÁGINA 2: Evaluación de habilidad ----------
        pdf.NewPage();
        y = 780f;

        pdf.DrawText(leftMargin, y, "EVALUACIÓN DE HABILIDAD", 14, bold: true, colorHex: ColorTextPrimary); y -= 20;

        string skillIntro = $"Estos indicadores dan cuenta del nivel estimado de la habilidad {report.habilidadEvaluada} " +
                             $"del jugador, a partir de su interacción y desempeño con el software. Esta habilidad es " +
                             $"definida como {report.definicionHabilidad}";
        y = DrawWrappedText(pdf, skillIntro, leftMargin, y, 9, 110, 60, ColorSubtext);
        y -= 26;

        // Tarjeta grande de puntaje
        float bigCardW = 170f, bigCardH = 60f;
        float bigCardX = leftMargin + (rightMargin - leftMargin - bigCardW) / 2f;
        pdf.DrawRect(bigCardX, y - bigCardH, bigCardW, bigCardH, fillHex: ColorCardBg, borderHex: ColorCardBorder);
        DrawCentered(pdf, pageWidth, y - 24, $"{report.puntajeHabilidadTotal:0} / 100", 22, bold: true, color: ColorTextPrimary);
        DrawCentered(pdf, pageWidth, y - 44, "PUNTAJE HABILIDAD", 9, bold: true, color: ColorTextPrimary);
        y -= (bigCardH + 30);

        foreach (var d in report.dimensiones)
        {
            if (y < 100) { pdf.NewPage(); y = 780f; }

            pdf.DrawText(leftMargin, y, $"{d.percent:0}%", 12, bold: true, colorHex: ColorAccentBlue);
            pdf.DrawText(leftMargin + 55, y, d.nombre, 12, bold: true, colorHex: ColorTextPrimary);
            y -= 15;

            if (!string.IsNullOrEmpty(d.descripcion))
                y = DrawWrappedText(pdf, d.descripcion, leftMargin, y, 9, 100, 60, ColorSubtext);

            y -= 12;
        }

        DrawFooterNote(pdf, leftMargin, 60f);

        return pdf.GetBytes();
    }

    public static void GenerateAndSave(ReportData report, string outputPath)
    {
        byte[] bytes = GenerateBytes(report);
        System.IO.File.WriteAllBytes(outputPath, bytes);
        UnityEngine.Debug.Log($"Reporte generado en: {outputPath}");
    }

    private static void DrawKpiCard(MinimalPdfWriter pdf, float x, float y, float w, float h, string label, string value)
    {
        pdf.DrawRect(x, y - h, w, h, fillHex: ColorCardBg, borderHex: ColorCardBorder);
        pdf.DrawText(x + 10, y - 16, label, 8, bold: true, colorHex: ColorTextPrimary);
        pdf.DrawText(x + 10, y - 38, value, 16, bold: true, colorHex: ColorTextPrimary);
    }

    // Centrado aproximado (Helvetica ~0.5*size de ancho promedio por carácter)
    private static void DrawCentered(MinimalPdfWriter pdf, float pageWidth, float y, string text, float size, bool bold, string color)
    {
        float approxWidth = text.Length * size * 0.5f;
        float x = (pageWidth - approxWidth) / 2f;
        pdf.DrawText(x, y, text, size, bold, color);
    }

    private static float DrawWrappedText(MinimalPdfWriter pdf, string text, float x, float y, float fontSize, int maxCharsPerLine, float pageBottom, string colorHex)
    {
        if (string.IsNullOrEmpty(text)) return y;

        string[] words = text.Split(' ');
        string line = "";

        foreach (var word in words)
        {
            string testLine = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (testLine.Length > maxCharsPerLine)
            {
                pdf.DrawText(x, y, line, fontSize, colorHex: colorHex);
                y -= (fontSize + 4);
                line = word;
                if (y < pageBottom) { pdf.NewPage(); y = 780f; }
            }
            else line = testLine;
        }

        if (!string.IsNullOrEmpty(line))
        {
            pdf.DrawText(x, y, line, fontSize, colorHex: colorHex);
            y -= (fontSize + 4);
        }

        return y;
    }

    private static void DrawFooterNote(MinimalPdfWriter pdf, float x, float y)
    {
        string note = "Nota: Es importante tener en cuenta que este informe de evaluación no constituye una " +
                       "categorización taxativa del perfil de un evaluado, es un insumo valioso que puede ser " +
                       "complementado con otras estrategias de evaluación.";
        DrawWrappedText(pdf, note, x, y, 7, 130, 20, ColorSubtext);
    }
}