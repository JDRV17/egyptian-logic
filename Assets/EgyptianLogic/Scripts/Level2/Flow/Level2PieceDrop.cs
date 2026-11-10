using System.Collections;
using UnityEngine;

/// <summary>
/// Las compuertas de cada reto esperan ocultas y, cuando el reto termina de subir del piso, caen desde arriba
/// sobre su sitio (con su sonido de impacto, si tienen ImpactSound). Así el jugador ve y oye que aparecieron.
/// Mientras esperan están desactivadas: no se ven ni caen al vacío con el reto escondido bajo el piso.
/// Después de caer son objetos normales con gravedad, como las compuertas del Nivel 1.
/// Va en el mismo objeto que Level2Flow (Sistemas_Nivel2). No hay que configurar nada por reto.
/// </summary>
[RequireComponent(typeof(Level2Flow))]
public class Level2PieceDrop : MonoBehaviour
{
    [Tooltip("Metros sobre su sitio desde los que caen (el techo del comedor está a 8 m).")]
    [SerializeField] private float dropHeight = 3.5f;
    [Tooltip("Segundos de espera después de que el reto termina de subir.")]
    [SerializeField] private float delayAfterRise = 0.4f;
    [Tooltip("Segundos entre una compuerta y la siguiente, para que no caigan todas a la vez.")]
    [SerializeField] private float stagger = 0.12f;

    private Level2Flow flow;

    private void Awake()
    {
        flow = GetComponent<Level2Flow>();
        flow.onStationOpened.AddListener(OnStationOpened);
    }

    private void Start()
    {
        // El primer reto ya está arriba desde el inicio: sus piezas (si tiene) se quedan donde están
        for (int i = 1; i < flow.stations.Length; i++)
            foreach (var gate in Pieces(i))
                gate.gameObject.SetActive(false);
    }

    /// <summary>Lo dispara Level2Flow con el índice del reto que se dejó atrás: el siguiente empieza a subir.</summary>
    private void OnStationOpened(int i)
    {
        if (i + 1 < flow.stations.Length) StartCoroutine(Drop(i + 1));
    }

    private IEnumerator Drop(int i)
    {
        yield return new WaitForSeconds(flow.riseSeconds + delayAfterRise);

        foreach (var gate in Pieces(i))
        {
            // Solo las de la versión activa (las otras versiones están apagadas)
            if (gate.transform.parent != null && !gate.transform.parent.gameObject.activeInHierarchy) continue;

            gate.transform.position += Vector3.up * dropHeight;
            gate.gameObject.SetActive(true);
            var body = gate.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            yield return new WaitForSeconds(stagger);
        }
    }

    private LogicGateItem[] Pieces(int i)
    {
        var root = flow.stations[i].rising;
        return root != null ? root.GetComponentsInChildren<LogicGateItem>(true) : new LogicGateItem[0];
    }
}