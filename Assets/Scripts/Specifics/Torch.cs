﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Torch : MonoBehaviour
{
    private Light _light;
    private ParticleSystem _flame; 

    void Awake()
    {
        _light = gameObject.GetComponentInChildren<Light>();
        _flame = gameObject.GetComponentInChildren<ParticleSystem>();
        var em = _flame.emission;
        em.enabled = false; 
        _light.enabled = false;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag != "Player") return;
        var em = _flame.emission;
        em.enabled = true; 
        _light.enabled = true;
        Destroy(this);
    }
   /* private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Player") return;

        _light.enabled = true;
        Destroy(this);
    }*/
}
