﻿using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Yarn.Unity;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;


public class MovementController : MonoBehaviour
{
    [Header("General Settings")]
    [Tooltip("Value of pick ups that add to your amount of moves.")]
    public int PickUpValue = 3;
    [Tooltip("Amount of moves at the start of the game.")]
    public int AmountOfDashMoves = 10;
    [Tooltip("How much the player should bounce of a wall when colliding.")]
    public float BounceValue = 0.3f;

    private int maxAmountOfDashMoves;

    [Header("Move Settings")]
    [Tooltip("Duration of a move in seconds (how long it takes to get to target position).")]
    public float MoveDuration = 0.2f;
    [Tooltip("This gets multiplied by the drag distance (value between 0-1) to get the distance of a move.")]
    public float MoveDistanceFactor = 0.01f;
    [Tooltip("Easing function of the move.")]
    public Ease MoveEase = Ease.OutCubic;
    public float MoveDistance { get; set; }   

    [Header("Dash Settings")]
    [Tooltip("Time in seconds for how long you need to tap and hold for it to be recognized as a dash.")]
    public float DashThreshold = 0.25f;
    [Tooltip("Duration of a dash in seconds (how long it takes to get to target position).")]
    public float DashDuration = 0.1f;
    [Tooltip("Distance of a dash.")]
    public float DashDistance = 4;
    [Tooltip("Cost of a dash.")]
    public int DashCost = 1;
    [Tooltip("Easing function of the dash.")]
    public Ease DashEase = Ease.OutCubic;

    [Header("Canvas Fields")]
    private TMP_Text MovesText;

    private GameController gameController;
    private FireGirlAnimationController animationController;
    private Rigidbody rigidBody;
    private Material material;
    private TrailRenderer trailRenderer;
    private Vector3 previousPosition;
    private Tweener moveTweener;
    public List <AudioEvent> audioEvents;

    private AttachToPlane attachToPlane;

    private float colorValue = 1;
    private float changeTextColorDuration = 0.2f;

    private bool isOutOfMoves;
    private bool isCharged;
    private bool reachedGoal;
    public Vector3 TargetPosition;

    [Header("Scriptable Objects")]
    public FloatVariable GoalDistance;
    public FloatVariable GoalDistanceRelative;
    public FloatVariable HealthPercentage;

    private Vector3 startPosition;
    private Vector3 goalPosition;

    CameraShake cameraShake;
    private float chargedDashShakeDur = 0.2f;

    public bool IsMoving { get; set; }

    public bool IsFuseMoving { get; set; }

    public bool TriggerCoyoteTime { get; set; }

    public bool IsDashCharged { get; set; }

    public bool IsDashing { get; set; }
    public bool HasDied { get; set; }

    public GameObject UpcomingFusePoint;

    public UnityEvent FuseEvent;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
        animationController = GetComponentInChildren<FireGirlAnimationController>();
        material = GetComponent<Renderer>().material;
        trailRenderer = GetComponent<TrailRenderer>();
        gameController = FindObjectOfType<GameController>();
        audioEvents = GetComponents<AudioEvent>().ToList<AudioEvent>();
        attachToPlane = GetComponent<AttachToPlane>();

    }

    // Start is called before the first frame update
    private void Start()
    {
        MovesText = GameObject.Find("MovesText").GetComponent<TextMeshProUGUI>();
        cameraShake = GameObject.FindGameObjectWithTag("VirtualCamera").GetComponent<CameraShake>();

        MovesText.text = AmountOfDashMoves.ToString();

        trailRenderer.enabled = false;

        maxAmountOfDashMoves = AmountOfDashMoves;

        HealthPercentage.Value = ((float)AmountOfDashMoves / (float)maxAmountOfDashMoves) * 100f;

        gameController.IsPlaying = true;

        SetStartAndEndPositions();

        if (FuseEvent == null)
            FuseEvent = new UnityEvent();
    }

    private void Update()
    {
        Debug.DrawRay(new Vector3(transform.position.x, 0.5f, transform.position.z), transform.forward * DashDistance, Color.magenta);
    }

    public void SetStartAndEndPositions()
    {
        startPosition = rigidBody.position;

        var goal = GameObject.FindGameObjectWithTag("Goal");
        if (goal != null)
            goalPosition = goal.transform.position;
        else
            goalPosition = Vector3.zero;

        GoalDistanceRelative.Value = 0f;
        UpdateGoalDistances();
    }

    /// <summary>
    /// Visualizes charging the dash.
    /// </summary>
    public void ChargeDash()
    {
        if (!isCharged)
        {
            // Play Animation
            animationController.ChargeDash();

            AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ChargingDash, audioEvents, gameObject);
            isCharged = true;
        }
    }

    /// <summary>
    /// Performs Move Action.
    /// </summary>
    public void Move(Vector3 moveDirection)
    {
        if (IsMoving)
        {
            TriggerCoyoteTime = true;
            return;
        }

        previousPosition = transform.position;
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.Dash, audioEvents, gameObject);
        Vector3 targetPos = transform.position + moveDirection * MoveDistance;
        targetPos.y = transform.position.y;

        StartCoroutine(MoveRoutine(targetPos, MoveDuration));
    }

    /// <summary>
    /// Performs Dash Action.
    /// </summary>
    public void Dash(Vector3 dashDirection)
    {
        if (IsMoving)
        {
            TriggerCoyoteTime = true;
            return;
        }

        // checks if you have enough moves left for a dash
        int movesLeft = AmountOfDashMoves - DashCost;
        if (movesLeft < 0)
        {
            AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ChargingRejection, audioEvents, gameObject);
            StartCoroutine(ChangeTextColorRoutine());
            return;
        }

        attachToPlane.Detach(false);

        IsDashing = true;
        trailRenderer.enabled = true;
        previousPosition = transform.position;
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.ChargedDash, audioEvents, gameObject);
        previousPosition = transform.position;
        Vector3 targetPos = transform.position + dashDirection * DashDistance;
        
        //Play Animation
        animationController.Dash();

        StartCoroutine(MoveRoutine(targetPos, DashDuration));
    }

    /// <summary>
    /// Resets the charging dash state.
    /// </summary>
    public void ResetDash()
    {
        if (isCharged)
        {
            // Play Animation
            animationController.Cancel();
        }

        material.SetColor("_Color", Color.yellow);
        colorValue = 1f;
        IsDashCharged = false;
        isCharged = false;
    }

    /// <summary>
    /// CoRoutine responsible for moving the Player back (after a collision).
    /// </summary>
    private IEnumerator MoveBackRoutine(Vector3 target, float duration)
    {
        moveTweener?.Kill();

        IsMoving = true;

        moveTweener = rigidBody.DOMove(target, duration);

        yield return new WaitForSeconds(duration);

        DashEnded();
    }

    /// <summary>
    /// CoRoutine responsible for moving the Player.
    /// </summary>
    private IEnumerator MoveRoutine(Vector3 target, float duration)
    {
        moveTweener?.Kill();

        if (IsDashing)
        {
            cameraShake.setShakeElapsedTime(chargedDashShakeDur);
            UpdateDashMovesAmount();
        }

        TargetPosition = target;

        IsMoving = true;

        CheckCollision();

        moveTweener = rigidBody.DOMove(TargetPosition, duration);
        moveTweener.SetEase(IsDashing ? DashEase : MoveEase);
        yield return new WaitForSeconds(duration);

        CheckMovesLeft();
        DashEnded();

        FuseEvent.Invoke();
    }

    /// <summary>
    /// Checks the collision.
    /// </summary>
    private void CheckCollision()
    {
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(transform.position.x, 0.5f, transform.position.z), transform.forward, out hit,
            Vector3.Distance(transform.position, TargetPosition)))
        {
            InteractibleObject interactableObj = hit.transform.gameObject.GetComponent<InteractibleObject>();
            if (interactableObj == null)
                return;

            interactableObj.Interact(hit.point);
        }
    }

    public void StopMoving()
    {
        moveTweener?.Kill();
        StopCoroutine(nameof(MoveRoutine));
        StopCoroutine(nameof(MoveBackRoutine));
    }

    private void DashEnded()
    {
        AudioEvent.SendAudioEvent(AudioEvent.AudioEventType.DashEnded, audioEvents, gameObject);

        trailRenderer.enabled = false;
        IsMoving = false;

        if (IsDashing)
            IsDashing = false;

        HealthPercentage.Value = ((float)AmountOfDashMoves / (float)maxAmountOfDashMoves) * 100f;
        UpdateGoalDistances();

        // Play Animation
        animationController.Land();
    }

    private void UpdateGoalDistances()
    {
        if (goalPosition == Vector3.zero)
            return;

        GoalDistance.Value = (goalPosition - rigidBody.position).magnitude;
        var baseDistance = (goalPosition - startPosition).magnitude;
        GoalDistanceRelative.Value = (1f - GoalDistance.Value / baseDistance) * 100f;
        if (GoalDistanceRelative.Value < 0f)
            GoalDistanceRelative.Value = 0f;
    }

    /// <summary>
    /// CoRoutine responsible for changing the color of the moves text.
    /// </summary>
    private IEnumerator ChangeTextColorRoutine()
    {
        MovesText.DOColor(Color.red, changeTextColorDuration);
        yield return new WaitForSeconds(changeTextColorDuration);
        MovesText.DOColor(Color.white, changeTextColorDuration);
        yield return new WaitForSeconds(changeTextColorDuration);
    }

    /// <summary>
    /// Updates the moves text.
    /// </summary>
    private void UpdateDashMovesAmount()
    {
        AmountOfDashMoves -= DashCost;
        MovesText.text = AmountOfDashMoves.ToString();
    }

    /// <summary>
    /// Checks if the player has moves left.
    /// </summary>
    private void CheckMovesLeft()
    {
        if (AmountOfDashMoves <= 0)
        {
            isOutOfMoves = true;
            CheckGameEnd();
        }
    }

    /// <summary>
    /// Checks how the game ended.
    /// </summary>
    public void CheckGameEnd()
    {
        if (reachedGoal)
            gameController.Win();
        else if (HasDied)
            gameController.GameOverDied();
        else if (isOutOfMoves)
            gameController.GameOverOutOfMoves();
    }

    public void CollideFusePoint()
    {
        FuseEvent.RemoveListener(CollideFusePoint);
        StartPoint startPoint = UpcomingFusePoint.GetComponent<StartPoint>();
        startPoint.StartFollowingFuse();
    }

    public void CollidePickUp()
    {
        AmountOfDashMoves += PickUpValue;
        if (AmountOfDashMoves > maxAmountOfDashMoves)
            AmountOfDashMoves = maxAmountOfDashMoves;
        MovesText.text = AmountOfDashMoves.ToString();
    }

    public void CollideGoal(GameObject goal)
    {
        StartCoroutine(IsDashing
            ? MoveBackRoutine(goal.transform.position, DashDuration)
            : MoveBackRoutine(goal.transform.position, MoveDuration));
        reachedGoal = true;
        CheckGameEnd();
    }

    public void InfiniteLives()
    {
        maxAmountOfDashMoves = AmountOfDashMoves = 999;
        MovesText.text = AmountOfDashMoves.ToString();
    }

    public Vector3 DashDirection() { return TargetPosition - rigidBody.position; }
}
