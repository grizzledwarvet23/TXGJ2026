using UnityEngine;

public class Pickup : MonoBehaviour
{

    //enum for the type of pickup this is - 
    public enum PickupType
    {
        Length,
        Joint,
    }

    //if we have a joint we will need to add a joint, so we will store a prefab of a chain link to add to the chain.
    public PickupType pickupType;

    public GameObject chainLinkPrefab;

    public float lengthIncreaseAmount = 1f;

    [Header("Bouncing around the room")]
    public float moveSpeed = 2f;
    public float bounceRadius = 0.3f; //should roughly match the pickup's visual/collider size

    private Vector2 velocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        velocity = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * moveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        //classic DVD-screensaver bounce: check each axis separately against the Ground layer and
        //flip whichever axis would move into a wall - no Rigidbody2D/physics material needed,
        //same OverlapCircle/Ground-layer approach already used elsewhere in the project.
        Vector2 pos = transform.position;
        LayerMask groundMask = LayerMask.GetMask("Ground");

        Vector2 nextX = pos + new Vector2(velocity.x * Time.deltaTime, 0f);
        if (Physics2D.OverlapCircle(nextX, bounceRadius, groundMask) != null)
        {
            velocity.x = -velocity.x;
        }

        Vector2 nextY = pos + new Vector2(0f, velocity.y * Time.deltaTime);
        if (Physics2D.OverlapCircle(nextY, bounceRadius, groundMask) != null)
        {
            velocity.y = -velocity.y;
        }

        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    //basically on trigger enter2d if its the player it will be:
    //if its lenght , it will increase the length of the last chain link in the chain
    //if its joint it will add a new joint to the chain, and make it the new last link in the chain
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        Chain chain = Object.FindFirstObjectByType<Chain>();
        if (chain == null || chain.chainLinks == null || chain.chainLinks.Length == 0) return;

        ChainLink lastLink = chain.chainLinks[chain.chainLinks.Length - 1];

        if (pickupType == PickupType.Length)
        {
            lastLink.SetLength(lastLink.length + lengthIncreaseAmount);
        }
        else if (pickupType == PickupType.Joint)
        {
            GameObject newLinkObj = Instantiate(chainLinkPrefab, chain.transform);
            ChainLink newLink = newLinkObj.GetComponent<ChainLink>();

            //start it out matching the current last link's angle so it doesn't visually snap on
            //the first frame - Chain.Update() will reposition/re-simulate it properly from there
            newLink.angle = lastLink.angle;
            newLink.angVel = 0f;

            chain.AddLink(newLink);
        }

        Destroy(gameObject);
    }
}
