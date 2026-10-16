using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Una de las salidas de una ranura de compuerta (Y, o S/Cout en los sumadores).</summary>
public class GateOutputPin : SignalNode
{
    public GateSlot slot;
    [Tooltip("0 = salida principal (Y / Suma). 1 = acarreo en los sumadores.")]
    public int outputIndex;
    public TextMeshPro label;

    public override bool TryGetValue(CircuitContext ctx, out bool value)
    {
        value = false;
        return slot != null && slot.TryEvaluate(ctx, outputIndex, out value);
    }
}
