using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class T6_BalanceLEFT : MonoBehaviour
{
    public XRSocketInteractor SocketLEFT;
    public GameObject WeightObjectLEFT;
    private GameObject vialActive;
    public Text ScreenText;
    private bool object_exited;
    // Start is called before the first frame update
    void Start()
    {
        WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
    }

    // Update is called once per frame
    void Update()
    {
        CheckWeight();
    }
    void CheckWeight(){
        IXRSelectInteractable objName = SocketLEFT.GetOldestInteractableSelected();
        if(objName != null){
            vialActive = objName.transform.gameObject;
            if(objName.transform.name == "Vial1"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.0";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial2"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.5";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial3"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.1";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial4"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.2";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial5"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "19.0";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial6"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.7";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial7"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "18.8";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial8"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "19.1";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial9"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.2";
                object_exited = false;
                return;
            }
            else if(objName.transform.name == "Vial10"){
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceLEFT");
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
                ScreenText.gameObject.SetActive(true);
                ScreenText.text = "17.5";
                object_exited = false;
                return;
            }
            else{
                WeightObjectLEFT.GetComponent<Animator>().Play("BalanceL_IDLE");
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
