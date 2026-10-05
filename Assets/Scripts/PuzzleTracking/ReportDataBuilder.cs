using System;
using UnityEngine;

public static class ReportDataBuilder
{
    public static ReportData Build(PuzzleTracker tracker, SkillCalculator skillCalc,
                                    DimensionWeightMatrix matrix, string playerName, string playerEmail)
    {
        var report = new ReportData
        {
            nombreJugador = playerName,
            correoJugador = playerEmail,
            fecha = DateTime.Now.ToString("dd/MM/yyyy"),
            hora = DateTime.Now.ToString("HH:mm")
        };

        foreach (var def in tracker.AllDefinitions)
        {
            var data = tracker.GetRuntime(def.puzzleId);
            float score = ScoringFormula.CalculateScorePercent(def, data);
            float efectividad = data.completed ? 100f : 0f;

            int minutes = Mathf.FloorToInt(data.elapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(data.elapsedSeconds % 60f);

            report.pruebas.Add(new ReportPruebaRow
            {
                nombrePuzzle = def.displayName,
                tiempoDisplay = $"{minutes}:{seconds:00}",
                efectividadPercent = efectividad,
                eficienciaPercent = score
            });
        }

        report.puntajeHabilidadTotal = skillCalc.GetOverallSkillPercent();

        for (int i = 0; i < matrix.dimensionNames.Count; i++)
        {
            string desc = i < matrix.dimensionDescriptions.Count ? matrix.dimensionDescriptions[i] : "";
            report.dimensiones.Add(new ReportDimensionRow
            {
                nombre = matrix.dimensionNames[i],
                descripcion = desc,
                percent = skillCalc.GetDimensionPercent(matrix.dimensionNames[i])
            });
        }

        return report;
    }
}