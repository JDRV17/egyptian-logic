using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionActions : MonoBehaviour
{

    public GameObject CompanionController;
    public GameObject CompanionBot;
    public GameObject AreaT2, AreaT3, AreaT4, AreaT5, AreaT6, AreaT7;
    public AudioClip  voiceKeyCardEnding, idle_1, idle_2, idle_3, idle_4, idle_5;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        BotActions();
    }

    void BotActions(){
        if(CompanionController.GetComponent<Companion>().CompanionBot.active){
            
            // if(){

            // }
            
            // else{
            //     CompanionBot.GetComponent<Animator>().Play("Companion_IDLE");
            // }
        }
    }
}
