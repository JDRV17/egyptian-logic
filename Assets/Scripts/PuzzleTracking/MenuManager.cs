using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles principales")]
    public GameObject panelDesempeno;
    public GameObject panelHabilidades;
    public GameObject panelGenerarReporte;

    [Header("Configuración")]
    [Tooltip("Nombre exacto de la escena del juego para 'Reiniciar' (Build Settings).")]
    public string gameSceneName = "SpaceCodeO2Program";

    void Start()
    {
        // Estado inicial: pestaña Desempeño visible, Habilidades y Reporte ocultos
        ShowDesempeno();
    }

    // ---------- Pestañas ----------

    public void ShowDesempeno()
    {
        if (panelDesempeno != null) panelDesempeno.SetActive(true);
        if (panelHabilidades != null) panelHabilidades.SetActive(false);
        if (panelGenerarReporte != null) panelGenerarReporte.SetActive(false);
    }

    public void ShowHabilidades()
    {
        if (panelDesempeno != null) panelDesempeno.SetActive(false);
        if (panelHabilidades != null) panelHabilidades.SetActive(true);
        if (panelGenerarReporte != null) panelGenerarReporte.SetActive(false);
    }

    // ---------- Panel "Generar Reporte" (modal) ----------

    public void OpenGenerarReporte()
    {
        if (panelGenerarReporte != null) panelGenerarReporte.SetActive(true);
    }

    public void CloseGenerarReporte() // botón "<" (volver)
    {
        if (panelGenerarReporte != null) panelGenerarReporte.SetActive(false);
    }

    // ---------- Reiniciar / Salir ----------

    public void OnReiniciarClicked()
    {
        // Reinicia también los datos de progreso, para empezar de cero
        if (PuzzleTracker.Instance != null)
            PuzzleTracker.Instance.ResetAll();

        SceneManager.LoadScene(gameSceneName);
    }

    public void OnSalirClicked()
    {
        Debug.Log("Cerrando aplicación...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}