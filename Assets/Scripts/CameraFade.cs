using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraFade : MonoBehaviour
{
    public bool fadeOnStart = true;
    public GameObject Fader;
    public GameObject Door;
    public GameObject RoomTimer;
    private Animation fader_animation;

    void Start() {
        if(fadeOnStart){
            FadeIn();
        }
    }

    void Update(){
        if(Door.GetComponent<DoorAccess>().is_open == true){
            FadeOut();
        }
        if(RoomTimer.GetComponent<Timer>().timeRemaining == 0){
            Fader.GetComponent<Animator>().Play("FadeTime");
            WaitRestartCoroutine();
        }
    }

    public void FadeIn(){
        Fader.GetComponent<Animator>().Play("FadeIn");
    }

    public void FadeOut(){
        Fader.GetComponent<Animator>().Play("FadeOut");
        WaitMenuCoroutine();
        
    }

    IEnumerator WaitRestartCoroutine()
    {
        yield return new WaitForSeconds(4);
        RestartGame();
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Restart the current scene
    }

    IEnumerator WaitMenuCoroutine()
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene("MainMenu");
    }
    
}
