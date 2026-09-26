using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    public GameObject bullet;
    public Transform firePoint;
    private Transform player;

    private bool canFire = true;

    public float fireRate;

    int health = 1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;   
    }

    //draw the raycast  with gizmos:

    

    // Update is called once per frame
    void Update()
    {
        //if we have line of sight to the player, fire a bullet.
        //use raycast, basically we wanna check we hit player and not ground:
        //and use a layermask to only hit the player and ground layer:
        //make it unlimited: 
        //and make raycast face player, instead of vector2.right, it should be the direction to the player:
        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, (Vector2)(player.position - firePoint.position).normalized, Mathf.Infinity, LayerMask.GetMask("Player", "Ground"));
    
        Debug.Log(hit.collider);  
        if (canFire && hit.collider != null && hit.collider.CompareTag("Player"))
        {
            StartCoroutine(FireBullet(hit.collider.transform.position));
        }

    }

    //have a parameter so we can aim at the player:
    IEnumerator FireBullet(Vector2 target)
    {

        //fire bullet:
        //rotate firepoint to face target:
        Vector2 dir = target - (Vector2)firePoint.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(0, 0, angle);
        GameObject b = Instantiate(bullet, firePoint.position, firePoint.rotation);    

        canFire = false;
        yield return new WaitForSeconds(fireRate);
        canFire = true;

    }

    public void TakeDamage(int damage)
    {
        health -= damage;
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
