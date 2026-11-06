using UnityEngine;

/// <summary>Efectos de partículas de usar y tirar (aparición de piezas del Nivel 3).</summary>
public static class Fx
{
    /// <summary>Crea una copia del prefab de partículas en esa posición, la reproduce y la borra al terminar.</summary>
    public static void Play(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null) return;
        var ps = Object.Instantiate(prefab, position, prefab.transform.rotation);
        ps.Play(true);
        var main = ps.main;
        Object.Destroy(ps.gameObject, main.duration + main.startLifetime.constantMax + 0.5f);
    }
}
