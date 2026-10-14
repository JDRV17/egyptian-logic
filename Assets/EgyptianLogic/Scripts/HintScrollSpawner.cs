using UnityEngine;
using TMPro;

public class HintScrollSpawner : MonoBehaviour
{
    [SerializeField] private GameObject scrollPrefab;
    [SerializeField] private Transform head;                 // Main Camera
    [SerializeField] private float distance = 1.0f;
    [SerializeField] private float height = 2.5f;
    [SerializeField] private float lifeTime = 120f;          // se destruye solo

    public void Spawn(string text, bool isSolution)
    {
        Vector3 fwd = head.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 pos = head.position + fwd * distance;
        pos.y = head.position.y + (height - 1.6f);           // cae desde arriba

        var go = Instantiate(scrollPrefab, pos, Quaternion.LookRotation(fwd));
        var label = go.GetComponentInChildren<TMP_Text>(true);
        if (label != null) label.text = text;

        Destroy(go, lifeTime);
    }
}