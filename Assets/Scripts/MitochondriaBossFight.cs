using UnityEngine;

//orchestrates the two-switch boss encounter:
//1. only switchA starts active - hitting it (on the non-deflect side, same as any other Switch)
//   opens every door in `doors` (the shield covering the boss) and makes the boss vulnerable.
//2. reflecting an enemy bullet into the now-visible boss deals damage and immediately closes the
//   shield again; switchA deactivates and switchB activates.
//3. the player has to go trigger switchB next, which repeats the cycle (back to switchA, etc.)
//   until the boss's health reaches 0.
public class MitochondriaBossFight : MonoBehaviour
{
    public Switch switchA;
    public Switch switchB;
    public Door[] doors;
    public MitochondriaBoss boss;

    private Switch activeSwitch;
    private Switch waitingSwitch;

    void Start()
    {
        activeSwitch = switchA;
        waitingSwitch = switchB;

        switchA.SetActive(true);
        switchB.SetActive(false);

        activeSwitch.OnActivated += HandleSwitchActivated;
        boss.OnHit += HandleBossHit;
        boss.OnDied += HandleBossDied;
    }

    void HandleSwitchActivated()
    {
        OpenDoors();
        boss.isVulnerable = true;
    }

    void HandleBossHit()
    {
        boss.isVulnerable = false;
        CloseDoors();
        SwapActiveSwitch();
    }

    void HandleBossDied()
    {
        boss.isVulnerable = false;
        OpenDoors(); //leave the shield down for good

        switchA.SetActive(false);
        switchB.SetActive(false);
    }

    void OpenDoors()
    {
        foreach (Door d in doors)
        {
            if (d != null) d.Explode();
        }
    }

    void CloseDoors()
    {
        foreach (Door d in doors)
        {
            if (d != null) d.Reset();
        }
    }

    void SwapActiveSwitch()
    {
        activeSwitch.OnActivated -= HandleSwitchActivated;

        Switch temp = activeSwitch;
        activeSwitch = waitingSwitch;
        waitingSwitch = temp;

        waitingSwitch.SetActive(false);
        activeSwitch.SetActive(true);
        activeSwitch.OnActivated += HandleSwitchActivated;
    }
}
