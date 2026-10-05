using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class DebugTrackerDisplay : MonoBehaviour
{
    [Tooltip("Arrastra aquí el Text de la tablet que quieres usar para debug.")]
    public Text targetText;

    public PuzzleTracker tracker;
    public SkillCalculator skillCalculator;
    public DimensionWeightMatrix matrix;

    [Tooltip("Cada cuántos segundos se refresca el texto.")]
    public float refreshInterval = 0.5f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;

        if (targetText == null || tracker == null) return;

        var sb = new StringBuilder();
        sb.AppendLine("=== DESEMPEÑO (DEBUG) ===");

        foreach (var def in tracker.AllDefinitions)
        {
            var data = tracker.GetRuntime(def.puzzleId);
            float score01 = ScoringFormula.CalculateScore01(def, data);
            bool active = tracker.IsActive(def.puzzleId);

            string estado = data.completed ? "COMPLETO" : (data.started ? "en curso" : "sin iniciar");
            string activo = active ? " <- ACTIVO" : "";

            sb.AppendLine($"{def.displayName}: {estado} | {score01 * 100f:0}% | " +
                          $"T={data.elapsedSeconds:0.0}s | Int={data.interactions}{activo}");
        }

        if (skillCalculator != null && matrix != null)
        {
            sb.AppendLine();
            sb.AppendLine("=== HABILIDADES (DEBUG) ===");
            foreach (var dim in matrix.dimensionNames)
            {
                float pct = skillCalculator.GetDimensionPercent(dim);
                sb.AppendLine($"{dim}: {pct:0}%");
            }
            sb.AppendLine($"TOTAL: {skillCalculator.GetOverallSkillPercent():0}%");
        }

        targetText.text = sb.ToString();
    }
}