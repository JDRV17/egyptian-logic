using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T7_KeyCardsActivator : MonoBehaviour
{
    public GameObject KeyCardsChecker;
    public bool isActive;
    public GameObject GraphAnim;
    // Start is called before the first frame update
    void Start()
    {
        GraphAnim.GetComponent<Animator>().Play("Graph_IDLE");
    }

    // Update is called once per frame
    void Update()
    {
        if((!isActive) && (KeyCardsChecker.GetComponent<FinalKeyChecker>().keyAccepted == true)){
            GraphAnim.GetComponent<Animator>().Play("Graph_ACTIVE");
        }
    }
}
