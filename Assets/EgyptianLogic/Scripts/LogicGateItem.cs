using UnityEngine;

public enum LogicGateType
{
    AND,
    OR,
    XOR,
    NAND,
    NOR,
    XNOR,
    NOT // Incluida por completitud, aunque no se use en pruebas binarias
}

public class LogicGateItem : MonoBehaviour
{
    [Header("Configuración de Compuerta")]
    [Tooltip("Selecciona el tipo de compuerta que representa este objeto.")]
    [SerializeField] private LogicGateType gateType;

    public LogicGateType GateType => gateType;
}