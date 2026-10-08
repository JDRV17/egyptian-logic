using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    private Light torchLight;

    [Header("Configuración de Parpadeo")]
    [Tooltip("Intensidad mínima de la luz")]
    public float minIntensity = 1.2f;

    [Tooltip("Intensidad máxima de la luz")]
    public float maxIntensity = 2.2f;

    [Tooltip("Velocidad de cambio del parpadeo")]
    public float flickerSpeed = 8f;

    void Start()
    {
        // Obtener el componente Light del mismo objeto
        torchLight = GetComponent<Light>();
    }

    void Update()
    {
        if (torchLight == null) return;

        // Uso de PerlinNoise para lograr una fluctuación suave y natural
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.0f);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
