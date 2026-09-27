using UnityEngine;

//attach to the boss's root object, alongside a trigger Collider2D sized to cover its mesh. It only
//takes damage while isVulnerable is true - MitochondriaBossFight flips that to match whether the
//shield doors around it are currently open or closed.
public class MitochondriaBoss : MonoBehaviour, IDamageable
{
    public int health = 3;
    public bool isVulnerable = false;

    public System.Action OnHit;  //fired on every hit that doesn't kill it
    public System.Action OnDied; //fired once, when health reaches 0

    public void TakeDamage(int amount)
    {
        if (!isVulnerable) return;

        health -= amount;

        if (health <= 0)
        {
            OnDied?.Invoke();
        }
        else
        {
            OnHit?.Invoke();
        }
    }
}
