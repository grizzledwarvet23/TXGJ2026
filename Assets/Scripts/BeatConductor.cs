using UnityEngine;

//shared schedule so every enemy fires on the same beat instead of its own independent timer.
//add this to one empty GameObject in the scene (e.g. "GameManager") - only one should exist.
public class BeatConductor : MonoBehaviour
{
    public static BeatConductor Instance { get; private set; }

    //if the level track gets wired in here, elapsed time is read from its own playback position
    //instead of wall-clock time, so the schedule can't drift from the music even on a frame hitch.
    //leave empty and it just uses time since this object started.
    public AudioSource musicSource;

    [Header("Music - the track is two clips: intro plays first, loop takes over at the tempo change")]
    public AudioClip introClip;
    public AudioClip loopClip;

    public event System.Action OnAttackBeat;

    //--- schedule from the level track's timing sheet ---
    const float firstAttackTime = 0.75f;
    const float initialInterval = 1.5f;
    const float tempoChangeTime = 24f;
    const float newTempoFirstAttackTime = 24.705f;
    const float newInterval = 1.412f;

    float trackStartTime;
    float clipStartOffset; //elapsed-time value corresponding to musicSource.time == 0 for the CURRENT clip
    float nextBeatTime = firstAttackTime;
    bool usingNewTempo;
    bool hasSwitchedClip;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        trackStartTime = Time.time;

        if (musicSource != null && introClip != null)
        {
            musicSource.clip = introClip;
            musicSource.loop = false;
            musicSource.Play();
        }
    }

    void Update()
    {
        float elapsed = GetElapsed();

        //the intro clip hands off to the looping clip right at the tempo change point - independent
        //of the beat schedule below, since musicSource.time resets to 0 on the new clip, so elapsed
        //has to be re-anchored via clipStartOffset the moment the switch happens
        if (!hasSwitchedClip && elapsed >= tempoChangeTime)
        {
            SwitchToLoopClip();
            elapsed = GetElapsed();
        }

        //a while loop rather than if, so a frame hitch that skips past more than one beat still
        //fires each of them instead of silently dropping attacks
        while (elapsed >= nextBeatTime)
        {
            OnAttackBeat?.Invoke();
            AdvanceSchedule();
            elapsed = GetElapsed();
        }
    }

    float GetElapsed()
    {
        return musicSource != null ? clipStartOffset + musicSource.time : Time.time - trackStartTime;
    }

    void SwitchToLoopClip()
    {
        hasSwitchedClip = true;

        if (musicSource != null && loopClip != null)
        {
            clipStartOffset = tempoChangeTime;
            musicSource.clip = loopClip;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    void AdvanceSchedule()
    {
        if (!usingNewTempo)
        {
            float candidate = nextBeatTime + initialInterval;
            if (candidate >= tempoChangeTime)
            {
                //the next old-tempo beat would land past the tempo change - switch to the new
                //tempo's fixed first beat instead of continuing the old interval past that point
                usingNewTempo = true;
                nextBeatTime = newTempoFirstAttackTime;
            }
            else
            {
                nextBeatTime = candidate;
            }
        }
        else
        {
            nextBeatTime += newInterval;
        }
    }
}
