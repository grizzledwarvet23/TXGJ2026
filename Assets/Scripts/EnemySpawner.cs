using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{

    //array of topleft / bottomright corners
    //so its like a set of spawn areas we defining
    //and also array of enemy prefabs to spawn:

    //well each will be a PAIR of TRANSFORMS in an array:

    public Transform[] topLeftCorners;
    public Transform[] bottomRightCorners;





    public GameObject[] enemyPrefabs;


    //randomly spawn them at a certain rate:

    public float spawnRate = 5f; //spawn every 5 seconds

    void Start()
    {
        //start the spawn coroutine:
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        while(true)
        {
            //pick a random spawn area:
            int areaIndex = Random.Range(0, topLeftCorners.Length);
            Transform topLeft = topLeftCorners[areaIndex];
            Transform bottomRight = bottomRightCorners[areaIndex];

            //pick a random position within that area:
            Vector2 spawnPos = new Vector2(Random.Range(topLeft.position.x, bottomRight.position.x), Random.Range(bottomRight.position.y, topLeft.position.y));

            //pick a random enemy prefab to spawn:
            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            //spawn the enemy:
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            yield return new WaitForSeconds(spawnRate);
        }
    }
}
