using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PuzzleBarEntry
{
    [Tooltip("Debe coincidir EXACTO con el puzzleId usado en el PuzzleDefinition: hanoi, tubos, pintar, botones, llave")]
    public string puzzleId;

    public Image fillImage;     // la barra azul de relleno (Image Type = Filled)
    public Text percentText;    // texto "XX/XX"
    public Text timeText;       // texto "XXs"
}

public class DesempenoMenuController : MonoBehaviour
{
    [Header("Una entrada por cada uno de los 5 puzzles")]
    public List<PuzzleBarEntry> entries = new List<PuzzleBarEntry>();

    private PuzzleTracker tracker;

    void Start()
    {
        tracker = PuzzleTracker.Instance;
        if (tracker == null)
            Debug.LogError("PuzzleTracker.Instance es null. Verifica que el GameManager exista y haya cargado antes que esta escena.");
    }

    void Update()
    {
        if (tracker == null) return;

        foreach (var entry in entries)
        {
            var def = tracker.GetDefinition(entry.puzzleId);
            if (def == null)
            {
                Debug.LogWarning($"No se encontró un PuzzleDefinition con puzzleId '{entry.puzzleId}'. Revisa el nombre.");
                continue;
            }

            var data = tracker.GetRuntime(entry.puzzleId);
            float score01 = ScoringFormula.CalculateScore01(def, data);

            if (entry.fillImage != null)
                entry.fillImage.fillAmount = score01;

            if (entry.percentText != null)
                entry.percentText.text = $"{Mathf.RoundToInt(score01 * 100f)}/100";

            if (entry.timeText != null)
            {
                int minutes = Mathf.FloorToInt(data.elapsedSeconds / 60f);
                int seconds = Mathf.FloorToInt(data.elapsedSeconds % 60f);
                entry.timeText.text = $"{minutes}:{seconds:00}s";
            }
        }
    }
}