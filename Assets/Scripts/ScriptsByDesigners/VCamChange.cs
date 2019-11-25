﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VCamChange : MonoBehaviour
{
    public GameObject vcam;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            StartCoroutine(switchToCam());
        }
 
    }


   /* void OnTriggerExit(Collider other)
    {
        switchVcamOff();
    } 
    */

   private IEnumerator switchToCam()
    {
        yield return new WaitForSeconds(2f);
        vcam.gameObject.SetActive(true);
        yield return new WaitForSeconds(5f);
        vcam.gameObject.SetActive(false);
    }
}