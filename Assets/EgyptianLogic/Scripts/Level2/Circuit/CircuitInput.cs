using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Entrada del circuito: un switch del jugador o un valor fijo (ej. GND = 0 para el acarreo inicial).</summary>
public class CircuitInput : SignalNode
{
    [Tooltip("Switch que controla la entrada. Vacío = valor fijo.")]
    public XRSwitchToggle sourceSwitch;
    public bool fixedValue;
    [Tooltip("Constante: no se recorre en la tabla de verdad (GND, VCC).")]
    public bool isConstant;

    [Header("Indicador (opcional)")]
    public Renderer indicator;
    public Color onColor = new Color(1f, 0.75f, 0.2f);
    public Color offColor = new Color(0.15f, 0.12f, 0.1f);

    public bool CurrentValue => sourceSwitch != null ? sourceSwitch.IsOn : fixedValue;

    public override bool TryGetValue(CircuitContext ctx, out bool value)
    {
        if (!isConstant && ctx?.Overrides != null && ctx.Overrides.TryGetValue(this, out value)) return true;
        value = CurrentValue;
        return true;
    }

    public void RefreshIndicator() => Level2Visuals.SetColor(indicator, CurrentValue ? onColor : offColor, CurrentValue);
}
