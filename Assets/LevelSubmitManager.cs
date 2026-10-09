using UnityEngine;

public class LevelSubmitManager : MonoBehaviour
{
    [Header("Referencias del Nivel")]
    [SerializeField] private LightCombinationLock targetObjective;

    [Tooltip("Un circuito del jugador por versión: posición 0 = V1, 1 = V2, 2 = V3.")]
    [SerializeField] private SocketLogicEvaluator[] playerCircuitsByVersion = new SocketLogicEvaluator[3];

    [SerializeField] private XRPhysicalButton submitButton;

    private void OnEnable()
    {
        if (submitButton != null) submitButton.onPressed.AddListener(ValidateSubmission);
    }

    private void OnDisable()
    {
        if (submitButton != null) submitButton.onPressed.RemoveListener(ValidateSubmission);
    }

    public void ValidateSubmission()
    {
        if (targetObjective == null)
        {
            Debug.LogError("[SubmitManager] Falta asignar targetObjective.");
            return;
        }

        int v = (int)targetObjective.Version;
        if (v >= playerCircuitsByVersion.Length || playerCircuitsByVersion[v] == null)
        {
            Debug.LogError($"[SubmitManager] No hay circuito asignado para la versión {targetObjective.Version}.");
            return;
        }

        if (EvaluateTruthTableMatch(playerCircuitsByVersion[v]))
        {
            Debug.Log("<color=green><b>¡FELICITACIONES!</b> Has logrado reproducir el comportamiento de la prueba correctamente.</color>");
        }
        else
        {
            Debug.Log("<color=yellow><b>VUELVE A INTENTARLO.</b> La respuesta de tu circuito no coincide con el comportamiento de la prueba.</color>");
        }
    }

    private bool EvaluateTruthTableMatch(SocketLogicEvaluator player)
    {
        // Prueba las 16 combinaciones de los 4 switches
        for (int i = 0; i < 16; i++)
        {
            bool[] inputs = new bool[4];
            for (int k = 0; k < 4; k++)
                inputs[k] = ((i >> k) & 1) == 1;

            bool expected = targetObjective.EvaluateOutputForInputs(inputs);
            bool actual = player.EvaluateOutputForInputs(inputs);

            if (expected != actual)
            {
                Debug.Log($"[SubmitManager] Difiere en S1={inputs[0]} S2={inputs[1]} S3={inputs[2]} S4={inputs[3]}: esperado {expected}, obtenido {actual}");
                return false;
            }
        }

        return true;
    }
}