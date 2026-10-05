using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChestOpener : MonoBehaviour
{

    public XRSocketInteractor socket;
    public bool is_open;
    public GameObject Chest;
    private Animation anim;
    // Start is called before the first frame update
    void Start()
    {
        is_open = false;
        socket = GetComponent<XRSocketInteractor>();
    }

    // Update is called once per frame
    void Update()
    {
        KeyChecker();
    }

    public void KeyChecker(){
        IXRSelectInteractable objName = socket.GetOldestInteractableSelected();
        if (objName != null){
            if ((objName.transform.name == "FinalKey")){
                Chest.GetComponent<Animator>().Play("KeyContainer_OPEN");
                is_open = true;
                return;
            }
            else{
                Chest.GetComponent<Animator>().Play("KeyContainer_IDLE");
            }
        }
    }
}
