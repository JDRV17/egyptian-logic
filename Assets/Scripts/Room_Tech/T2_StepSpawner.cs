using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class T2_StepSpawner : MonoBehaviour
{
    public Transform SpawnPoint;
    public GameObject Card;
    public GameObject button;
    public UnityEvent onPressed, onReleased;
    
    GameObject presser;
    bool isPressed;
    void Start()
    {
        isPressed = false;
    }
    private void OnTriggerEnter(Collider other){
        if(!isPressed && (other.tag == "PlayerHand")){
            button.transform.localPosition = new Vector3(0, 0.003f, 0);
            presser = other.gameObject;
            onPressed.Invoke();
            GameObject newObject = Instantiate(Card, SpawnPoint.position, SpawnPoint.rotation);
            newObject.name = Card.name;
            newObject.transform.localScale = new Vector3(0.07749537f, 0.07749537f, 0.07749537f);
            isPressed = true;
        }
        
    }
    private void OnTriggerExit(Collider other) {
        if(other.gameObject == presser){
            button.transform.localPosition = new Vector3(0, 0.015f, 0);
            onReleased.Invoke();
            isPressed = false;
        }
    }
    // void onTriggerEnter(){
    //     Instantiate(Card, SpawnPoint.position, SpawnPoint.rotation);
    // }

}