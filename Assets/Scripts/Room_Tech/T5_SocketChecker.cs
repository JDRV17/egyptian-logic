using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class T5_SocketChecker : XRSocketInteractor
{
    [SerializeField]
    public string targetTag = string.Empty;

    // public override bool CanHover(XRBaseInteractable interactable){
    //     return base.CanHover(interactable) && MatchTag(interactable);
    // }
    // public override bool CanSelect(XRBaseInteractable interactable){
    //     return base.CanSelect(interactable) && MatchTag(interactable);
    // }

    // public bool MatchTag(XRBaseInteractable interactable){
    //     return interactable.CompareTag(targetTag);
    // }
    
}
