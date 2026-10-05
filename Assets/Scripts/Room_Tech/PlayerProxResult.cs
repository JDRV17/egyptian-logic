using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProxResult : MonoBehaviour
{
    public bool isPlayerInside;
    public GameObject CompanionController;
    public GameObject BotAnimation;
    public GameObject TaskCompleted;
    public AudioSource BotVoice;
    public AudioClip voice_default, voice_result;
    public bool defaultPlayed, resultPlayed, bot_active;
    
    // Start is called before the first frame update
    void Start()
    {
        isPlayerInside = false;
        defaultPlayed = false;
        resultPlayed = false;
        BotAnimation.SetActive(false);
    }
    void Update() {
        if (isPlayerInside)
        {
            // if(CompanionController.GetComponent<Companion>().CompanionBot.active){
            //     CompanionController.GetComponent<Companion>().CompanionBot.active = false;
            //     if(!BotVoice.isPlaying){
            //         if(!defaultPlayed){
            //             BotAnimation.SetActive(true);
            //             BotVoice.PlayOneShot(voice_default);
            //             BotAnimation.GetComponent<Animator>().Play("Bot_T4");
            //             defaultPlayed = true;
            //         }
                    
            //         else if(TaskCompleted.GetComponent<T4_GridChecker>().FigurePrinted == true ){
            //             defaultPlayed = true;
            //             if(!resultPlayed){
            //                 BotVoice.PlayOneShot(voice_result);
            //                 BotAnimation.GetComponent<Animator>().Play("Bot_T4R");
            //                 resultPlayed = true;
            //             }
                        
            //         } 
            //     }
            // }
            
            if (CompanionController.GetComponent<Companion>().CompanionBot.active && !TaskCompleted.GetComponent<T4_GridChecker>().FigurePrinted && !defaultPlayed)
            {
                
                CompanionController.GetComponent<Companion>().CompanionBot.SetActive(false);
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T4");
                BotVoice.PlayOneShot(voice_default);
                defaultPlayed = true;
                
            }
            // Play result animation and sound
            else if (CompanionController.GetComponent<Companion>().CompanionBot.active && TaskCompleted.GetComponent<T4_GridChecker>().FigurePrinted && !resultPlayed)
            {
                CompanionController.GetComponent<Companion>().CompanionBot.active = false;
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T4R");
                BotVoice.PlayOneShot(voice_result);
                defaultPlayed = true;
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
