using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Companion_StartAnim : MonoBehaviour
{
    public GameObject BotAnimation;
    public AudioSource BotVoice;
    public AudioClip start_voice;
    public bool start_played, isPlayerInside;
    // Start is called before the first frame update
    void Start()
    {
        start_played = false;
    }

    // Update is called once per frame
    void Update()
    {
        // if(isPlayerInside && !start_played){
        //     BotAnimation.GetComponent<Animator>().Play("Bot_Start");
        //     BotVoice.PlayOneShot(start_voice);
        //     start_played = true;
        // }
    }


   
}
