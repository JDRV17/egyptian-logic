using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class buttonLightActivation : MonoBehaviour
{
    public Light ButtonLightObject;
    public bool LightEnabled  = false;
    public UnityEvent Light;
    void Update ()
    {
        if (LightEnabled == true){
            LightEnabled = false;
            ButtonLightObject.enabled = LightEnabled;
        }
        else {
            LightEnabled = true;
            ButtonLightObject.enabled = LightEnabled;
        }
    }
}
