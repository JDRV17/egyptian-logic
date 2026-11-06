using UnityEngine;

/// <summary>
/// Reloj mini (3.5): copia en cada frame el giro de las manecillas del reloj grande.
/// Debe ser el mismo modelo, para que la posición de reposo de las manecillas coincida.
/// </summary>
public class ClockMirror : MonoBehaviour
{
    [SerializeField] private GameClock source;
    [SerializeField] private Transform minuteHand;
    [SerializeField] private Transform hourHand;

    private void LateUpdate()
    {
        if (source == null) return;
        if (minuteHand != null && source.MinuteHand != null) minuteHand.localRotation = source.MinuteHand.localRotation;
        if (hourHand != null && source.HourHand != null) hourHand.localRotation = source.HourHand.localRotation;
    }
}
