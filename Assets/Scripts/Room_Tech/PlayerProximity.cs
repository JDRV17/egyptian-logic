using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProximity : MonoBehaviour
{
    public bool isPlayerInside;
    public GameObject CompanionController;
    public GameObject CompanionBot;
    public AudioSource BotVoice;
    public AudioClip voice_default;
    
    // Start is called before the first frame update
    void Start()
    {
        isPlayerInside = false;
    }

     public void OnTriggerEnter(Collider other){
        
        if (other.tag == "Player")
        {
            isPlayerInside = true;
            if(CompanionController.GetComponent<Companion>().CompanionBot.active){
                if(!BotVoice.isPlaying){
                    BotVoice.PlayOneShot(voice_default);
                }
            }
        }
        else{
            isPlayerInside = false;
        }
    }
    private void OnTriggerExit(Collider other){
        if (other.tag == "Player"){
            isPlayerInside = false;
        }
    }
}
