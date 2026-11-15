using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Menú de inicio con partida guardada. Si hay una, al tocar Play los mismos dos botones pasan a decir
/// Resume y New: Resume abre el nivel guardado y New sigue el flujo normal (nombre, historia, test).
/// Sin partida guardada no cambia nada. Al confirmar el nombre se borra la partida anterior y empieza una nueva.
/// No cambia las conexiones de la escena: mientras se elige, solo apaga temporalmente los eventos originales
/// de los botones y los vuelve a prender (e invoca) cuando toca seguir con el flujo normal.
/// </summary>
public class SaveMenu : MonoBehaviour
{
    [Header("Botones del menú principal")]
    public XRPhysicalButton playButton;
    public XRPhysicalButton exitButton;
    public TMP_Text playLabel;
    public TMP_Text exitLabel;

    [Header("Textos con partida guardada")]
    public string resumeText = "Resume";
    public string newText = "New";

    [Header("Nombre")]
    [Tooltip("Teclado del nombre: al confirmarlo empieza una partida nueva y se borra la guardada.")]
    public CubeKeyboard nameKeyboard;

    private enum State { Normal, Choosing, Done }
    private State state = State.Normal;
    private string playOriginal, exitOriginal;

    private void Awake()
    {
        if (nameKeyboard != null) nameKeyboard.onConfirmed.AddListener(OnNameConfirmed);

        if (!SaveSystem.HasSave || playButton == null || exitButton == null) return;

        // Hay partida: los eventos originales se apagan hasta saber qué quiere el jugador.
        SetPersistent(playButton.onPressed, false);
        SetPersistent(exitButton.onPressed, false);
        playButton.onPressed.AddListener(OnPlay);
        exitButton.onPressed.AddListener(OnExit);
        playOriginal = playLabel != null ? playLabel.text : null;
        exitOriginal = exitLabel != null ? exitLabel.text : null;
        Debug.Log("[SaveMenu] Hay una partida guardada.");
    }

    private void OnNameConfirmed()
    {
        SaveSystem.Delete();
        SessionData.Instance.BeginNewRun();
        Debug.Log("[SaveMenu] Partida nueva: se borró la guardada.");
    }

    private void OnPlay()
    {
        if (state == State.Normal)
        {
            state = State.Choosing;
            SetLabels(resumeText, newText);
            return;
        }
        if (state != State.Choosing) return;

        state = State.Done;
        if (!SaveSystem.Resume())
        {
            Debug.LogWarning("[SaveMenu] No se pudo reanudar; se empieza una partida nueva.");
            StartNormalFlow(playButton);
        }
    }

    private void OnExit()
    {
        if (state == State.Done) return;
        // Antes de elegir, Exit sigue siendo Exit; después, es New.
        StartNormalFlow(state == State.Choosing ? playButton : exitButton);
    }

    /// <summary>Vuelve los botones a como estaban y ejecuta los eventos originales del botón indicado.</summary>
    private void StartNormalFlow(XRPhysicalButton which)
    {
        state = State.Done;
        playButton.onPressed.RemoveListener(OnPlay);
        exitButton.onPressed.RemoveListener(OnExit);
        SetPersistent(playButton.onPressed, true);
        SetPersistent(exitButton.onPressed, true);
        SetLabels(playOriginal, exitOriginal);
        StartCoroutine(InvokeNextFrame(which.onPressed));   // fuera del evento que se está ejecutando
    }

    private static IEnumerator InvokeNextFrame(UnityEvent e)
    {
        yield return null;
        e.Invoke();
    }

    private void SetLabels(string play, string exit)
    {
        if (playLabel != null && play != null) playLabel.text = play;
        if (exitLabel != null && exit != null) exitLabel.text = exit;
    }

    private static void SetPersistent(UnityEvent e, bool on)
    {
        for (int i = 0; i < e.GetPersistentEventCount(); i++)
            e.SetPersistentListenerState(i, on ? UnityEventCallState.RuntimeOnly : UnityEventCallState.Off);
    }
}
