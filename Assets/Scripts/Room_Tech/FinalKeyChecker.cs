using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FinalKeyChecker : MonoBehaviour
{
    public XRSocketInteractor Key1Socket, Key2Socket;
    public bool keyAccepted;
    public GameObject TableAnim;
    // Start is called before the first frame update
    void Start()
    {
        keyAccepted = false;
        TableAnim.GetComponent<Animator>().Play("Table_IDLE");
    }

    // Update is called once per frame
    void Update()
    {
        CheckFinalKeys();
    }

    void CheckFinalKeys(){
        IXRSelectInteractable key1_name = Key1Socket.GetOldestInteractableSelected();
        IXRSelectInteractable key2_name = Key2Socket.GetOldestInteractableSelected();
        if((key1_name != null) && (key2_name != null)){
            
            if((key1_name.transform.name == "Key1") && (key2_name.transform.name == "Key2")){
                keyAccepted = true;
                TableAnim.GetComponent<Animator>().Play("Table_UNLOCK");
                // Debug.Log("Keys in place, Final Key delivered");
                return;
            }
        }
        // else{
        //     
        // }
    }
    
}
