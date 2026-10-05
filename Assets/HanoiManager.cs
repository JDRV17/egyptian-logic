using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HanoiManager : MonoBehaviour
{
    private const string PuzzleId = "hanoi";

    public List<HanoiSocket> sockets;
    public List<HanoiPiece> pieces;
    public GameObject finalObject;
    public bool panex_completed = false;

    private HanoiPiece currentPiece;

    // Guarda el socket en el que estaba cada pieza justo antes de agarrarla,
    // para poder detectar si el release resultó en un MOVIMIENTO real.
    private Dictionary<HanoiPiece, HanoiSocket> socketBeforeGrab = new Dictionary<HanoiPiece, HanoiSocket>();

    void Start()
    {
        if (finalObject != null)
            finalObject.SetActive(false);
    }

    // =============================
    // WHEN PIECE IS GRABBED
    // =============================
    public void OnPieceGrabbed(HanoiPiece piece)
    {
        currentPiece = piece;

        if (piece.currentSocket == null)
        {
            Debug.LogError("Piece has no current socket");
            return;
        }

        // Registrar el socket de origen para comparar luego en el release
        socketBeforeGrab[piece] = piece.currentSocket;

        HanoiSocket startSocket = piece.currentSocket;
        HashSet<HanoiSocket> reachable = GetReachableSockets(startSocket);

        foreach (var socket in sockets)
        {
            if (socket == null)
                continue;

            bool shouldEnable = false;

            if (reachable.Contains(socket))
                shouldEnable = true;

            if (socket.IsOccupied())
                shouldEnable = true;

            socket.SetActive(shouldEnable);
        }
    }

    // =============================
    // FLOOD FILL: sockets alcanzables desde 'start'
    // =============================
    private HashSet<HanoiSocket> GetReachableSockets(HanoiSocket start)
    {
        HashSet<HanoiSocket> visited = new HashSet<HanoiSocket>();
        Queue<HanoiSocket> frontier = new Queue<HanoiSocket>();

        visited.Add(start);
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            HanoiSocket current = frontier.Dequeue();

            if (current.neighbors == null)
                continue;

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || visited.Contains(neighbor))
                    continue;

                if (neighbor.IsOccupied())
                    continue;

                visited.Add(neighbor);
                frontier.Enqueue(neighbor);
            }
        }

        return visited;
    }

    // =============================
    // WHEN PIECE IS RELEASED
    // =============================
    public void OnPieceReleased(HanoiPiece piece)
    {
        EnableAllSockets();

        // --- TRACKING: contar movimiento solo si el socket realmente cambió ---
        if (socketBeforeGrab.TryGetValue(piece, out var previousSocket))
        {
            if (piece.currentSocket != null && piece.currentSocket != previousSocket)
            {
                PuzzleTracker.Instance?.RegisterInteraction(PuzzleId);
            }
            socketBeforeGrab.Remove(piece);
        }

        CheckSolution();
    }

    void EnableAllSockets()
    {
        foreach (var socket in sockets)
        {
            if (socket != null)
                socket.SetActive(true);
        }
    }

    // =============================
    // GET SOCKET FROM XR
    // =============================
    public HanoiSocket GetSocketFromInteractor(HanoiPiece piece)
    {
        foreach (var socket in sockets)
        {
            if (socket == null || socket.socket == null)
                continue;

            if (socket.socket.hasSelection)
            {
                var obj = socket.socket.firstInteractableSelected;
                if (obj != null && obj.transform.GetComponent<HanoiPiece>() == piece)
                {
                    return socket;
                }
            }
        }
        return null;
    }

    // =============================
    // CHECK SOLUTION
    // =============================
    void CheckSolution()
    {
        if (panex_completed) return;

        foreach (var piece in pieces)
        {
            if (piece == null || piece.currentSocket == null)
                return;

            if (piece.currentSocket.socketId != piece.targetSocketId)
                return;
        }

        Debug.Log("PUZZLE COMPLETED");
        if (finalObject != null)
            finalObject.SetActive(true);

        panex_completed = true;

        // --- TRACKING: puzzle completado ---
        PuzzleTracker.Instance?.CompletePuzzle(PuzzleId);
    }
}