using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Progresión del Nivel 3:
///   3.1 resuelto → cae la primera pieza (repara el cable de la pareja que el jugador elija).
///   Primera pareja completa (sus dos retos) → cae la segunda pieza (repara el otro cable).
///   Segunda pareja completa → On Level Complete (abrir la salida).
/// </summary>
public class Level3Progression : MonoBehaviour
{
    [Serializable]
    public class Pair
    {
        public string name = "Estados";
        [Tooltip("Índices en el tracker. Estados: 2 (3.3) y 3 (3.4). Registros: 4 (3.5) y 1 (3.2).")]
        public int[] stages = new int[2];
        [Tooltip("Opcional: se dispara al completar esta pareja.")]
        public UnityEvent onCompleted;
        [NonSerialized] public bool done;
    }

    [SerializeField] private PerformanceTracker tracker;

    [Header("Inicio")]
    [SerializeField] private int firstStage = 0;

    [Header("Parejas")]
    [SerializeField] private Pair[] pairs = new Pair[2];

    [Header("Piezas (inactivas en la escena; se activan para que caigan)")]
    [Tooltip("Pieza 0: tras resolver 3.1. Pieza 1: tras completar la primera pareja.")]
    [SerializeField] private GameObject[] pieces = new GameObject[2];

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que cae una pieza (sonido, partículas…). El índice es 0 o 1.")]
    public UnityEvent<int> onPieceDropped;
    public UnityEvent onLevelComplete;

    private bool firstDone;
    private int pairsDone;

    private void OnEnable() { if (tracker != null) tracker.onStageCompleted += OnStageCompleted; }
    private void OnDisable() { if (tracker != null) tracker.onStageCompleted -= OnStageCompleted; }

    private void OnStageCompleted(StageResult r) => Evaluate();

    /// <summary>Revisa el estado de todos los retos (público para pruebas).</summary>
    public void Evaluate()
    {
        if (!firstDone && Solved(firstStage))
        {
            firstDone = true;
            DropPiece(0);
        }

        foreach (var p in pairs)
        {
            if (p == null || p.done || !AllSolved(p.stages)) continue;
            p.done = true;
            pairsDone++;
            p.onCompleted?.Invoke();

            if (pairsDone == 1) DropPiece(1);
            else
            {
                Debug.Log("[Level3Progression] Nivel 3 completo.");
                onLevelComplete?.Invoke();
            }
        }
    }

    private void DropPiece(int i)
    {
        if (pieces != null && i < pieces.Length && pieces[i] != null) pieces[i].SetActive(true);
        Debug.Log($"[Level3Progression] Cae la pieza {i + 1}.");
        onPieceDropped?.Invoke(i);
    }

    private bool Solved(int stage) => tracker != null && tracker.GetState(stage) == PerformanceTracker.State.Solved;

    private bool AllSolved(int[] stages)
    {
        if (stages == null || stages.Length == 0) return false;
        foreach (int s in stages) if (!Solved(s)) return false;
        return true;
    }
}
