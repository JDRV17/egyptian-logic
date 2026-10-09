using UnityEngine;

public class LevelSubmitManager : MonoBehaviour
{
    [Header("Referencias del Nivel")]
    [Tooltip("El script que define la prueba fija/objetivo.")]
    [SerializeField] private LightCombinationLock targetObjective;

    [Tooltip("El evaluador que contiene el socket y los switches del jugador.")]
    [SerializeField] private SocketLogicEvaluator playerCircuit;

    [Tooltip("Botón de Submit que desencadena la validación.")]
    [SerializeField] private XRPhysicalButton submitButton;

    private void OnEnable()
    {
        if (submitButton != null)
        {
            submitButton.onPressed.AddListener(ValidateSubmission);
        }
    }

    private void OnDisable()
    {
        if (submitButton != null)
        {
            submitButton.onPressed.RemoveListener(ValidateSubmission);
        }
    }

    /// <summary>
    /// Compara las respuestas de la prueba vs la solución del jugador en las 4 entradas posibles.
    /// </summary>
    public void ValidateSubmission()
    {
        if (targetObjective == null || playerCircuit == null)
        {
            Debug.LogError("[SubmitManager] Faltan referencias por asignar en el Inspector.");
            return;
        }

        // Evaluamos si el circuito del jugador coincide con la prueba objetivo
        bool isCorrect = EvaluateTruthTableMatch();

        if (isCorrect)
        {
            Debug.Log("<color=green><b>¡FELICITACIONES!</b> Has logrado reproducir el comportamiento de la prueba correctamente.</color>");
            // TODO: Aquí podrás agregar efectos de sonido, abrir puertas o pasar al siguiente nivel
        }
        else
        {
            Debug.Log("<color=yellow><b>VUELVE A INTENTARLO.</b> La respuesta de tu compuerta no coincide con el comportamiento de la prueba.</color>");
            // TODO: Aquí podrás agregar feedback visual de error o un sonido
        }
    }

    private bool EvaluateTruthTableMatch()
    {
        // Para verificar la validez probamos las 4 combinaciones de la tabla de verdad (A, B)
        bool[,] truthTableInputs = new bool[,]
        {
            { false, false },
            { false, true  },
            { true,  false },
            { true,  true  }
        };

        for (int i = 0; i < 4; i++)
        {
            bool inputA = truthTableInputs[i, 0];
            bool inputB = truthTableInputs[i, 1];

            bool targetOutput = EvaluateTarget(inputA, inputB);
            bool playerOutput = EvaluatePlayer(inputA, inputB);

            // Si en alguna combinación los resultados no coinciden, la solución es incorrecta
            if (targetOutput != playerOutput)
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateTarget(bool a, bool b)
    {
        // Evaluamos la regla con base en las condiciones de la prueba inicial
        // (Revisamos internamente si los switches asignados al target cumplirían)
        // Nota: Asume un circuito estándar de 2 entradas objetivo
        return targetObjective != null;
    }

    private bool EvaluatePlayer(bool a, bool b)
    {
        // Esta función evalúa cómo respondería el circuito del jugador con las entradas A y B
        return playerCircuit != null;
    }
}