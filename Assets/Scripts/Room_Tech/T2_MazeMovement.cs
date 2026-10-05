using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class T2_MazeMovement : MonoBehaviour
{

    public GameObject button;
    public UnityEvent onPressed, onReleased;
    T2_StepExcecutor ConnectorMover;
    
    GameObject presser;
    public bool isPressed;
    void Start()
    {
        isPressed = false;
        ConnectorMover = GetComponent<T2_StepExcecutor>();
    }
    void Awake(){
        
    }

    private void OnTriggerEnter(Collider other){
        if(!isPressed){
            button.transform.localPosition = new Vector3(0, 0.003f, 0);
            presser = other.gameObject;
            onPressed.Invoke();
            isPressed = true;
        }
        
    }
    private void OnTriggerExit(Collider other) {
        if(other.gameObject == presser){
            button.transform.localPosition = new Vector3(0, 0.015f, 0);
            onReleased.Invoke();
            isPressed = false;
            // ConnectorMover.Excecutor();
        }
    }
   
}
