using UnityEngine;

public enum LogicGateType
{
    AND,
    OR,
    XOR,
    NAND,
    NOR,
    XNOR,
    NOT, // Incluida por completitud, aunque no se use en pruebas binarias

    // Módulos del Nivel 2 (siempre al final para no cambiar los valores ya serializados)
    MUX2,       // entradas: I0, I1, S              salidas: Y
    MUX4,       // entradas: I0, I1, I2, I3, S0, S1  salidas: Y
    HALF_ADDER, // entradas: A, B                    salidas: S, C
    FULL_ADDER  // entradas: A, B, Cin               salidas: S, Cout
}

public class LogicGateItem : MonoBehaviour
{
    [Header("Configuración de Compuerta")]
    [Tooltip("Selecciona el tipo de compuerta que representa este objeto.")]
    [SerializeField] private LogicGateType gateType;

    public LogicGateType GateType => gateType;
}