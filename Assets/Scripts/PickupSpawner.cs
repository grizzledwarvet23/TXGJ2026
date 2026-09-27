using UnityEngine;
using System.Collections;

public class PickupSpawner : MonoBehaviour
{

    //basically lets define a top left and bottom right corner of the spawn area, and then we will spawn a pickup at a random position within that area.
    public Transform topLeftCorner;
    public Transform bottomRightCorner;

    public GameObject[] pickups;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(SpawnPickup());
    }

    public float clearCheckRadius = 0.3f; //should roughly match a pickup's own bounceRadius
    public int maxSpawnAttempts = 30;

    IEnumerator SpawnPickup()
    {
        while(true)
        {
            //spawn a pickup at a random position within the spawn area, re-rolling if it would
            //land inside Ground - otherwise a pickup spawned embedded gets permanently stuck, since
            //its own bounce logic can only react to hitting a wall, not escape starting inside one
            Vector2 spawnPos = FindClearSpawnPosition();
            Instantiate(pickups[Random.Range(0, pickups.Length)], spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(5f);
        }
    }

    Vector2 FindClearSpawnPosition()
    {
        LayerMask groundMask = LayerMask.GetMask("Ground");

        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(topLeftCorner.position.x, bottomRightCorner.position.x),
                Random.Range(bottomRightCorner.position.y, topLeftCorner.position.y));

            if (Physics2D.OverlapCircle(candidate, clearCheckRadius, groundMask) == null)
            {
                return candidate;
            }
        }

        //fell through every attempt (rare - spawn area is mostly/all blocked) - fall back to the
        //area's center rather than looping forever or spawning without checking at all
        return new Vector2(
            (topLeftCorner.position.x + bottomRightCorner.position.x) / 2f,
            (topLeftCorner.position.y + bottomRightCorner.position.y) / 2f);
    }
}
