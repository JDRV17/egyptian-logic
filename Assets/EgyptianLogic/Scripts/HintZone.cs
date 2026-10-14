using UnityEngine;

public class HintZone : MonoBehaviour
{
    [SerializeField] private HintManager manager;
    [SerializeField] private int stageIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) manager.MarkArrived(stageIndex);
    }
}