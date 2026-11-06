/// <summary>
/// Cualquier reto que tenga versiones V1–V3. El AdaptationEngine y el HintManager lo reconocen
/// sin que haya que agregar un caso nuevo por cada tipo de reto (lo usan los retos del Nivel 3).
/// </summary>
public interface IVersioned
{
    /// <summary>0 = V1, 1 = V2, 2 = V3.</summary>
    int CurrentVersion { get; }

    void SelectVersion(int version);
}
