using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TextSelector : MonoBehaviour
{
    [SerializeField] private bool randomizeOnStart = true;
    [Tooltip("0 = V1, 1 = V2, 2 = V3")]
    [Range(0, 2)] [SerializeField] private int forcedVersion = 0;

    public int CurrentVersion { get; private set; }

    private void Awake()
    {
        CurrentVersion = randomizeOnStart ? Random.Range(0, 3) : forcedVersion;
        Debug.Log($"[TextSelector] Versión activa: V{CurrentVersion + 1}");
    }

    public void SelectVersion(int version)
    {
        CurrentVersion = Mathf.Clamp(version, 0, 2);
        foreach (var s in FindObjectsOfType<ScrollVersionText>())
            s.Apply();
    }
}