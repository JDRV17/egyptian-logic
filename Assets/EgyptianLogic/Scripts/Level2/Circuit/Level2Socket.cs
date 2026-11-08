using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// XRSocketInteractor que solo acepta cierto tipo de pieza: compuertas (opcionalmente de ciertos tipos),
/// enchufes de cable o un objeto de desbloqueo concreto (llave, antorcha…).
/// Reemplaza al XRSocketInteractor normal en las ranuras, puertos y cerraduras del Nivel 2.
/// </summary>
public class Level2Socket : XRSocketInteractor
{
    // Los valores nuevos van al final para no cambiar los ya serializados.
    // El 3 lo usaban unas tablillas que se quitaron: UnlockItem sigue en 4 para no romper escenas ya construidas.
    public enum Accept { Anything, LogicGate, WirePlug, UnlockItem = 4 }

    [Header("Filtro del Nivel 2")]
    public Accept accept = Accept.Anything;
    [Tooltip("Solo con Accept = LogicGate. Vacío = cualquier compuerta.")]
    public LogicGateType[] allowedGates = new LogicGateType[0];
    [Tooltip("Solo con Accept = UnlockItem: el id que debe tener el objeto (UnlockItem.id).")]
    public string requiredItemId = "";

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
            case Accept.UnlockItem:
                return t.TryGetComponent<UnlockItem>(out var item) && item.id == requiredItemId;
            default:
                return true;
        }
    }
}
