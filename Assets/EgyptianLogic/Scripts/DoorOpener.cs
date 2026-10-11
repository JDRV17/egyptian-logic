using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DoorOpener : MonoBehaviour
{
    [Header("Puertas / objetos que se mueven")]
    [SerializeField] private Transform[] doors;

    [Header("Movimiento")]
    [Tooltip("Desplazamiento en espacio mundo al abrir. Para subir: (0, 4, 0).")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 4f, 0f);
    [SerializeField] private float speed = 2f;

    [Header("Física (opcional)")]
    [Tooltip("Rigidbody que NO son hijos de Doors pero deben quedarse quietos mientras se mueve (ej: la antorcha, si no es hija del cofre).")]
    [SerializeField] private Rigidbody[] extraBodies;

    [Header("Extras (opcionales)")]
    [SerializeField] private AudioSource openSound;
    public UnityEvent onOpened;

    private bool opened;
    private readonly List<(Rigidbody rb, bool wasKinematic, bool hadGravity)> bodies =
        new List<(Rigidbody, bool, bool)>();

    private void Awake()
    {
        // Congela desde el inicio para que nada caiga o se desplace mientras está escondido
        foreach (var door in doors)
        {
            if (door == null) continue;
            foreach (var rb in door.GetComponentsInChildren<Rigidbody>(true))
                Freeze(rb);
        }

        foreach (var rb in extraBodies)
            if (rb != null) Freeze(rb);
    }

    private void Freeze(Rigidbody rb)
    {
        foreach (var b in bodies)
            if (b.rb == rb) return; // ya registrado

        bodies.Add((rb, rb.isKinematic, rb.useGravity));
        rb.isKinematic = true;
    }

    private void Release()
    {
        foreach (var b in bodies)
        {
            if (b.rb == null) continue;
            b.rb.isKinematic = b.wasKinematic;
            b.rb.useGravity = b.hadGravity;
        }
        bodies.Clear();
    }

    /// <summary>Conéctalo al evento On Solved.</summary>
    public void Open()
    {
        if (opened) return;
        opened = true;

        if (openSound != null) openSound.Play();

        int pending = 0;
        foreach (var d in doors) if (d != null) pending++;

        foreach (var door in doors)
        {
            if (door == null) continue;
            StartCoroutine(MoveDoor(door, () =>
            {
                pending--;
                if (pending == 0)
                {
                    Release();
                    onOpened?.Invoke();
                }
            }));
        }
    }

    private IEnumerator MoveDoor(Transform door, System.Action done)
    {
        Vector3 target = door.position + openOffset;

        while (Vector3.Distance(door.position, target) > 0.01f)
        {
            door.position = Vector3.MoveTowards(door.position, target, speed * Time.deltaTime);
            yield return null;
        }

        door.position = target;
        done?.Invoke();
    }
}