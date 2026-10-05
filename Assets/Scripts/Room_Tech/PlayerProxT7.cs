using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProxT7 : MonoBehaviour
{
    public bool isPlayerInside;
    public GameObject CompanionController;
    public GameObject BotAnimation;
    public GameObject TaskCompleted;
    public AudioSource BotVoice;
    public AudioClip voice_default, voice_result;
    public bool defaultPlayedT7, resultPlayed, bot_active;
    
    // Start is called before the first frame update
    void Start()
    {
        isPlayerInside = false;
        defaultPlayedT7 = false;
        resultPlayed = false;
        BotAnimation.SetActive(false);
    }
    void Update() {
        if (isPlayerInside)
        {
            // if(CompanionController.GetComponent<Companion>().CompanionBot.active){
            //     CompanionController.GetComponent<Companion>().CompanionBot.active = false;
            //     if(!BotVoice.isPlaying){
            //         if(!defaultPlayedT7){
            //             BotAnimation.SetActive(true);
            //             BotVoice.PlayOneShot(voice_default);
            //             BotAnimation.GetComponent<Animator>().Play("Bot_T4");
            //             defaultPlayedT7 = true;
            //         }
                    
            //         else if(TaskCompleted.GetComponent<T4_GridChecker>().FigurePrinted == true ){
            //             defaultPlayedT7 = true;
            //             if(!resultPlayed){
            //                 BotVoice.PlayOneShot(voice_result);
            //                 BotAnimation.GetComponent<Animator>().Play("Bot_T4R");
            //                 resultPlayed = true;
            //             }
                        
            //         } 
            //     }
            // }
            
            if (CompanionController.GetComponent<Companion>().CompanionBot.active && !TaskCompleted.GetComponent<T7_GraphChecker>().GraphFinished && !defaultPlayedT7)
            {
                
                CompanionController.GetComponent<Companion>().CompanionBot.SetActive(false);
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T7");
                BotVoice.PlayOneShot(voice_default);
                defaultPlayedT7 = true;
                
            }
            // Play result animation and sound
            else if (CompanionController.GetComponent<Companion>().CompanionBot.active && TaskCompleted.GetComponent<T7_GraphChecker>().GraphFinished && !resultPlayed)
            {
                CompanionController.GetComponent<Companion>().CompanionBot.active = false;
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T7R");
                BotVoice.PlayOneShot(voice_result);
                defaultPlayedT7 = true;
                resultPlayed = true;
            }
            // Turn off animation if neither condition is met
            
            
        }
        // else{
        //     BotAnimation.SetActive(false);
        // }
    }
    public void OnTriggerEnter(Collider other){
        
        if (other.tag == "Player")
        {
            isPlayerInside = true;
            
        }
        
    }
    private void OnTriggerExit(Collider other){
        if (other.tag == "Player"){
            isPlayerInside = false;
            // BotAnimation.SetActive(false);
        }
    }

    
}
