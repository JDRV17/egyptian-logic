using UnityEngine;

/// <summary>
/// Los dos retos de una pareja no se pueden jugar a la vez: cuando el jugador llega a uno
/// (entra a su HintZone), el otro queda bloqueado hasta que el primero se resuelva.
/// Así el resultado de cada reto alcanza a ajustar la versión del siguiente.
/// </summary>
public class PairExclusion : MonoBehaviour
{
    [SerializeField] private Level3Puzzle first;
    [SerializeField] private Level3Puzzle second;

    private void OnEnable()
    {
        Hook(first, true);
        Hook(second, true);
    }

    private void OnDisable()
    {
        Hook(first, false);
        Hook(second, false);
    }

    private void Hook(Level3Puzzle p, bool on)
    {
        if (p == null) return;
        if (on) { p.Engaged += OnEngaged; p.Solved += OnSolved; }
        else { p.Engaged -= OnEngaged; p.Solved -= OnSolved; }
    }

    private Level3Puzzle Other(Level3Puzzle p) => p == first ? second : first;

    private void OnEngaged(Level3Puzzle p)
    {
        var other = Other(p);
        if (other != null && !other.IsSolved) other.SetBlocked(true);
    }

    private void OnSolved(Level3Puzzle p)
    {
        var other = Other(p);
        if (other != null) other.SetBlocked(false);
    }
}
