using UnityEngine;

public class SkillCalculator : MonoBehaviour
{
    public static SkillCalculator Instance { get; private set; }

    public PuzzleTracker tracker;
    public DimensionWeightMatrix matrix;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // No necesita DontDestroyOnLoad propio si vive en el mismo GameObject
        // que PuzzleTracker (que ya tiene DontDestroyOnLoad).
    }

    public float GetDimensionPercent(string dimensionName)
    {
        float totalPoints = 0f;

        foreach (var def in tracker.AllDefinitions)
        {
            float weight = matrix.GetWeight(def.puzzleId, dimensionName);
            if (weight <= 0f) continue;

            var data = tracker.GetRuntime(def.puzzleId);
            float score01 = ScoringFormula.CalculateScore01(def, data);

            totalPoints += score01 * weight;
        }

        return Mathf.Clamp(totalPoints, 0f, 100f);
    }

    public float GetOverallSkillPercent()
    {
        if (matrix.dimensionNames.Count == 0) return 0f;
        float sum = 0f;
        foreach (var dim in matrix.dimensionNames)
            sum += GetDimensionPercent(dim);
        return sum / matrix.dimensionNames.Count;
    }
}