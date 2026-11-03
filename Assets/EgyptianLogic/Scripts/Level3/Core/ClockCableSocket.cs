using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// El hueco de un cable dañado. Va en el mismo objeto que un XR Socket Interactor.
/// Al encajar una pieza (ClockCablePiece), la pieza queda fija, el reloj pasa a seguir
/// al reto de este cable y se dispara On Connected (revelar la pareja, encender luces, etc.).
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class ClockCableSocket : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [Tooltip("Reto con reloj al que lleva este cable (2 = 3.3, 4 = 3.5).")]
    [SerializeField] private int stageIndex = 2;
    [Tooltip("Tiempo para que el socket termine de atraer la pieza antes de fijarla.")]
    [SerializeField] private float snapDelay = 0.25f;

    [Header("Eventos")]
    [Tooltip("Aquí van el PuzzleRevealer de la pareja, luces, sonido, el tramo de cable reparado, etc.")]
    public UnityEvent onConnected;

    private XRSocketInteractor socket;
    private bool connected;

    public bool IsConnected => connected;
    public int StageIndex => stageIndex;

    private void Awake() => socket = GetComponent<XRSocketInteractor>();
    private void OnEnable() => socket.selectEntered.AddListener(OnSelect);
    private void OnDisable() => socket.selectEntered.RemoveListener(OnSelect);

    private void OnSelect(SelectEnterEventArgs args)
    {
        if (connected || args.interactableObject == null) return;
        var piece = args.interactableObject.transform.GetComponent<ClockCablePiece>();
        if (piece == null) return;   // otro objeto: no conecta nada

        connected = true;
        StartCoroutine(LockRoutine(piece));
    }

    /// <summary>Conecta sin pieza física (pruebas en el editor).</summary>
    [ContextMenu("Conectar (prueba)")]
    public void ConnectNow()
    {
        if (connected && clock != null && clock.IsFollowing(stageIndex)) return;
        connected = true;
        Notify();
    }

    private IEnumerator LockRoutine(ClockCablePiece piece)
    {
        yield return new WaitForSeconds(snapDelay);
        piece.LockAt(transform);
        socket.socketActive = false;   // el hueco ya está ocupado para siempre
        Notify(piece);
    }

    private void Notify(ClockCablePiece piece = null)
    {
        if (clock != null)
        {
            // La pieza pasa a ser parte de este cable (brilla con él en cada tic)
            if (piece != null) clock.AddCableRenderers(stageIndex, piece.Renderers);
            clock.Connect(stageIndex);
        }
        onConnected?.Invoke();
    }
}