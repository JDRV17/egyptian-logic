using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Portal entre niveles: cuando el jugador (tag "Player", el mismo que usan las HintZones) entra a su trigger,
/// carga la escena indicada. Va en el fondo blanco que aparece al abrirse la puerta del Nivel 1.
///
/// La escena debe estar en File → Build Settings → Scenes In Build.
/// SessionData no se destruye al cambiar de escena, así que el θ y los resultados del nivel anterior
/// pasan al siguiente (el motor del Nivel 3 arranca desde ese θ si su Theta Start es CarryOver).
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelPortal : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena a cargar (como aparece en Build Settings, sin .unity).")]
    [SerializeField] private string sceneName = "Nivel3";
    [Tooltip("Segundos de espera antes de cargar (para que suene algo o el jugador alcance a ver el blanco).")]
    [SerializeField] private float delay = 0.5f;
    [Tooltip("Opcional: suena al cruzar el portal.")]
    [SerializeField] private AudioSource crossSound;

    public UnityEvent onCrossed;

    private bool loading;

    private void Reset()
    {
        // Al agregarlo, el collider queda como trigger
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Cross();
    }

    /// <summary>También se puede llamar desde un evento (p. ej., al tocar con la mano un XRSimpleInteractable).</summary>
    public void Cross()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[LevelPortal] La escena \"{sceneName}\" no está en Build Settings (o el nombre no coincide).");
            return;
        }
        loading = true;
        if (crossSound != null) crossSound.Play();
        onCrossed?.Invoke();
        StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        Debug.Log($"[LevelPortal] Cargando {sceneName}…");
        yield return SceneManager.LoadSceneAsync(sceneName);
    }

    [ContextMenu("Cruzar ahora (prueba)")]
    private void TestCross() => Cross();
}