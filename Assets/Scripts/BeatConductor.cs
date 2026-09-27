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

    //primary pattern - used by the original enemy type
    public event System.Action OnAttackBeat;

    //secondary pattern - a different instrument line in the same song (SecondEnemy fires on this
    //instead), sharing the same tempo-change point but its own offsets/interval either side of it
    public event System.Action OnAttackBeatSecondary;

    //--- schedule from the level track's timing sheet ---
    const float firstAttackTime = 0.75f;
    const float initialInterval = 1.5f;
    const float tempoChangeTime = 24f;
    const float newTempoFirstAttackTime = 24.705f;
    const float newInterval = 1.412f;

    //--- secondary pattern (SecondEnemy) - same song/tempo change, different beat offsets ---
    const float firstAttackTimeSecondary = 1.349f;
    const float intervalSecondary = 3.045f; //4.394 - 1.349
    const float newTempoFirstAttackTimeSecondary = 25.312f; //tempoChangeTime + 1.312
    const float newIntervalSecondary = 2.823f; //4.135 - 1.312

    float trackStartTime;
    float clipStartOffset; //elapsed-time value corresponding to musicSource.time == 0 for the CURRENT clip
    bool hasSwitchedClip;

    float nextBeatTime = firstAttackTime;
    bool usingNewTempo;

    float nextBeatTimeSecondary = firstAttackTimeSecondary;
    bool usingNewTempoSecondary;

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
            nextBeatTime = AdvanceSchedule(nextBeatTime, ref usingNewTempo, initialInterval, newTempoFirstAttackTime, newInterval);
            elapsed = GetElapsed();
        }

        while (elapsed >= nextBeatTimeSecondary)
        {
            OnAttackBeatSecondary?.Invoke();
            nextBeatTimeSecondary = AdvanceSchedule(nextBeatTimeSecondary, ref usingNewTempoSecondary, intervalSecondary, newTempoFirstAttackTimeSecondary, newIntervalSecondary);
            elapsed = GetElapsed();
        }
    }

    float GetElapsed()
    {
        //fall back to wall-clock time whenever the music isn't actually playing (missing/broken
        //clip reference, audio failed to start, etc.) - otherwise musicSource.time sits at 0
        //forever and the whole beat schedule silently never fires a single attack
        if (musicSource != null && musicSource.isPlaying)
        {
            return clipStartOffset + musicSource.time;
        }
        return Time.time - trackStartTime;
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

    //shared by both patterns - each keeps its own `current`/`usingNewTempo` state, but they all
    //switch over at the same shared tempoChangeTime since it's the same song underneath
    float AdvanceSchedule(float current, ref bool usingNewTempoFlag, float interval, float newTempoFirstAttack, float newIntervalValue)
    {
        if (!usingNewTempoFlag)
        {
            float candidate = current + interval;
            if (candidate >= tempoChangeTime)
            {
                //the next old-tempo beat would land past the tempo change - switch to the new
                //tempo's fixed first beat instead of continuing the old interval past that point
                usingNewTempoFlag = true;
                return newTempoFirstAttack;
            }
            return candidate;
        }
        else
        {
            return current + newIntervalValue;
        }
    }
}
