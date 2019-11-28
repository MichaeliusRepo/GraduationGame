﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace MoMa
{
    public class SalamanderController : MonoBehaviour
    {
        #region Vars
        // TODO: This should happen offline. Instead we only need to open its result
        //this._anim.Add(Packer.Pack("walk", "MoCapData", "walk_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("jog", "MoCapData", "jog3_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("acceleration", "MoCapData", "acceleration_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("run", "MoCapData", "Copy of run1_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("walk_continuous", "MoCapData", "walk_continuous2_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("circle_left", "MoCapData", "circle_left_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("circle_right", "MoCapData", "circle_right_DEFAULT_FIX"));
        //this._anim.Add(Packer.Pack("salamander", "MoCapData", "salamander_walk_test"));
        //this._anim.Add(Packer.Pack("run1right_DEFAULT_C26", "MoCapData", "run1right_DEFAULT_C26"));
        //this._anim.Add(Packer.Pack("run2right_DEFAULT_C26", "MoCapData", "run2right_DEFAULT_C26"));
        //this._anim.Add(Packer.Pack("runLeft_DEFAULT_C26", "MoCapData", "runLeft_DEFAULT_C26"));

        // Fine-tuning
        //public const float RecalculationThreshold = 0.3f; // The maximum diff of two Trajectories before recalculating the Animation
        public const float RecalculationThreshold = Mathf.Infinity; // The maximum diff of two Trajectories before recalculating the Animation
        public const int CooldownTime = 500; // Number of frames that a Frame is on cooldown after being played
        public const int CandidateFramesSize = 30; // Number of candidate frames for a transition (tradeoff: fidelity/speed)
        public const int ClipBlendPoints = 0; // Each Animation Clip is blended with the next one for smoother transition. The are both played for this num of Frames

        // Frame/Point/Feature ratios
        // FeaturePoints % FeatureEveryPoints should be 0
        public const int SkipFrames = 3;  // Take 1 Frame every SkipFrames in the Animation file
        public const int FeaturePoints = 3;  // Trajectory.Points per Feature. The lower the number, the shorter time the Feature covers
        public const int FeaturePastPoints = 4;  // The number of Points in the past that is used in a Snippet. The lower the number, the lower the fidelity
        public const int FeatureEveryPoints = 3;  // Trajectory.Points per Feature. The lower the nuber, the shorter time the Feature covers
        // FramesPerPoint % 2 should be 0
        public const int FramesPerPoint = 4;    // Animation.Frames per Trajectory.Point. The lower the number, the denser the Trajectory points will be.

        public const int FramesPerFeature = FramesPerPoint * FeaturePoints;  // Animation.Frames per Feature
        public const int FeatureStep = FeaturePoints / FeatureEveryPoints;  // Features overlap generally. This is the distance between two matching Features.
        public const int SnippetSize = FeaturePoints + FeaturePastPoints;

        // Movement
        public const float DefaultDampTime = 1f;
        public const float StopDampTime = 3f;
        public const float WalkingSpeed = 2.70f;
        public const float RunningSpeed = 1.4f;

        private MovementComponent _mc;
        private FollowerComponent _fc;
        private RuntimeComponent _rc;
        private AnimationComponent _ac;
        private Trajectory _trajectory = new Trajectory();
        private Transform _model;
        private int _currentFrame = 0;

        #endregion

        void Start()
        {
            // TODO: The Animations should not be packed on runtime
            List<(string, string)> animationFiles = new List<(string, string)>();
            animationFiles.Add(("take-1_DEFAULT_C26", "TempMoCapData"));
            //animationFiles.Add(("take-2_DEFAULT_C26", "cleanUps"));
            //animationFiles.Add(("take-3_DEFAULT_C26", "cleanUps"));
            //animationFiles.Add(("take-4_DEFAULT_C26", "cleanUps"));
            //animationFiles.Add(("take-5_DEFAULT_C26", "cleanUps"));
            //animationFiles.Add(("take-6_DEFAULT_C26", "cleanUps"));
            //animationFiles.Add(("take-7_DEFAULT_C26", "TempMoCapData"));
            //animationFiles.Add(("take-8_DEFAULT_C26", "TempMoCapData"));
            //animationFiles.Add(("take-9_DEFAULT_C26", "TempMoCapData"));

            // We assume that the Character has the correct structure
            this._model = this.gameObject.transform;
            this._mc = new MovementComponent(this._model);
            this._fc = new FollowerComponent(this._model);
            this._ac = new AnimationComponent(this._model);
            this._rc = new RuntimeComponent(this._fc, animationFiles);

            if (this._model == null)
            {
                throw new System.Exception("SalamanderController was unable to find the model.");
            }

            // Initialize Trajectory's past to the initial position
            for (int i = 0; i < FeaturePastPoints; i++)
            {
                this._trajectory.points.Add(new Trajectory.Point(new Vector2(0f, 0f), Quaternion.identity));
            }
        }

        void FixedUpdate()
        {
            StartCoroutine(UpdateCoroutine());
        }

        public void UpdateTarget(MovementController.EventType type, Vector2 position)
        {
            _mc.UpdateTargets(type, position);
        }

        private IEnumerator UpdateCoroutine()
        {
            // Update MovementComponent
            _mc.Update();

            // Add Point to Trajectory, removing the oldest point
            if (_currentFrame % FramesPerPoint == 0)
            {
                this._trajectory.points.Add(
                    new Trajectory.Point(
                        new Vector2(this._model.position.x, this._model.position.z),
                        this._model.rotation
                        )
                    );
                this._trajectory.points.RemoveAt(0);

                // Reset current Frame
                _currentFrame = 0;
            }

            _currentFrame++;

            // Load new Animation.Clip
            if (_ac.IsOver())
            {
                // Find and load next Animation.Clip
                Trajectory.Snippet snippet = GetCurrentSnippet();
                _ac.LoadClip(this._rc.QueryClip(snippet));
            }

            // Play Animation.Frame
            _ac.Step();

            yield return null;
        }

        private Trajectory.Snippet GetCurrentSnippet()
        {
            Trajectory.Snippet snippet;
            int futureFramesNumber = FramesPerPoint * FeaturePoints;

            // Get simulated future
            List<(Vector3, Quaternion)> futureTransforms = this._mc.GetFuture(futureFramesNumber);

            // Convert the (many) Frames to (few) Point and add them to the Trajectory
            for (int i = 0; i < FeaturePoints; i++)
            {
                //Trajectory.Point point = Trajectory.Point.getMedianPoint(futureFrames.GetRange(i * Trajectory.FramesPerPoint, Trajectory.FramesPerPoint));
                //Trajectory.Point point = new Trajectory.Point(futureFrames[i * Feature.FramesPerPoint + Feature.FramesPerPoint / 2].GetXZVector2());
                Trajectory.Point point = new Trajectory.Point(
                    futureTransforms[(i + 1) * FramesPerPoint - 1].Item1.GetXZVector2(),
                    futureTransforms[(i + 1) * FramesPerPoint - 1].Item2
                    );
                this._trajectory.points.Add(point);
            }

            // Compute the Trajectory Snippet
            snippet = this._trajectory.GetLocalSnippet(FeaturePastPoints - 1);

            // Remove future Points from Trajectory
            this._trajectory.points.RemoveRange(FeaturePastPoints, FeaturePoints);

            return snippet;
        }
    }
}