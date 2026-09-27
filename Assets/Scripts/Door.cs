using UnityEngine;

//attach to the Door prefab. Switch.cs looks for this component and calls Explode() instead of
//just deactivating the door directly, so opening it plays a "blew up" animation first.
public class Door : MonoBehaviour
{
    public GameObject explosionPrefab;

    public void Explode()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }

    //brings the door back (no VFX) - used by encounters that re-close a shield after a hit
    public void Reset()
    {
        gameObject.SetActive(true);
    }
}
