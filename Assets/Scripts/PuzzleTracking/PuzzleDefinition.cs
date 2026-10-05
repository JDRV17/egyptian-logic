using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleDefinition", menuName = "PuzzleTracking/Puzzle Definition")]
public class PuzzleDefinition : ScriptableObject
{
    [Header("Identificación")]
    public string puzzleId;
    public string displayName;

    [Header("Dificultad (0-1, según orden: Hanoi=1.0 ... Llave=0.2)")]
    [Range(0f, 1f)] public float difficulty = 0.5f;

    [Header("Interacciones óptimas")]
    [Tooltip("Desmarcar en puzzles de estrategia libre (Tubos, Pintar) donde no hay número óptimo.")]
    public bool hasIdealInteractions = true;
    public int idealInteractions = 1;

    [Header("Tiempo ideal (segundos)")]
    public float idealTimeSeconds = 60f;
}