using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La "guía del nivel" del Nivel 3: el pergamino que sale al presionar Y cuando el jugador no está en ningún reto.
/// Dice qué hacer a continuación según el estado real del nivel (no según un reto). No cuenta para el puntaje.
/// Se asigna en el campo Guide del HintManager; sin guía, el HintManager funciona como en el Nivel 1.
/// </summary>
public class Level3Guide : MonoBehaviour
{
    [Header("Estado del nivel")]
    [SerializeField] private PerformanceTracker tracker;
    [Tooltip("Retos 3.2–3.5 (el orden no importa: se usa su Stage Index).")]
    [SerializeField] private Level3Puzzle[] puzzles = new Level3Puzzle[0];
    [Tooltip("Los dos huecos de cable del reloj.")]
    [SerializeField] private ClockCableSocket[] cableSockets = new ClockCableSocket[0];
    [SerializeField] private int firstStage = 0;

    [Header("Textos de la guía")]
    [TextArea(2, 5)] public string startText = "Junto al gran reloj te espera el primer reto.";
    [TextArea(2, 5)] public string firstPieceText = "El reloj tiene dos cables rotos. Pon la pieza que cayó en cualquiera de los dos: cada cable despierta dos retos nuevos.";
    [TextArea(2, 5)] public string twoOpenHeader = "Despertaron dos retos.";
    [TextArea(2, 5)] public string oneOpenHeader = "Te falta un reto de esta pareja.";
    [TextArea(2, 5)] public string secondPieceText = "Cayó una nueva pieza. Ponla en el cable del reloj que sigue roto.";
    [TextArea(2, 5)] public string endText = "Todos los mecanismos funcionan. La salida está abierta.";

    [Tooltip("Dónde queda cada reto. Índice = Stage Index (0 = 3.1 … 4 = 3.5).")]
    [TextArea(2, 5)]
    public string[] whereIs =
    {
        "",
        "Busca el cajón de cubos ensartados en un cilindro: es el registro de desplazamiento.",
        "Sigue el cable del reloj que brilla: te lleva al guardián de estados.",
        "Busca el semáforo que espera ser diseñado.",
        "Sigue el cable del reloj que brilla: te lleva a la contrarreloj binaria.",
    };

    /// <summary>Reto que el jugador ya empezó o al que ya llegó y no ha resuelto; −1 si no hay.</summary>
    public int EngagedStage()
    {
        if (puzzles == null) return -1;
        foreach (var p in puzzles)
            if (p != null && p.IsEngaged && !p.IsSolved) return p.StageIndex;
        return -1;
    }

    /// <summary>Texto de la guía para el estado actual del nivel.</summary>
    public string CurrentText()
    {
        if (!Solved(firstStage)) return startText;

        // Retos desbloqueados y sin resolver (la pareja abierta)
        var open = new List<Level3Puzzle>();
        if (puzzles != null)
            foreach (var p in puzzles)
                if (p != null && p.IsUnlocked && !Solved(p.StageIndex)) open.Add(p);

        if (open.Count > 0)
        {
            var parts = new List<string> { open.Count > 1 ? twoOpenHeader : oneOpenHeader };
            foreach (var p in open)
            {
                string w = Where(p.StageIndex);
                if (!string.IsNullOrEmpty(w)) parts.Add(w);
            }
            return string.Join(" ", parts);
        }

        if (AllPuzzlesSolved()) return endText;

        int connected = 0;
        if (cableSockets != null)
            foreach (var s in cableSockets)
                if (s != null && s.IsConnected) connected++;
        return connected == 0 ? firstPieceText : secondPieceText;
    }

    private string Where(int stage) => whereIs != null && stage >= 0 && stage < whereIs.Length ? whereIs[stage] : "";

    private bool Solved(int stage) => tracker != null && tracker.GetState(stage) == PerformanceTracker.State.Solved;

    private bool AllPuzzlesSolved()
    {
        if (tracker == null) return false;
        for (int i = 0; i < tracker.StageCount; i++)
            if (!Solved(i)) return false;
        return true;
    }

    [ContextMenu("Mostrar el texto actual en la consola")]
    private void LogCurrent() => Debug.Log("[Level3Guide] " + CurrentText());
}
