using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T3_combinationchecker : MonoBehaviour
{
    private const string PuzzleId = "botones";

    public GameObject Switch1, Switch2, Switch3, Switch4, Switch5, Switch6, Switch7, Switch8;
    public GameObject Object3D;
    AudioSource correctSound;
    private Animation anim;
    public bool Check1, Check2, Check3, Check4;
    T3_buttonVR CheckButton1, CheckButton2, CheckButton3, CheckButton4, CheckButton5, CheckButton6, CheckButton7, CheckButton8;
    public GameObject UpLights, RightLights, DownLights, LeftLights;
    private bool alreadyActivated = false;

    // --- TRACKING: snapshot del estado anterior para detectar cambios (rising/falling edge) ---
    private bool[] previousStates = new bool[8];
    private bool statesInitialized = false;

    private void Start()
    {
        Check1 = Check2 = Check4 = false;
        Check3 = true;

        if (Object3D != null)
            Object3D.SetActive(false);
    }

    void Awake()
    {
        CheckButton1 = Switch1.GetComponent<T3_buttonVR>();
        CheckButton2 = Switch2.GetComponent<T3_buttonVR>();
        CheckButton3 = Switch3.GetComponent<T3_buttonVR>();
        CheckButton4 = Switch4.GetComponent<T3_buttonVR>();
        CheckButton5 = Switch5.GetComponent<T3_buttonVR>();
        CheckButton6 = Switch6.GetComponent<T3_buttonVR>();
        CheckButton7 = Switch7.GetComponent<T3_buttonVR>();
        CheckButton8 = Switch8.GetComponent<T3_buttonVR>();
        correctSound = GetComponent<AudioSource>();
    }

    void Update()
    {
        DetectButtonChanges();
        comboChecker();
    }

    // --- TRACKING: revisa si algún switch cambió de estado desde el frame anterior ---
    void DetectButtonChanges()
    {
        bool[] currentStates = new bool[]
        {
            CheckButton1.LightActivated, CheckButton2.LightActivated,
            CheckButton3.LightActivated, CheckButton4.LightActivated,
            CheckButton5.LightActivated, CheckButton6.LightActivated,
            CheckButton7.LightActivated, CheckButton8.LightActivated
        };

        if (!statesInitialized)
        {
            // primer frame: solo guardamos el estado inicial, no contamos interacción
            System.Array.Copy(currentStates, previousStates, 8);
            statesInitialized = true;
            return;
        }

        for (int i = 0; i < 8; i++)
        {
            if (currentStates[i] != previousStates[i])
            {
                PuzzleTracker.Instance?.RegisterInteraction(PuzzleId);
            }
        }

        System.Array.Copy(currentStates, previousStates, 8);
    }

    void comboChecker()
    {
        if (CheckButton1.LightActivated == true && CheckButton2.LightActivated == false)
        {
            Check1 = true;
            UpLights.GetComponent<Animator>().Play("UPLightsOK");
        }
        else
        {
            Check1 = false;
            UpLights.GetComponent<Animator>().Play("UPLightsNO");
        }

        if (CheckButton3.LightActivated == true && CheckButton4.LightActivated == true)
        {
            Check2 = true;
            RightLights.GetComponent<Animator>().Play("RLightsON");
        }
        else
        {
            Check2 = false;
            RightLights.GetComponent<Animator>().Play("RLightsNO");
        }

        if (CheckButton5.LightActivated == false && CheckButton6.LightActivated == false)
        {
            Check4 = true;
            LeftLights.GetComponent<Animator>().Play("LLightsON");
        }
        else
        {
            Check3 = false;
            LeftLights.GetComponent<Animator>().Play("LLightsNO");
        }

        if (CheckButton7.LightActivated == false && CheckButton8.LightActivated == true)
        {
            Check3 = true;
            DownLights.GetComponent<Animator>().Play("DOWNLightsON");
        }
        else
        {
            Check4 = false;
            DownLights.GetComponent<Animator>().Play("DOWNLightsNO");
        }

        if ((Check1 == true) && (Check2 == true) && (Check3 == true) && (Check4 == true))
        {
            Debug.Log("LOGIC GATES COMPLETED");
            if (!alreadyActivated && Object3D != null)
            {
                Object3D.SetActive(true);
                alreadyActivated = true;
                if (correctSound != null)
                    correctSound.Play();

                // --- TRACKING: puzzle completado (solo la primera vez) ---
                PuzzleTracker.Instance?.CompletePuzzle(PuzzleId);
            }
        }
    }
}