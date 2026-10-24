using UnityEngine;

public class Torch : MonoBehaviour
{
    [SerializeField] private bool lit = true;   // la del jugador nace encendida
    public bool IsLit => lit;
}