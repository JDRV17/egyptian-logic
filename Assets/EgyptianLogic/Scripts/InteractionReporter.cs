using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractionReporter : MonoBehaviour
{
    [SerializeField] private PerformanceTracker tracker;
    [SerializeField] private int stageIndex;

    [Header("Qué contar (dentro de este objeto y sus hijos, incluso inactivos)")]
    [SerializeField] private bool switches = true;           // XRSwitchToggle
    [SerializeField] private bool socketPlacements = true;   // piezas que entran a un socket
    [SerializeField] private bool cubes = false;             // CubeRotator.onRotated
    [SerializeField] private bool bitTorches = false;        // BitTorch.onChanged
    [SerializeField] private bool grabs = false;             // agarrar XRGrabInteractable (pergaminos de 1.4)

    [Header("Opciones")]
    [Tooltip("La primera interacción arranca el reloj del reto (1.4: primer pergamino).")]
    [SerializeField] private bool startsStage = false;

    private readonly List<Action> unsubscribe = new List<Action>();

    private void OnEnable()
    {
        if (tracker == null) return;

        if (switches)
            foreach (var s in GetComponentsInChildren<XRSwitchToggle>(true))
            {
                var sw = s; UnityAction<bool> a = _ => Report();
                sw.onStateChanged.AddListener(a);
                unsubscribe.Add(() => sw.onStateChanged.RemoveListener(a));
            }

        if (socketPlacements)
            foreach (var s in GetComponentsInChildren<XRSocketInteractor>(true))
            {
                var sk = s; UnityAction<SelectEnterEventArgs> a = _ => Report();
                sk.selectEntered.AddListener(a);
                unsubscribe.Add(() => sk.selectEntered.RemoveListener(a));
            }

        if (cubes)
            foreach (var c in GetComponentsInChildren<CubeRotator>(true))
            {
                var cu = c; UnityAction a = Report;
                cu.onRotated.AddListener(a);
                unsubscribe.Add(() => cu.onRotated.RemoveListener(a));
            }

        if (bitTorches)
            foreach (var b in GetComponentsInChildren<BitTorch>(true))
            {
                var bt = b; UnityAction<bool> a = _ => Report();
                bt.onChanged.AddListener(a);
                unsubscribe.Add(() => bt.onChanged.RemoveListener(a));
            }

        if (grabs)
            foreach (var g in GetComponentsInChildren<XRGrabInteractable>(true))
            {
                var gr = g; UnityAction<SelectEnterEventArgs> a = _ => Report();
                gr.selectEntered.AddListener(a);
                unsubscribe.Add(() => gr.selectEntered.RemoveListener(a));
            }
    }

    private void OnDisable()
    {
        foreach (var u in unsubscribe) u();
        unsubscribe.Clear();
    }

    private void Report()
    {
        if (startsStage) tracker.StartStage(stageIndex);
        tracker.RegisterInteraction(stageIndex);
    }
}