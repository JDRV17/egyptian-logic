using UnityEngine;
using UnityEngine.Events;

public class HintZone : MonoBehaviour
{
    [SerializeField] private HintManager manager;
    [SerializeField] private PerformanceTracker tracker;
    [SerializeField] private int stageIndex;

    [Header("Nivel 3 (opcional)")]
    [Tooltip("Si se asigna, la llegada solo cuenta para pistas y tracker cuando el reto está disponible (no bloqueado).")]
    [SerializeField] private Level3Puzzle gate;
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        onPlayerEnter?.Invoke();

        if (gate != null && !gate.AcceptsPlayer) return;
        manager.MarkArrived(stageIndex);
        if (tracker != null) tracker.EnterZone(stageIndex);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (manager != null) manager.MarkLeft(stageIndex);
        onPlayerExit?.Invoke();
    }
}
