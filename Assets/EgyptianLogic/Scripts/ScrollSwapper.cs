using UnityEngine;

public class ScrollSwapper : MonoBehaviour
{
    [Header("Referencias a los hijos")]
    public GameObject scrollCerrado;
    public GameObject scrollAbierto;

    [Header("Efectos Opcionales")]
    public AudioSource audioSource;
    public AudioClip soundOpenClose;

    private bool isOpen = false;

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