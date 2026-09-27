using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour, IDamageable
{
    public GameObject bullet;
    public Transform firePoint;
    private Transform player;

    public int health = 1;

    public GameObject deathExplosionPrefab; //different per enemy type - assigned per prefab

    //which of BeatConductor's two beat patterns this enemy type fires on - different enemy types
    //fire on different instrument lines of the same song, sharing the same tempo-change point
    public enum AttackPattern { Primary, Secondary }
    public AttackPattern attackPattern = AttackPattern.Primary;

    //0 = dies permanently, like a normal room enemy. >0 = comes back to life in place after this
    //many seconds instead of being destroyed - used for the boss room so the player always has
    //bullets to reflect at the boss.
    public float respawnDelay = 0f;

    private int maxHealth;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    DamageFlash damageFlash;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        damageFlash = GetComponent<DamageFlash>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        maxHealth = health;

        if (BeatConductor.Instance != null)
        {
            if (attackPattern == AttackPattern.Secondary)
                BeatConductor.Instance.OnAttackBeatSecondary += TryFireOnBeat;
            else
                BeatConductor.Instance.OnAttackBeat += TryFireOnBeat;
        }
    }

    void OnDestroy()
    {
        if (BeatConductor.Instance != null)
        {
            if (attackPattern == AttackPattern.Secondary)
                BeatConductor.Instance.OnAttackBeatSecondary -= TryFireOnBeat;
            else
                BeatConductor.Instance.OnAttackBeat -= TryFireOnBeat;
        }
    }

    void TryFireOnBeat()
    {
        if (isDead) return;

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
        if (isDead) return;

        health -= damage;

        if (damageFlash != null) damageFlash.TriggerFlash();

        if(health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (deathExplosionPrefab != null)
        {
            Instantiate(deathExplosionPrefab, transform.position, Quaternion.identity);
        }

        if (respawnDelay > 0f)
        {
            StartCoroutine(RespawnAfterDelay());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    IEnumerator RespawnAfterDelay()
    {
        isDead = true;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (col != null) col.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        health = maxHealth;
        isDead = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (col != null) col.enabled = true;
    }
}
