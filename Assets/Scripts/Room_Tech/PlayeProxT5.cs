using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayeProxT5 : MonoBehaviour
{
    public bool isPlayerInside;
    public GameObject CompanionController;
    public GameObject BotAnimation;
    public GameObject TaskCompleted;

    public AudioSource BotVoice;
    public AudioClip voice_default, voice_result;

    public bool defaultPlayedT5, resultPlayed;

    private Companion companion;
    private HanoiManager manager;

    void Start()
    {
        isPlayerInside = false;
        defaultPlayedT5 = false;
        resultPlayed = false;

        BotAnimation.SetActive(false);

        companion = CompanionController.GetComponent<Companion>();
        manager = TaskCompleted.GetComponent<HanoiManager>();
    }

    void Update()
    {
        if (!isPlayerInside)
            return;

        // DEFAULT DIALOGUE
        if (companion.CompanionBot.activeSelf
            && !manager.panex_completed
            && !defaultPlayedT5)
        {
            companion.CompanionBot.SetActive(false);

            BotAnimation.SetActive(true);
            BotAnimation.GetComponent<Animator>().Play("Bot_T5");

            BotVoice.PlayOneShot(voice_default);

            defaultPlayedT5 = true;
        }
        // RESULT DIALOGUE
        else if (companion.CompanionBot.activeSelf
                 && manager.panex_completed
                 && !resultPlayed)
        {
            companion.CompanionBot.SetActive(false);

            BotAnimation.SetActive(true);
            BotAnimation.GetComponent<Animator>().Play("Bot_T5R");

            BotVoice.PlayOneShot(voice_result);

            resultPlayed = true;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInside = false;
    }
}
