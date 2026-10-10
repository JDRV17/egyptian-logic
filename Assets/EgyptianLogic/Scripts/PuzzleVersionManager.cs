using UnityEngine;

// Se ejecuta antes que los demás scripts para que, al iniciar, solo quede activa la versión elegida.
[DefaultExecutionOrder(-100)]
public class PuzzleVersionManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private LightCombinationLock targetObjective;

    [Tooltip("Los padres de cada versión: posición 0 = Puzzle 1.1.1, 1 = Puzzle 1.1.2, 2 = Puzzle 1.1.3.")]
    [SerializeField] private GameObject[] puzzleRoots = new GameObject[3];

    [Header("Selección de versión")]
    [Tooltip("Si está activo, la versión se elige al azar al iniciar. Si no, se usa Forced Version.")]
    [SerializeField] private bool randomizeOnStart = true;

    [SerializeField] private LightCombinationLock.LevelVersion forcedVersion = LightCombinationLock.LevelVersion.V1;

    public LightCombinationLock.LevelVersion CurrentVersion { get; private set; }

    private void Awake()
    {
        LightCombinationLock.LevelVersion chosen;

        if (randomizeOnStart)
            chosen = (LightCombinationLock.LevelVersion)Random.Range(0, puzzleRoots.Length);
        else
            chosen = forcedVersion;

        SelectVersion(chosen);
    }

    /// <summary>
    /// Activa la versión indicada y desactiva las demás.
    /// Más adelante puedes llamarlo con la versión que decida el desempeño del jugador.
    /// </summary>
    public void SelectVersion(LightCombinationLock.LevelVersion version)
    {
        int index = (int)version;

        if (index < 0 || index >= puzzleRoots.Length || puzzleRoots[index] == null)
        {
            Debug.LogError($"[PuzzleVersionManager] No hay un Puzzle asignado para la versión {version}.");
            return;
        }

        CurrentVersion = version;

        if (targetObjective != null)
            targetObjective.SetVersion(version);

        for (int i = 0; i < puzzleRoots.Length; i++)
        {
            if (puzzleRoots[i] != null)
                puzzleRoots[i].SetActive(i == index);
        }

        Debug.Log($"[PuzzleVersionManager] Versión activa: {version} ({puzzleRoots[index].name})");
    }

    // Atajos para previsualizar en el editor (clic derecho sobre el componente en el Inspector).
    [ContextMenu("Previsualizar V1")] private void PreviewV1() => SelectVersion(LightCombinationLock.LevelVersion.V1);
    [ContextMenu("Previsualizar V2")] private void PreviewV2() => SelectVersion(LightCombinationLock.LevelVersion.V2);
    [ContextMenu("Previsualizar V3")] private void PreviewV3() => SelectVersion(LightCombinationLock.LevelVersion.V3);
}