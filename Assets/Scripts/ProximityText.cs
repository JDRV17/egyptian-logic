using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProximityText : MonoBehaviour
{
    public GameObject Chest;
    public string TextToDisplay;
    public Text ScreenText;
    public void Start(){
        ScreenText.gameObject.SetActive(false);
    }
    public void OnTriggerEnter(Collider other){
        if (other.tag == "Player" && (Chest.GetComponent<ChestOpener>().is_open == false))
        {
            ScreenText.gameObject.SetActive(true);
            ScreenText.text = TextToDisplay;
        }
        else{
            ScreenText.gameObject.SetActive(false);
        }
    }
    private void OnTriggerExit(Collider other){
        if (other.tag == "Player"){
            ScreenText.gameObject.SetActive(false);
            ScreenText.text = "";
        }
    }
}
