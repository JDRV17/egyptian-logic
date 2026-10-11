using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DoorOpener : MonoBehaviour
{
    [Header("Puertas")]
    [SerializeField] private Transform[] doors;

    [Header("Movimiento")]
    [Tooltip("Desplazamiento en espacio mundo al abrir. Para subir: (0, 4, 0).")]
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 4f, 0f);
    [SerializeField] private float speed = 2f;

    [Header("Extras (opcionales)")]
    [SerializeField] private AudioSource openSound;
    public UnityEvent onOpened;

    private bool opened;

    /// <summary>Conéctalo al evento On Solved de SpinningCubes.</summary>
    public void Open()
    {
        if (opened) return;
        opened = true;

        if (openSound != null) openSound.Play();

        int pending = 0;
        foreach (var door in doors)
        {
            if (door == null) continue;
            pending++;
            StartCoroutine(MoveDoor(door, () =>
            {
                pending--;
                if (pending == 0) onOpened?.Invoke();
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