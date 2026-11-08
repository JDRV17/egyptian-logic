using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Zona de salida del nivel: cuando el jugador (tag "Player") la cruza, carga la escena siguiente.
/// La θ y los resultados del jugador viajan con SessionData, así que el siguiente nivel continúa la adaptación.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelExit : MonoBehaviour
{
    [Tooltip("Nombre de la escena a cargar. Debe estar en File > Build Settings.")]
    public string nextSceneName = "EgyptLevel3";
    [Tooltip("Si está asignado, la salida solo funciona cuando el nivel está completo.")]
    public Level2Flow flow;
    [Tooltip("Segundos de espera antes de cargar (para un sonido o un fundido).")]
    public float delay = 1f;

    public UnityEvent onExit;

    private bool leaving;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (leaving || !other.CompareTag("Player")) return;
        if (flow != null && !flow.LevelCompleted) return;
        StartCoroutine(Leave());
    }

    private IEnumerator Leave()
    {
        leaving = true;
        onExit?.Invoke();
        yield return new WaitForSeconds(delay);

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[LevelExit] La escena '{nextSceneName}' no existe o no está en File > Build Settings.");
            leaving = false;
            yield break;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
