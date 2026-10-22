using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>Entrada de cable (en una compuerta o en una salida). Recibe un WirePlug.</summary>
[RequireComponent(typeof(Level2Socket))]
public class WirePort : MonoBehaviour
{
    public string portName = "in";
    public TextMeshPro label;

    public UnityEvent onChanged = new UnityEvent();

    public WirePlug Plug { get; private set; }
    public SignalNode Source => Plug != null ? Plug.source : null;
    public bool IsConnected => Source != null;

    private Level2Socket socket;

    private void Awake()
    {
        socket = GetComponent<Level2Socket>();
        socket.accept = Level2Socket.Accept.WirePlug;
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnEnter);
        socket.selectExited.AddListener(OnExit);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnEnter);
        socket.selectExited.RemoveListener(OnExit);
    }

    private void OnEnter(SelectEnterEventArgs args)
    {
        Plug = args.interactableObject.transform.GetComponent<WirePlug>();
        if (Plug != null) Plug.CurrentPort = this;
        onChanged.Invoke();
    }

    private void OnExit(SelectExitEventArgs args)
    {
        if (Plug != null && Plug.CurrentPort == this) Plug.CurrentPort = null;
        Plug = null;
        onChanged.Invoke();
    }

    public void SetLabel(string text)
    {
        portName = text;
        if (label != null) label.text = text;
    }

    public bool TryGetValue(CircuitContext ctx, out bool value)
    {
        value = false;
        return Source != null && Source.TryGetValue(ctx, out value);
    }
}
