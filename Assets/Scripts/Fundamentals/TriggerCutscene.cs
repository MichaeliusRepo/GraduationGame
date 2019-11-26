﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TriggerCutscene : MonoBehaviour
{
    private PlayableDirector _timeline;
    public UnityEvent OnTrigger;

    public enum TimelineTrack
    {
        LucyBodyAnimator,
        LucyFireAnimator,
        VirtualCamera,
    }

    public TimelineTrack[] TrackReferences;

    public bool OnlyOnce = true;

    private void Awake()
    {
        _timeline = GetComponent<PlayableDirector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            SetBindings();
            OnTrigger.Invoke();
            _timeline.Play();

            if (OnlyOnce)
                Destroy(this);
        }


    }

    private void SetBindings()
    {
        var timelineAsset = _timeline.playableAsset;
        var playableBindings = new List<PlayableBinding>(timelineAsset.outputs);

        for (int i = 0; i < TrackReferences.Length; ++i)
        {
            var track = playableBindings[i].sourceObject as TrackAsset;
            var gameObjectRef = GameObject.FindGameObjectWithTag(TrackReferences[i].ToString());
            _timeline.SetGenericBinding(track, gameObjectRef);
        }
    }

}
