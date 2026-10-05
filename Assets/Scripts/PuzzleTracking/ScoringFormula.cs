using UnityEngine;

public static class ScoringFormula
{
    private const float A1_Resultado = 0.30f;
    private const float A2_Dificultad = 0.20f;
    private const float A3_Pistas = 0.10f;
    private const float A4_A5_ErroresInteracciones = 0.20f;

    private const float A1_SinIdeal = 0.40f;
    private const float A2_SinIdeal = 0.20f;
    private const float A3_SinIdeal = 0.10f;
    private const float A6_SinIdeal = 0.30f;

    private const float A6_ConIdeal = 0.20f;

    /// <summary>Devuelve el puntaje del puzzle en escala 0.0 - 1.0.</summary>
    public static float CalculateScore01(PuzzleDefinition def, PuzzleRuntimeData data)
    {
        if (def == null || data == null || !data.started) return 0f;

        float R = data.completed ? 1f : 0f;
        float D = def.difficulty;
        float P = data.hintUsed ? 0f : 1f;
        float T = CalculateTimeScore(def, data);

        if (def.hasIdealInteractions)
        {
            float EI = CalculateErrorInteractionScore(def, data);
            float score = (R * A1_Resultado) + (D * A2_Dificultad) + (P * A3_Pistas)
                         + (EI * A4_A5_ErroresInteracciones) + (T * A6_ConIdeal);
            return Mathf.Clamp01(score);
        }
        else
        {
            float score = (R * A1_SinIdeal) + (D * A2_SinIdeal) + (P * A3_SinIdeal) + (T * A6_SinIdeal);
            return Mathf.Clamp01(score);
        }
    }

    public static float CalculateScorePercent(PuzzleDefinition def, PuzzleRuntimeData data)
        => CalculateScore01(def, data) * 100f;

    private static float CalculateErrorInteractionScore(PuzzleDefinition def, PuzzleRuntimeData data)
    {
        int errors = data.GetErrorsCount(def);
        return 1f / (1f + errors);
    }

    private static float CalculateTimeScore(PuzzleDefinition def, PuzzleRuntimeData data)
    {
        if (data.elapsedSeconds <= 0f) return 0f;
        if (data.elapsedSeconds <= def.idealTimeSeconds) return 1f;
        return Mathf.Clamp01(def.idealTimeSeconds / data.elapsedSeconds);
    }

    public static int GetErrorsCount(PuzzleDefinition def, PuzzleRuntimeData data)
        => data.GetErrorsCount(def);
}