using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class T5_PanexEnabler : MonoBehaviour
{
    public XRSocketInteractor socket;
    public bool is_active;

    public GameObject panex;
    public GameObject PrintedCube;
    private Animation anim;

    // Start is called before the first frame update
    void Start()
    {
        is_active = false;
        panex.GetComponent<Animator>().Play("PanexIDLE");
    }

    // Update is called once per frame
    void Update()
    {
        CheckObject();
    }
    public void CheckObject(){
        IXRSelectInteractable objName = socket.GetOldestInteractableSelected();
        if (objName != null){
            if(objName.transform.name == "Printed"){
                panex.GetComponent<Animator>().Play("PanexEnable");
                is_active = true;
                PrintedCube.SetActive(false);
                return;
            }        
            else{
                panex.GetComponent<Animator>().Play("PanexIDLE");
            }    
        }
    }
}
