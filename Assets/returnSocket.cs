using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class returnSocket : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Rigidbody rb;
    private HanoiPiece piece;

    public float returnDelay = 0.2f;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        piece = GetComponent<HanoiPiece>();

        if (grab != null)
            grab.selectExited.AddListener(OnRelease);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        Invoke(nameof(CheckAndReturn), returnDelay);
    }

    void CheckAndReturn()
    {
        if (grab == null || piece == null)
            return;

        // if grabbed again, ignore
        if (grab.isSelected)
            return;

        // if somehow lost reference, stop
        if (piece.currentSocket == null)
            return;

        // if already placed correctly in a socket, do nothing
        if (piece.currentSocket.IsOccupied())
            return;

        ReturnToLastSocket();
    }

    void ReturnToLastSocket()
    {
        if (piece == null || piece.currentSocket == null)
            return;

        var socket = piece.currentSocket;

        if (socket.socket == null)
            return;

        Transform target = socket.socket.attachTransform != null
            ? socket.socket.attachTransform
            : socket.transform;

        if (target == null)
            return;

        // reset physics
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}
