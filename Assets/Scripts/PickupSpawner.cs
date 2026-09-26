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

    IEnumerator SpawnPickup()
    {
        while(true)
        {
            //spawn a pickup at a random position within the spawn area:
            Vector2 spawnPos = new Vector2(Random.Range(topLeftCorner.position.x, bottomRightCorner.position.x), Random.Range(bottomRightCorner.position.y, topLeftCorner.position.y));
            Instantiate(pickups[Random.Range(0, pickups.Length)], spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(5f);
        }
    }
}
