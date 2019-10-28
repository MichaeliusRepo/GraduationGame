﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperPlane1 : DashInteractable
{
    [HideInInspector]
    public float speed = 5;

    private Collider lastCollision;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime,Space.Self);
    }

    private void OnTriggerEnter (Collider other)
    {
        Debug.Log("Plane Collision");
        lastCollision = other;
        if (lastCollision.gameObject.tag == "Obsticle")
            Destroy(gameObject);
        
    }

    public override void Interact(GameObject player)
    {
        Player1 playerScript = player.GetComponent<Player1>();

        Debug.Log("Collision with: " + gameObject.name);
        player.transform.SetParent(gameObject.transform);
        player.transform.position = player.transform.parent.position;
        player.GetComponent<Rigidbody>().useGravity = false;
        player.GetComponent<Rigidbody>().velocity = new Vector3();
        playerScript._attached = true;
    }
}


