using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Algo que entrega una señal y del que pueden salir cables: una entrada o la salida de una compuerta.</summary>
public abstract class SignalNode : MonoBehaviour
{
    [Tooltip("Nombre de la señal. En las entradas debe coincidir con la variable de la especificación (A, B, S0, Cin...).")]
    public string displayName = "A";
    [Tooltip("Punto donde nace el cable. Vacío = este objeto.")]
    public Transform cableAnchor;

    public Transform Anchor => cableAnchor != null ? cableAnchor : transform;

    /// <summary>Último valor calculado en vivo (para colorear cables). Solo es fiable si LiveValid.</summary>
    public bool LiveValue { get; set; }
    public bool LiveValid { get; set; }

    /// <summary>false si la señal no está definida (compuerta vacía, cables sin conectar o bucle).</summary>
    public abstract bool TryGetValue(CircuitContext ctx, out bool value);
}

/// <summary>
/// Contexto de una evaluación. Con Overrides = null se usan los valores reales de los switches;
/// si no, los de la tabla (así se prueban todas las combinaciones sin mover nada en la escena).
/// </summary>
public sealed class CircuitContext
{
    public IDictionary<CircuitInput, bool> Overrides;
    private readonly HashSet<GateSlot> visiting = new HashSet<GateSlot>();

    public bool Enter(GateSlot slot) => visiting.Add(slot); // false = bucle en el circuito
    public void Exit(GateSlot slot) => visiting.Remove(slot);
}
