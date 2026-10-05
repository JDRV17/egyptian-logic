using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    public float timeRemaining = 10;
    public bool timerIsRunning = false;
    public bool anim_finished = false;
    public Text timeText;
    

    public GameObject BotAnimation;
    public AudioSource BotVoice;
    public AudioClip start_voice;
    public bool anim_started;


    private void Start()
    {
        // Starts the timer automatically
        anim_started = false;
        StartCoroutine(ExampleCoroutine());
        // timerIsRunning = true;
    }
    void Update()
    {
        // if(!anim_finished && StartAnimation.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Bot_Start")){
        //     timerIsRunning = true;
        //     anim_finished = true;
        // }
        if(!anim_started){
            anim_started = true;
            BotAnimation.SetActive(true);
            BotVoice.PlayOneShot(start_voice);
            BotAnimation.GetComponent<Animator>().Play("Bot_Start");
            
        }

        // timerIsRunning = true;
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timerIsRunning = false;
            }
        }
    }
    IEnumerator ExampleCoroutine()
    {
        yield return new WaitForSeconds(52);
        timerIsRunning = true;
    }
    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60); 
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
