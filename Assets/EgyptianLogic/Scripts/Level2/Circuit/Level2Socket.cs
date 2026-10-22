using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// XRSocketInteractor que solo acepta compuertas (opcionalmente de ciertos tipos) o solo enchufes de cable.
/// Reemplaza al XRSocketInteractor normal en las ranuras y puertos del Nivel 2.
/// </summary>
public class Level2Socket : XRSocketInteractor
{
    public enum Accept { Anything, LogicGate, WirePlug }

    [Header("Filtro del Nivel 2")]
    public Accept accept = Accept.Anything;
    [Tooltip("Solo con Accept = LogicGate. Vacío = cualquier compuerta.")]
    public LogicGateType[] allowedGates = new LogicGateType[0];

    public override bool CanHover(IXRHoverInteractable interactable) =>
        base.CanHover(interactable) && Accepts(interactable.transform);

    public override bool CanSelect(IXRSelectInteractable interactable) =>
        base.CanSelect(interactable) && Accepts(interactable.transform);

    public bool Accepts(Transform t)
    {
        switch (accept)
        {
            case Accept.LogicGate:
                if (!t.TryGetComponent<LogicGateItem>(out var gate)) return false;
                if (allowedGates == null || allowedGates.Length == 0) return true;
                foreach (var g in allowedGates) if (g == gate.GateType) return true;
                return false;
            case Accept.WirePlug:
                return t.GetComponent<WirePlug>() != null;
            default:
                return true;
        }
    }
}
