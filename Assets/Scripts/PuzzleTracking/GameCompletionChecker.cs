using UnityEngine;
using UnityEngine.SceneManagement;

public class GameCompletionChecker : MonoBehaviour
{
    public static GameCompletionChecker Instance { get; private set; }

    [Tooltip("Nombre exacto de la escena de resultados (debe estar agregada en Build Settings).")]
    public string resultsSceneName = "ResultadosFinales";

    [Tooltip("Segundos de espera después de abrir la puerta final antes de cambiar de escena.")]
    public float delayBeforeLoad = 5f;

    private bool sceneLoadTriggered = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // Llamado por DoorAccess cuando la puerta final se abre por primera vez
    public void NotifyGameFinished()
    {
        if (sceneLoadTriggered) return;
        sceneLoadTriggered = true;

        Debug.Log($"Juego terminado. Cargando escena de resultados en {delayBeforeLoad} segundos...");
        Invoke(nameof(LoadResultsScene), delayBeforeLoad);
    }

    private void LoadResultsScene()
    {
        SceneManager.LoadScene(resultsSceneName);
    }
}