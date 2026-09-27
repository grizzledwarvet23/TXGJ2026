using UnityEngine;

public class Enemy : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    private Transform player;

    public int health = 1;

    DamageFlash damageFlash;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        damageFlash = GetComponent<DamageFlash>();

        if (BeatConductor.Instance != null)
        {
            BeatConductor.Instance.OnAttackBeat += TryFireOnBeat;
        }
    }

    void OnDestroy()
    {
        if (BeatConductor.Instance != null)
        {
            BeatConductor.Instance.OnAttackBeat -= TryFireOnBeat;
        }
    }

    void TryFireOnBeat()
    {
        //only fire if we actually have line of sight to the player right when the beat lands -
        //use a raycast, and a layermask so the ground can block it same as before
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, (Vector2)(player.position - firePoint.position).normalized, Mathf.Infinity, LayerMask.GetMask("Player", "Ground"));

        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            FireBullet(hit.collider.transform.position);
        }
    }

    void FireBullet(Vector2 target)
    {
        //rotate firepoint to face target:
        Vector2 dir = target - (Vector2)firePoint.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(0, 0, angle);
        Instantiate(bullet, firePoint.position, firePoint.rotation);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (damageFlash != null) damageFlash.TriggerFlash();

        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
