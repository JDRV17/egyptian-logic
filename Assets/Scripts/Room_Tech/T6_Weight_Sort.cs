using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class T6_Weight_Sort : MonoBehaviour
{
    private const string PuzzleId = "tubos";

    public XRSocketInteractor vialSocket1, vialSocket2, vialSocket3,
    vialSocket4, vialSocket5, vialSocket6, vialSocket7, vialSocket8,
    vialSocket9, vialSocket10;

    public bool is_sorted;
    public GameObject Task6_Weights;

    // --- TRACKING: snapshot del contenido anterior de cada socket ---
    private string[] previousOccupants = new string[10];
    private bool occupantsInitialized = false;

    void Start()
    {
        is_sorted = false;
        Task6_Weights.GetComponent<Animator>().Play("WeightsIDLE");
    }

    void Update()
    {
        DetectVialChanges();
        CheckSorting();
    }

    // --- TRACKING: cuenta una interacción cada vez que cambia el contenido de algún socket ---
    void DetectVialChanges()
    {
        XRSocketInteractor[] socketsArr = {
            vialSocket1, vialSocket2, vialSocket3, vialSocket4, vialSocket5,
            vialSocket6, vialSocket7, vialSocket8, vialSocket9, vialSocket10
        };

        string[] currentOccupants = new string[10];
        for (int i = 0; i < 10; i++)
        {
            var interactable = socketsArr[i].GetOldestInteractableSelected();
            currentOccupants[i] = interactable != null ? interactable.transform.name : null;
        }

        if (!occupantsInitialized)
        {
            System.Array.Copy(currentOccupants, previousOccupants, 10);
            occupantsInitialized = true;
            return;
        }

        for (int i = 0; i < 10; i++)
        {
            if (currentOccupants[i] != previousOccupants[i])
            {
                PuzzleTracker.Instance?.RegisterInteraction(PuzzleId);
            }
        }

        System.Array.Copy(currentOccupants, previousOccupants, 10);
    }

    void CheckSorting()
    {
        IXRSelectInteractable obj_vial1 = vialSocket1.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial2 = vialSocket2.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial3 = vialSocket3.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial4 = vialSocket4.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial5 = vialSocket5.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial6 = vialSocket6.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial7 = vialSocket7.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial8 = vialSocket8.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial9 = vialSocket9.GetOldestInteractableSelected();
        IXRSelectInteractable obj_vial10 = vialSocket10.GetOldestInteractableSelected();

        if (is_sorted) return; // ya completado, no evaluar más

        if ((obj_vial1 != null) && (obj_vial2 != null) && (obj_vial3 != null) &&
        (obj_vial4 != null) && (obj_vial5 != null) && (obj_vial6 != null))
        {
            if ((obj_vial1.transform.name == "Vial9") && (obj_vial2.transform.name == "Vial10") &&
            (obj_vial3.transform.name == "Vial6") && (obj_vial4.transform.name == "Vial1") &&
            (obj_vial5.transform.name == "Vial3") && (obj_vial6.transform.name == "Vial4") &&
            (obj_vial7.transform.name == "Vial2") && (obj_vial8.transform.name == "Vial7") &&
            (obj_vial9.transform.name == "Vial5") && (obj_vial10.transform.name == "Vial8"))
            {
                is_sorted = true;
                Task6_Weights.GetComponent<Animator>().Play("CorrectWeights");
                Debug.Log("VIALS ARE SORTED");

                // --- TRACKING: puzzle completado ---
                PuzzleTracker.Instance?.CompletePuzzle(PuzzleId);
            }
        }
        else
        {
            Task6_Weights.GetComponent<Animator>().Play("WeightsIDLE");
        }
    }
}