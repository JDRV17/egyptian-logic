using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class T2_StepChecker : MonoBehaviour
{

    public XRSocketInteractor[] interactors;
    public GameObject objectToMove;
    public float stepSize = 1.0f;
    T2_MazeMovement Excecutor;

    private int currentStepIndex = 0;

    void Start()
    {
        Excecutor = GetComponent<T2_MazeMovement>();
        foreach (XRSocketInteractor interactor in interactors)
        {
            if (interactor != null)
            {
                interactor.onSelectEntered.AddListener(OnSelectEntered);
            }
        }
    }

    void OnSelectEntered(XRBaseInteractable interactable)
    {
        // if(Excecutor.isPressed == true){
        if (interactable.gameObject.CompareTag("MoveForward"))
        {
            MoveForward();
        }
        else if (interactable.gameObject.CompareTag("RotateLeft"))
        {
            RotateLeft();
        }
        else if (interactable.gameObject.CompareTag("RotateRight"))
        {
            RotateRight();
        }
        // }
        // else{
        //     HomeReturn();
        // }
    }

    void MoveForward()
    {
        Vector3 newPosition = objectToMove.transform.position + (objectToMove.transform.up * stepSize);
        objectToMove.transform.position = newPosition;
        currentStepIndex++;
    }

    void RotateLeft()
    {
        objectToMove.transform.Rotate(Vector3.forward, 90.0f);
        currentStepIndex++;
    }

    void RotateRight()
    {
        objectToMove.transform.Rotate(Vector3.forward, -90.0f);
        currentStepIndex++;
    }

    void HomeReturn()
    {
        objectToMove.transform.localPosition = new Vector3(-0.420100003f,1.27900004f,-0.173899993f);
    }

    void Update()
    {
        if (currentStepIndex >= interactors.Length)
        {
            Debug.Log("Maze solved!");
        }
    }
  
    // public XRSocketInteractor socket;
    // public string textToExcecute;

    // void Start()
    // {
    //     socket = GetComponent<XRSocketInteractor>();
        
    // }

    // void Update() {
    //     socketCheck();
    // }
 
    // void socketCheck()
    // {
    //     IXRSelectInteractable objName = socket.GetOldestInteractableSelected();
    //     if (objName != null){
    //         // Debug.Log(objName.transform.name + " in socket of " + transform.name);

            
    //         //              Z+ is UP
    //         //      X- is LEFT       X+ is RIGHT
    //         //              Z- is DOWN
            
    //         if (objName.transform.name == "MoveFORWARD")
    //         {
    //             textToExcecute = "MF1";
    //             // Debug.Log(textToExcecute);
    //         }
    //         else if (objName.transform.name == "LeftTURN")
    //         {
    //             textToExcecute = "RL";
    //             // Debug.Log(textToExcecute);
    //         }
    //         else if (objName.transform.name == "RightTURN")
    //         {
    //             textToExcecute = "RR";
    //             // Debug.Log(textToExcecute);
    //         }
    //         Debug.Log(textToExcecute);
    //     }
    //     else{
    //         textToExcecute = "NOTHING";
    //     }
    // }  
}
