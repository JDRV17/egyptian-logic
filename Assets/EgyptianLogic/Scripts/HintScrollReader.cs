using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class HintScrollReader : MonoBehaviour
{
    [System.NonSerialized] public PerformanceTracker tracker;   // lo asigna el spawner

    private XRGrabInteractable grab;
    private bool reading;

    private void Awake() => grab = GetComponent<XRGrabInteractable>();

    private void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
        StopReading();   // por si se destruye con el pergamino en la mano
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (reading || tracker == null) return;
        reading = true;
        tracker.BeginHintReading();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!grab.isSelected) StopReading();   // solo si ya no lo sostiene ninguna mano
    }

    private void StopReading()
    {
        if (!reading) return;
        reading = false;
        if (tracker != null) tracker.EndHintReading();
    }
}