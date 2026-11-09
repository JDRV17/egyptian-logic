using UnityEngine;

/// <summary>
/// Objeto que abre algo al encajarlo en su cerradura (Level2Socket con Accept = UnlockItem y el mismo id):
/// una llave en una cerradura, una antorcha en un brasero…
/// </summary>
public class UnlockItem : MonoBehaviour
{
    [Tooltip("Debe coincidir con Required Item Id de la cerradura.")]
    public string id = "llave";
}
