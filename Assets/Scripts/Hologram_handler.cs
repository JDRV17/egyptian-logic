using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hologram_handler : MonoBehaviour
{
    public float duration;
    Material myMat;

    void Start()
    {
        myMat = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        float phi = Time.time / duration * 2 * Mathf.PI;
        float amplitude = Mathf.Cos(phi) * 5f + 0.5F;
        float G = amplitude;
        float B = amplitude;
        myMat.SetColor("_EmissionColor", new Color(G, 1f, 1f));
        
    }
}
