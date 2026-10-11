using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class BitTorch : MonoBehaviour
{
    [SerializeField] private ParticleSystem flame;
    [SerializeField] private Light flameLight;
    public bool IsOn { get; private set; }
    public UnityEvent<bool> onChanged;

    private XRSimpleInteractable simple;

    private void Awake()
    {
        simple = GetComponent<XRSimpleInteractable>();
        SetState(false);
    }

    private void OnEnable() => simple.selectEntered.AddListener(OnPoked);
    private void OnDisable() => simple.selectEntered.RemoveListener(OnPoked);

    // Apagar: interactuar con ella (si está encendida)
    private void OnPoked(SelectEnterEventArgs args)
    {
        if (IsOn) SetState(false);
    }

    // Encender: antorcha del jugador, encendida y sujetada
    private void OnTriggerEnter(Collider other)
    {
        var torch = other.GetComponentInParent<Torch>();
        var grab = other.GetComponentInParent<XRGrabInteractable>();
        Debug.Log($"[BitTorch] {name} <- {other.name} | IsOn={IsOn} | Torch={(torch != null ? torch.IsLit.ToString() : "NO ENCONTRADO")} | Grab={(grab != null ? grab.isSelected.ToString() : "NO ENCONTRADO")}");

        if (IsOn) return;
        if (torch != null && torch.IsLit && grab != null && grab.isSelected)
            SetState(true);
    }

    private void SetState(bool on)
    {
        IsOn = on;
        if (flame != null) { if (on) flame.Play(); else flame.Stop(); }
        if (flameLight != null) flameLight.enabled = on;
        onChanged?.Invoke(on);
    }
}