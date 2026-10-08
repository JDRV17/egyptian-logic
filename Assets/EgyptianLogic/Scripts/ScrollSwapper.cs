using UnityEngine;

public class ScrollSwapper : MonoBehaviour
{
    [Header("Referencias a los hijos")]
    public GameObject scrollCerrado;
    public GameObject scrollAbierto;

    [Header("Efectos Opcionales")]
    public AudioSource audioSource;
    public AudioClip soundOpenClose;

    [Header("Configuración para Pruebas en PC")]
    [Tooltip("Tecla del teclado para abrir o cerrar el pergamino")]
    public KeyCode pcOpenKey = KeyCode.E;

    private bool isOpen = false;

    void Update()
    {
        // Esto permite probarlo en PC presionando la tecla 'E'
        if (Input.GetKeyDown(pcOpenKey))
        {
            ToggleScrollState();
        }
    }

    // Método principal para alternar el estado
    public void ToggleScrollState()
    {
        isOpen = !isOpen;

        // Alternar visibilidad de los hijos
        if (scrollCerrado != null) scrollCerrado.SetActive(!isOpen);
        if (scrollAbierto != null) scrollAbierto.SetActive(isOpen);

        // Sonido de papel (opcional)
        if (audioSource != null && soundOpenClose != null)
        {
            audioSource.PlayOneShot(soundOpenClose);
        }

        Debug.Log(isOpen ? "Pergamino ABIERTO" : "Pergamino CERRADO");
    }
}