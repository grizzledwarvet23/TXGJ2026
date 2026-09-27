using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//generic "needs multiple switches" puzzle piece: drag in every Switch that should count, and once
//every single one has been triggered (from its correct chain side, same as any normal Switch),
//this opens every door in `doors` and fires `onAllActivated` for anything else that isn't a door
//(disabling a hazard, playing an animation, etc.).
//
//each Switch's own `door` field should normally be left empty here - otherwise that switch would
//open its own door by itself the moment it's hit, before the rest of the group is done.
public class SwitchGroup : MonoBehaviour
{
    public Switch[] switches;
    public Door[] doors;
    public UnityEvent onAllActivated;

    private HashSet<Switch> activatedSwitches = new HashSet<Switch>();
    private bool triggered = false;

    void Start()
    {
        foreach (Switch s in switches)
        {
            if (s != null) s.OnActivated += () => HandleSwitchActivated(s);
        }
    }

    void HandleSwitchActivated(Switch s)
    {
        if (triggered) return;

        activatedSwitches.Add(s);

        if (activatedSwitches.Count >= switches.Length)
        {
            triggered = true;

            foreach (Door d in doors)
            {
                if (d != null) d.Explode();
            }

            onAllActivated?.Invoke();
        }
    }
}
