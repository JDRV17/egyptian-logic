using UnityEngine;

public class ScrollVersionText : MonoBehaviour
{
    [SerializeField] private TextSelector manager;

    [Tooltip("Elemento 0 = TMP de V1, 1 = V2, 2 = V3.")]
    [SerializeField] private GameObject[] textsByVersion = new GameObject[3];

    private void Start() => Apply();

    public void Apply()
    {
        if (manager == null) return;

        int v = manager.CurrentVersion;
        for (int i = 0; i < textsByVersion.Length; i++)
            if (textsByVersion[i] != null)
                textsByVersion[i].SetActive(i == v);
    }
}