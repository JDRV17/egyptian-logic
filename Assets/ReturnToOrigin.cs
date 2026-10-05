using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ReturnToOrigin : MonoBehaviour
{
    private Vector3 startPos;
    private Quaternion startRot;

    private XRGrabInteractable grab;

    private bool isInSocket = false;
    private Rigidbody rb;

    public float returnDelay = 2f;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;

        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grab.selectExited.AddListener(OnRelease);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        Invoke(nameof(CheckAndReturn), returnDelay);
    }

    void CheckAndReturn()
    {
        if (isInSocket) return;

        if (grab.isSelected) return;

        ReturnObject();
    }

    void ReturnObject()
    {
        // Reset físico
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Teleport limpio
        transform.position = startPos;
        transform.rotation = startRot;
    }

    public void SetInSocket(bool value)
    {
        isInSocket = value;
    }
}
