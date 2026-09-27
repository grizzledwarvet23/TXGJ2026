using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PickupSpawner : MonoBehaviour
{

    //basically lets define a top left and bottom right corner of the spawn area, and then we will spawn a pickup at a random position within that area.
    public Transform topLeftCorner;
    public Transform bottomRightCorner;

    public GameObject[] pickups;

    //chance of spawning a Length pickup vs a Joint pickup - read off each prefab's own Pickup.pickupType,
    //so it doesn't matter how `pickups` is ordered or how many variants of each type you add
    [Range(0f, 1f)]
    public float lengthPickupChance = 0.65f;

    private GameObject[] lengthPickups;
    private GameObject[] jointPickups;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CategorizePickups();
        StartCoroutine(SpawnPickup());
    }

    public float clearCheckRadius = 0.3f; //should roughly match a pickup's own bounceRadius
    public int maxSpawnAttempts = 30;

    void CategorizePickups()
    {
        List<GameObject> lengthList = new List<GameObject>();
        List<GameObject> jointList = new List<GameObject>();

        foreach (GameObject prefab in pickups)
        {
            Pickup p = prefab != null ? prefab.GetComponent<Pickup>() : null;
            if (p == null) continue;

            if (p.pickupType == Pickup.PickupType.Length) lengthList.Add(prefab);
            else if (p.pickupType == Pickup.PickupType.Joint) jointList.Add(prefab);
        }

        lengthPickups = lengthList.ToArray();
        jointPickups = jointList.ToArray();
    }

    GameObject PickWeightedPrefab()
    {
        bool wantsLength = Random.value < lengthPickupChance;
        GameObject[] pool = wantsLength ? lengthPickups : jointPickups;

        //fall back to whichever pool actually has entries, in case only one type got assigned in `pickups`
        if (pool == null || pool.Length == 0) pool = wantsLength ? jointPickups : lengthPickups;
        if (pool == null || pool.Length == 0) return null;

        return pool[Random.Range(0, pool.Length)];
    }

    IEnumerator SpawnPickup()
    {
        while(true)
        {
            //spawn a pickup at a random position within the spawn area, re-rolling if it would
            //land inside Ground - otherwise a pickup spawned embedded gets permanently stuck, since
            //its own bounce logic can only react to hitting a wall, not escape starting inside one
            Vector2 spawnPos = FindClearSpawnPosition();
            GameObject prefab = PickWeightedPrefab();
            if (prefab != null)
            {
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
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
