using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Salida del circuito: un puerto donde se conecta un cable y una bombilla que muestra su valor.</summary>
public class CircuitOutput : MonoBehaviour
{
    [Tooltip("Debe coincidir con el nombre de la salida en la especificación (S, Y, C4, GT...).")]
    public string displayName = "S";
    public WirePort port;

    [Header("Bombilla")]
    public Renderer bulb;
    public Light bulbLight;
    public Color onColor = new Color(1f, 0.85f, 0.3f);
    public Color offColor = new Color(0.12f, 0.1f, 0.08f);

    public bool LiveValue { get; private set; }

    public bool TryGetValue(CircuitContext ctx, out bool value)
    {
        value = false;
        return port != null && port.TryGetValue(ctx, out value);
    }

    public void RefreshLive(CircuitContext live)
    {
        LiveValue = TryGetValue(live, out bool v) && v;
        Level2Visuals.SetColor(bulb, LiveValue ? onColor : offColor, LiveValue, 2f);
        if (bulbLight != null) bulbLight.enabled = LiveValue;
    }
}
