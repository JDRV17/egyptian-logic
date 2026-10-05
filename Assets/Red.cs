using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Red: MonoBehaviour
{
    public InputAction clickAction;

    public float normalZ = 0.1f;
    public float pressedZ = 2f;
    public float duration = 0.01f;

    private bool isAnimating = false;

    void OnEnable()
    {
        clickAction.Enable();
    }

    void OnDisable()
    {
        clickAction.Disable();
    }

    void Update()
    {
        if (isAnimating) return;

        if (clickAction.triggered)
        {
            StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        isAnimating = true;

        // CAMBIO INSTANTÁNEO
        gameObject.tag = "MarkerRed";

        Vector3 scale = transform.localScale;
        scale.z = pressedZ;
        transform.localScale = scale;

        yield return new WaitForSeconds(duration);

        // VOLVER A NORMAL
        gameObject.tag = "Untagged";

        scale.z = normalZ;
        transform.localScale = scale;

        isAnimating = false;
    }
}
