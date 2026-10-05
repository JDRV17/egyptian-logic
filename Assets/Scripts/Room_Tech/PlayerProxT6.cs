using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProxT6 : MonoBehaviour
{
    public bool isPlayerInside;
    public GameObject CompanionController;
    public GameObject BotAnimation;
    public GameObject TaskCompleted;
    public AudioSource BotVoice;
    public AudioClip voice_default, voice_result;
    public bool defaultPlayedT6, resultPlayed, bot_active;
    
    // Start is called before the first frame update
    void Start()
    {
        isPlayerInside = false;
        defaultPlayedT6 = false;
        resultPlayed = false;
    }
    void Update() {
        if (isPlayerInside)
        {
            // if(CompanionController.GetComponent<Companion>().CompanionBot.active){
            //     CompanionController.GetComponent<Companion>().CompanionBot.active = false;
            //     if(!BotVoice.isPlaying){
            //         if(!defaultPlayedT6){
            //             BotAnimation.SetActive(true);
            //             BotVoice.PlayOneShot(voice_default);
            //             BotAnimation.GetComponent<Animator>().Play("Bot_T4");
            //             defaultPlayedT6 = true;
            //         }
                    
            //         else if(TaskCompleted.GetComponent<T4_GridChecker>().FigurePrinted == true ){
            //             defaultPlayedT6 = true;
            //             if(!resultPlayed){
            //                 BotVoice.PlayOneShot(voice_result);
            //                 BotAnimation.GetComponent<Animator>().Play("Bot_T4R");
            //                 resultPlayed = true;
            //             }
                        
            //         } 
            //     }
            // }
            
            if (CompanionController.GetComponent<Companion>().CompanionBot.active && !TaskCompleted.GetComponent<T6_Weight_Sort>().is_sorted && !defaultPlayedT6)
            {
                
                CompanionController.GetComponent<Companion>().CompanionBot.SetActive(false);
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T6");
                BotVoice.PlayOneShot(voice_default);
                defaultPlayedT6 = true;
                
            }
            // Play result animation and sound
            else if (CompanionController.GetComponent<Companion>().CompanionBot.active && TaskCompleted.GetComponent<T6_Weight_Sort>().is_sorted && !resultPlayed)
            {
                CompanionController.GetComponent<Companion>().CompanionBot.active = false;
                BotAnimation.SetActive(true);
                BotAnimation.GetComponent<Animator>().Play("Bot_T6R");
                BotVoice.PlayOneShot(voice_result);
                defaultPlayedT6 = true;
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
