using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MenuGroup : MonoBehaviour
{
    [SerializeField] private Vector3 hiddenOffset = new Vector3(0f, -1.5f, 0f);
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool startHidden = false;
    public UnityEvent onShown, onHidden;

    private Vector3 shownPos;
    private Coroutine routine;

    private void Awake()
    {
        shownPos = transform.position;
        if (startHidden)
        {
            transform.position = shownPos + hiddenOffset;
            gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Move(shownPos + hiddenOffset, shownPos, onShown, false);
    }

    public void Hide() => Move(transform.position, shownPos + hiddenOffset, onHidden, true);

    private void Move(Vector3 from, Vector3 to, UnityEvent done, bool deactivate)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run(from, to, done, deactivate));
    }

    private IEnumerator Run(Vector3 from, Vector3 to, UnityEvent done, bool deactivate)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.LerpUnclamped(from, to, curve.Evaluate(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        transform.position = to;
        if (deactivate) gameObject.SetActive(false);
        done?.Invoke();
    }
}