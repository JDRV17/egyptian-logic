using UnityEngine;
using UnityEngine.UI;

public class TotalProgressController : MonoBehaviour
{
    [Tooltip("Barra de relleno del bloque 'PROGRESO TOTAL' (arriba, compartido en ambas pestañas).")]
    public Image totalBarFill;

    [Tooltip("Texto 'XX/XX' del bloque 'PROGRESO TOTAL'.")]
    public Text totalPercentText;

    private PuzzleTracker tracker;

    void Start()
    {
        tracker = PuzzleTracker.Instance;
    }

    void Update()
    {
        if (tracker == null) return;

        float totalScore01 = GetOverallProgress01();

        if (totalBarFill != null)
            totalBarFill.fillAmount = totalScore01;

        if (totalPercentText != null)
            totalPercentText.text = $"{Mathf.RoundToInt(totalScore01 * 100f)}/100";
    }

    private float GetOverallProgress01()
    {
        var defs = tracker.AllDefinitions;
        if (defs.Count == 0) return 0f;

        float sum = 0f;
        foreach (var def in defs)
        {
            var data = tracker.GetRuntime(def.puzzleId);
            sum += ScoringFormula.CalculateScore01(def, data);
        }
        return sum / defs.Count;
    }
}