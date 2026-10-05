using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DoorAccess : MonoBehaviour
{
    public XRSocketInteractor socket;
    public bool is_open, soundplayed;
    public GameObject door;
    private Animation anim;
    public AudioSource DoorSource;
    public AudioClip DoorAudio;

    void Start()
    {
        is_open = false;
        soundplayed = false;
    }

    void Update()
    {
        keyCheck();
    }

    public void keyCheck()
    {
        IXRSelectInteractable objName = socket.GetOldestInteractableSelected();
        if (objName != null)
        {
            if (objName.transform.name == "Keycard")
            {
                door.GetComponent<Animator>().Play("Door_OPEN");
                if (!soundplayed)
                {
                    DoorSource.PlayOneShot(DoorAudio);
                    soundplayed = true;

                    // --- TRACKING: la puerta se abrió por primera vez -> juego terminado ---
                    GameCompletionChecker.Instance?.NotifyGameFinished();
                }
                is_open = true;
                return;
            }
            else
            {
                door.GetComponent<Animator>().Play("Door_IDLE");
            }
        }
    }
}