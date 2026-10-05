using UnityEngine;

public class GenerarReporteButton : MonoBehaviour
{
    public byte[] GenerarReportePdf(string nombreJugador, string correoJugador, out string fileName)
    {
        fileName = null;
        var tracker = PuzzleTracker.Instance;
        var skillCalculator = SkillCalculator.Instance;

        if (tracker == null || skillCalculator == null)
        {
            Debug.LogError("No se encontró PuzzleTracker o SkillCalculator.");
            return null;
        }

        var report = ReportDataBuilder.Build(tracker, skillCalculator, skillCalculator.matrix, nombreJugador, correoJugador);
        byte[] pdfBytes = ReportPdfGenerator.GenerateBytes(report);
        fileName = $"reporte_{System.DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return pdfBytes;
    }
}