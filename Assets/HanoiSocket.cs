using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HanoiSocket : MonoBehaviour
{
    public int socketId;
    public XRSocketInteractor socket;

    public List<HanoiSocket> neighbors = new List<HanoiSocket>();

    public bool IsOccupied()
    {
        return socket != null && socket.hasSelection;
    }

    public void SetActive(bool value)
    {
        if (socket != null)
            socket.enabled = value;
    }
}
