using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class T6_BalanceRIGHT : MonoBehaviour
{
    public XRSocketInteractor SocketRIGHT;
    public GameObject WeightObjectRIGHT;
    private GameObject vialActive;
    public Text ScreenText;
    private bool object_exited;
    // Start is called before the first frame update
    void Start()
    {
        WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceR_IDLE");
    }

    // Update is called once per frame
    void Update()
    {
        CheckWeight();
    }
    void CheckWeight(){
        IXRSelectInteractable objName = SocketRIGHT.GetOldestInteractableSelected();
        if(objName != null){
            vialActive = objName.transform.gameObject;
            WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceR_IDLE");
            if(objName.transform.name == "Vial1"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.0";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial2"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.5";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial3"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.1";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial4"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.2";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial5"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "19.0";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial6"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.7";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial7"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.8";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial8"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "19.1";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial9"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.2";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial10"){
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceRIGHT");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.5";
                object_exited = false;
                return;
            }
            else{
                WeightObjectRIGHT.GetComponent<Animator>().Play("BalanceR_IDLE");
                ScreenText.text = "00.0";
                object_exited = true;
                return;
            }
            
            // vialActive.GetComponent<Text>().text = "20";
        }
        else{
            object_exited = true;
            ScreenText.text = "00.0";
        }
        
    }
}
