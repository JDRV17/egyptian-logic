using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HanoiPiece : MonoBehaviour
{
    public int pieceId;

    public HanoiSocket currentSocket;
    public int targetSocketId;

    public HanoiManager manager;

    private XRGrabInteractable grab;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (manager == null)
        {
            Debug.LogError("Manager not assigned in " + gameObject.name);
            return;
        }

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        DetectInitialSocket();
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        manager.OnPieceGrabbed(this);
    }

    void OnRelease(SelectExitEventArgs args)
    {
        Invoke(nameof(UpdateSocket), 0.1f);
    }

    void UpdateSocket()
    {
        HanoiSocket newSocket = manager.GetSocketFromInteractor(this);

        if (newSocket != null)
        {
            currentSocket = newSocket;
            manager.OnPieceReleased(this);
        }
    }

    void DetectInitialSocket()
    {
        if (manager.sockets == null)
        {
            Debug.LogError("Sockets not assigned in manager");
            return;
        }

        foreach (var socket in manager.sockets)
        {
            if (socket == null || socket.socket == null)
                continue;

            if (socket.socket.hasSelection)
            {
                var obj = socket.socket.firstInteractableSelected;

                if (obj != null && obj.transform == this.transform)
                {
                    currentSocket = socket;
                    return;
                }
            }
        }

        Debug.LogWarning("Piece " + pieceId + " has no initial socket.");
    }
}
