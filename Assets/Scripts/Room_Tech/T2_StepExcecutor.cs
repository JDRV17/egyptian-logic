using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class T2_StepExcecutor : MonoBehaviour
{

    public GameObject Step1, Step2, Step3, Step4, Step5, Step6, Step7, Step8, Step9, 
    Step10, Step11, Step12, Step13, Step14, Step15, Step16, Step17, Step18, Step19,
    Step20, Step21, Step22, Step23, Step24, Step25, Step26, Step27, Step28, Step29, 
    Step30;
    
    bool wall_detected = false;
    // bool forward_step = false;
    bool rotationLeft = false;
    // // bool rotationRight = false;

    // float forward_value = 0;

    // void Start()
    // {
        
    // }

    // void Awake() {
        
    // }

    // // Update is called once per frame
    // void Update()
    // {
    //     Excecutor();
    // }

    // void OnCollisionEnter (Collision col) {
        
    //     if (col.gameObject.tag == "Collider" ) {
    //         wall_detected = true;
    //         return;
    //     }
    // }
    

    // //              Z+ is UP
    // //      X- is LEFT       X+ is RIGHT
    // //              Z- is DOWN
    // public void Excecutor(){
    
    // // checkear paso por paso la accion realizada 
    // // girar o mover el conector segun el paso a realizar
        
    //     Invoke("One", 0.5f);
        
    //     // STEP 2
    //     if ((Step2.GetComponent<T2_StepChecker>().textToExcecute == "MF1") )
    //     {
    //         forward_value = 0.1f;
    //         Vector3 forward = new Vector3(0, forward_value, 0); 
    //         Debug.Log("Object should be moving");
    //         transform.Translate(forward * Time.deltaTime);
    //         if(wall_detected == true){
    //             Debug.Log("Object STOPPED");
    //             return;
    //         }
    //         return;
    //     }
    //     else if ((Step2.GetComponent<T2_StepChecker>().textToExcecute == "RL")){
    //         bool rotatedL = false;
    //         transform.RotateAround(transform.position, transform.forward, 90f);
    //         // transform.forward = 90f;
    //         rotatedL = true;
    //         return;
    //     }
    //     else if ((Step2.GetComponent<T2_StepChecker>().textToExcecute == "RR")){
    //         transform.localRotation = new Quaternion(0, 0, -90, 1);
    //     }
    // }

    // void One(){
    //     if ((Step1.GetComponent<T2_StepChecker>().textToExcecute == "MF1") )
    //     {
    //         forward_value = 0.1f;
    //         Vector3 forward = new Vector3(0, forward_value, 0); 
    //         // Debug.Log("Object should be moving");
    //         transform.Translate(forward * Time.deltaTime);
    //         if(wall_detected == true){
    //             Debug.Log("Object STOPPED");
    //             transform.Translate(Vector3.zero);
    //             return;
    //         }
    //         return;
    //     }
    //     else if ((Step1.GetComponent<T2_StepChecker>().textToExcecute == "RL")){
    //         bool rotatedL = false;
    //         transform.RotateAround(transform.position, transform.forward, 90f);
    //         // transform.forward = 90f;
    //         rotatedL = true;
    //         if (rotatedL == true){
    //             return;
    //         }
    //         return;
    //     }
    //     else if ((Step1.GetComponent<T2_StepChecker>().textToExcecute == "RR")){
    //         bool rotatedL = false;
    //         transform.RotateAround(transform.position, transform.forward, -90f);
    //         // transform.forward = 90f;
    //         rotatedL = true;
    //         if (rotatedL == true){
    //             return;
    //         }
    //         return;
    //     }
    // }

}
