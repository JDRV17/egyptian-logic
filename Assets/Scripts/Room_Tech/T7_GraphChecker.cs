using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T7_GraphChecker : MonoBehaviour
{
    private const string PuzzleId = "pintar";

    public GameObject Area1, Area2, Area3, Area4, Area5,
    Area6, Area7, Area8, Area9, Area10, Area11,
    Area12, Area13, Area14, Area15, Area16, Area17,
    Area18, Area19, Area20;

    T7_ColorChanger CheckColor1, CheckColor2, CheckColor3, CheckColor4, CheckColor5,
    CheckColor6, CheckColor7, CheckColor8, CheckColor9, CheckColor10, CheckColor11,
    CheckColor12, CheckColor13, CheckColor14, CheckColor15, CheckColor16, CheckColor17,
    CheckColor18, CheckColor19, CheckColor20;

    public bool GraphFinished;

    // --- TRACKING ---
    private bool previousGraphFinished = false;
    private string[] previousMarkers = new string[20];
    private bool markersInitialized = false;

    void Start()
    {
        GraphFinished = false;
    }

    void Awake()
    {
        CheckColor1 = Area1.GetComponent<T7_ColorChanger>();
        CheckColor2 = Area2.GetComponent<T7_ColorChanger>();
        CheckColor3 = Area3.GetComponent<T7_ColorChanger>();
        CheckColor4 = Area4.GetComponent<T7_ColorChanger>();
        CheckColor5 = Area5.GetComponent<T7_ColorChanger>();
        CheckColor6 = Area6.GetComponent<T7_ColorChanger>();
        CheckColor7 = Area7.GetComponent<T7_ColorChanger>();
        CheckColor8 = Area8.GetComponent<T7_ColorChanger>();
        CheckColor9 = Area9.GetComponent<T7_ColorChanger>();
        CheckColor10 = Area10.GetComponent<T7_ColorChanger>();
        CheckColor11 = Area11.GetComponent<T7_ColorChanger>();
        CheckColor12 = Area12.GetComponent<T7_ColorChanger>();
        CheckColor13 = Area13.GetComponent<T7_ColorChanger>();
        CheckColor14 = Area14.GetComponent<T7_ColorChanger>();
        CheckColor15 = Area15.GetComponent<T7_ColorChanger>();
        CheckColor16 = Area16.GetComponent<T7_ColorChanger>();
        CheckColor17 = Area17.GetComponent<T7_ColorChanger>();
        CheckColor18 = Area18.GetComponent<T7_ColorChanger>();
        CheckColor19 = Area19.GetComponent<T7_ColorChanger>();
        CheckColor20 = Area20.GetComponent<T7_ColorChanger>();
    }

    void Update()
    {
        DetectColorChanges();
        ColorComboChecker();
    }

    // --- TRACKING: cuenta una interacción cada vez que cambia el color de alguna ficha ---
    void DetectColorChanges()
    {
        T7_ColorChanger[] all = {
            CheckColor1, CheckColor2, CheckColor3, CheckColor4, CheckColor5,
            CheckColor6, CheckColor7, CheckColor8, CheckColor9, CheckColor10,
            CheckColor11, CheckColor12, CheckColor13, CheckColor14, CheckColor15,
            CheckColor16, CheckColor17, CheckColor18, CheckColor19, CheckColor20
        };

        string[] currentMarkers = new string[20];
        for (int i = 0; i < 20; i++)
            currentMarkers[i] = all[i].currentMarker;

        if (!markersInitialized)
        {
            System.Array.Copy(currentMarkers, previousMarkers, 20);
            markersInitialized = true;
            return;
        }

        for (int i = 0; i < 20; i++)
        {
            if (currentMarkers[i] != previousMarkers[i])
            {
                PuzzleTracker.Instance?.RegisterInteraction(PuzzleId);
            }
        }

        System.Array.Copy(currentMarkers, previousMarkers, 20);
    }

    void ColorComboChecker()
    {
        if ((CheckColor1.currentMarker == "B") && (CheckColor2.currentMarker == "R") &&
        (CheckColor3.currentMarker == "B") && (CheckColor4.currentMarker == "R") &&
        (CheckColor5.currentMarker == "B") && (CheckColor6.currentMarker == "R") &&
        (CheckColor7.currentMarker == "B") && (CheckColor8.currentMarker == "R") &&
        (CheckColor9.currentMarker == "B") && (CheckColor10.currentMarker == "B") &&
        (CheckColor11.currentMarker == "B") && (CheckColor12.currentMarker == "R") &&
        (CheckColor13.currentMarker == "R") && (CheckColor14.currentMarker == "B") &&
        (CheckColor15.currentMarker == "B") && (CheckColor16.currentMarker == "R") &&
        (CheckColor17.currentMarker == "B") && (CheckColor18.currentMarker == "B") &&
        (CheckColor19.currentMarker == "R") && (CheckColor20.currentMarker == "B"))
        {
            Debug.Log("Correctly DONE");
            GraphFinished = true;
        }
        else if ((CheckColor1.currentMarker == "R") && (CheckColor2.currentMarker == "B") &&
        (CheckColor3.currentMarker == "R") && (CheckColor4.currentMarker == "B") &&
        (CheckColor5.currentMarker == "R") && (CheckColor6.currentMarker == "B") &&
        (CheckColor7.currentMarker == "R") && (CheckColor8.currentMarker == "B") &&
        (CheckColor9.currentMarker == "R") && (CheckColor10.currentMarker == "R") &&
        (CheckColor11.currentMarker == "R") && (CheckColor12.currentMarker == "B") &&
        (CheckColor13.currentMarker == "B") && (CheckColor14.currentMarker == "R") &&
        (CheckColor15.currentMarker == "R") && (CheckColor16.currentMarker == "B") &&
        (CheckColor17.currentMarker == "R") && (CheckColor18.currentMarker == "R") &&
        (CheckColor19.currentMarker == "B") && (CheckColor20.currentMarker == "R"))
        {
            Debug.Log("Correctly DONE");
            GraphFinished = true;
        }
        else
        {
            GraphFinished = false;
        }

        // --- TRACKING: puzzle completado (solo en la transición false -> true) ---
        if (GraphFinished && !previousGraphFinished)
        {
            PuzzleTracker.Instance?.CompletePuzzle(PuzzleId);
        }
        previousGraphFinished = GraphFinished;
    }
}