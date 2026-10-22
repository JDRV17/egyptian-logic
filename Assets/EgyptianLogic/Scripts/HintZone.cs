using UnityEngine;

public class HintZone : MonoBehaviour
{
    [SerializeField] private HintManager manager;
    [SerializeField] private PerformanceTracker tracker;
    [SerializeField] private int stageIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        manager.MarkArrived(stageIndex);
        if (tracker != null) tracker.EnterZone(stageIndex);
    }
}