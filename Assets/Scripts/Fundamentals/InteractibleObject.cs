﻿using System.Collections;
using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;

public class InteractibleObject : DashInteractable
{
    public const string BrokenBool = "Broken";

    public enum InteractType
    {
        Death,
        Projectile,
        Fuse,
        Goal,
        Block,
        Break,
        Candle,
        PickUp,
        FusePoint,
        Damage,
        DangerZone,
        BurnableProp,
        PopUp
    }
    public InteractType type;
    public float DamageValue;
    public float HealValue;
    public float BumpHeight = 0.3f;
    private MovementController movementController;
    private List<AudioEvent> audioEvents;
    //private DialogueRunner dialogRunner;
    private PopUpObject popUpObject;
    private BreakablesParticleManager _breakablesParticleManager;

    //disabling highlightShader
    private Material[] thisMaterial; 

    public bool IsBreakable { get; set; }

    //CameraShake
    CameraShake cameraShake;
    private float chargedDashShakeDur = 0.2f;
    private float breakBounceShakeDur = 0.1f;
    private float breakShake = 0.4f;

    //timeSlowdown
    TimeSlowdown timeSlowdown;

    private void Start()
    {
        if (type == InteractType.Break)
            IsBreakable = true;
        else if (type == InteractType.PickUp)
            IsBreakable = true;
        else
            IsBreakable = false;

        if ((gameObject.tag =="pic1") || (gameObject.tag == "pic2") || (gameObject.tag == "pic3") || (gameObject.tag == "pic4"))
        {
            thisMaterial = GetComponent<Renderer>().materials;
        }



        // This is null most of the time
        popUpObject = this.gameObject.GetComponent<PopUpObject>();
    }

    public override void Interact(Vector3 hitPoint)
    {
        Vector3 hitpoint = new Vector3(hitPoint.x, BumpHeight, hitPoint.z);
        if (movementController == null)
        {
            AssignDependencies();
        }
        switch (type)
        {
            case InteractType.Projectile:
                Projectile();
                break;
            case InteractType.Goal:
                Goal();
                break;
            case InteractType.Block:
                Block(hitpoint);
                break;
            case InteractType.Break:
                Break(hitpoint);
                break;
            case InteractType.Candle:
                Candle();
                break;
            case InteractType.PickUp:
                PickUp();
                break;
            case InteractType.FusePoint:
                FusePoint(hitpoint);
                break;
            case InteractType.Death:
                Death(hitpoint);
                break;
            case InteractType.Fuse:
                if (!movementController.IsFuseMoving)
                    Block(hitpoint);
                break;
            case InteractType.Damage:
                DamagePlayer(hitpoint);
                break;
            case InteractType.DangerZone:
                DangerZone();
                break;
            case InteractType.BurnableProp:
                BurnProp(hitpoint);
                break;
            case InteractType.PopUp:
                PopUp();
                break;
        }
    }

    public void Death(Vector3 hitpoint)
    {
        if (movementController == null)
            AssignDependencies();

        if (!movementController.IsInvulnerable)
        {
            Vector3 targetPosition = hitpoint + movementController.transform.forward * movementController.BounceValue;
            movementController.Die(false, targetPosition);
            //spawn particle for death

         
           /* var particleSystemEN = gameObject.GetComponentInChildren<ParticleSystem>();
            var em = particleSystemEN.emission;
            em.enabled = true;
            particleSystemEN.Play();
            */


            Vibration.Vibrate(80);
            StartCoroutine(deathParticleSys()); 

            //dialogRunner.StartDialogue("Death");
        }
    }

    IEnumerator deathParticleSys()
    {
        //yield return new WaitForSeconds(0.3f);
        var particleSystemEN = gameObject.GetComponentInChildren<ParticleSystem>();
        var em = particleSystemEN.emission;
        em.enabled = true;
        particleSystemEN.Play();
        yield return new WaitForSeconds(2f);
        em.enabled = false;
        particleSystemEN.Stop();

    }

    private void Projectile()
    {
        if (movementController.IsMoving)
        {
            gameObject.GetComponent<BurnObject>().SetObjectOnFire(new Vector3(0, 0, 0));
            gameObject.GetComponent<DashInteractable>().Interact(GameObject.FindGameObjectWithTag("Player"));
            //movementController.CollideProjectile();
        }
    }

    private void Goal()
    {
        gameObject.GetComponent<BoxCollider>().enabled = false;
        //dialogRunner.StartDialogue("Goal");
        movementController.Win(gameObject.transform.position);
    }

    private void Block(Vector3 hitpoint)
    {
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ObstacleBlock, audioEvents, gameObject);
        cameraShake.setShakeElapsedTime(breakShake / 2);
        //dialogRunner.StartDialogue("Block");
        movementController.Collide(hitpoint);
    }

    private void Break(Vector3 hitpoint)
    {
        if (movementController.IsDashing)
        {
            AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ObstacleBreak, audioEvents, gameObject);
            gameObject.GetComponentInChildren<Animator>()?.SetBool(BrokenBool, true);
            cameraShake.setShakeElapsedTime(breakShake);
            //timeSlowdown.doSlowmotion();
            gameObject.GetComponent<BurnObject>().SetObjectOnFire(hitpoint);
            //dialogRunner.StartDialogue("Break");
            movementController.UpdateFireAmount(-HealValue);
            FindObjectOfType<PlayerActionsCollectorQA>().DataConteiner.DestroyedObjectsCount++;
            _breakablesParticleManager.PollBreakableParticles(hitpoint);
        }
        else
        {
            cameraShake.setShakeElapsedTime(breakBounceShakeDur);
            AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ObstacleBreakMute, audioEvents, gameObject);
            movementController.Collide(hitpoint);
        }
    }

    private void BurnProp(Vector3 hitpoint)
    {
        gameObject.GetComponent<BurnObject>().SetObjectOnFire(hitpoint);
        movementController.UpdateFireAmount(-HealValue);
    }

    private void PopUp()
    {
        // If this is a pop-up Object, trigger the pop-up
        //if (popUpObject)
        //{
        //    thisMaterial[1].
        //    popUpObject.PopUp();
        //}
        var mat = thisMaterial[1];
        mat.SetFloat("_Highlighted", 0);
        popUpObject.PopUp();      
    }

    private void Candle()
    {
        Light light = gameObject.GetComponentInChildren<Light>();
        light.enabled = true;
    }

    private void PickUp()
    {
        //dialogRunner.StartDialogue("PickUp");
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.BurningItem, audioEvents, gameObject);
        gameObject.GetComponent<Collider>().enabled = false;
        gameObject.SetActive(false);
        //movementController.CollidePickUp();

        movementController.UpdateFireAmount(-HealValue);
    }

    private void FusePoint(Vector3 hitpoint)
    {
        if (!movementController.IsFuseMoving)
        {
            movementController.TargetPosition = hitpoint;
            movementController.UpcomingFusePoint = gameObject;
            movementController.FuseEvent.AddListener(movementController.CollideFusePoint);
        }
    }

    private void DamagePlayer(Vector3 hitpoint)
    {
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.Damage, audioEvents, gameObject);
        movementController.UpdateFireAmount(DamageValue);
        movementController.CheckFireLeft();
        movementController.TargetPosition = hitpoint;
    }

    private void DangerZone()
    {
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.DangerZone, audioEvents, gameObject);
        movementController.UpdateFireAmount(DamageValue * Time.deltaTime);
        movementController.CheckFireLeft();
    }

    public override void Interact(GameObject player)
    {

    }

    private bool PointInOABB(Vector3 point, BoxCollider box)
    {
        point = box.transform.InverseTransformPoint(point) - box.center;

        float halfX = (box.size.x * 0.5f);
        //float halfY = (box.size.y * 0.5f);
        float halfZ = (box.size.z * 0.5f);

        return (point.x < halfX && point.x > -halfX &&
           //point.y < halfY && point.y > -halfY &&
           point.z < halfZ && point.z > -halfZ);
    }

    private void AssignDependencies()
    {
        movementController = FindObjectOfType<MovementController>();
        audioEvents = new List<AudioEvent>(GetComponents<AudioEvent>());
        //dialogRunner = FindObjectOfType<DialogueRunner>();
        timeSlowdown = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<TimeSlowdown>();
        cameraShake = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CameraShake>();
        _breakablesParticleManager = FindObjectOfType<BreakablesParticleManager>();
    }

    public override string ToString()
    {
        return type.ToString() + " " + gameObject.name;
    }
}
