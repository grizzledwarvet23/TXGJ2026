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
        bool isPlayer = other.CompareTag("Player");
        bool isChainLink = other.GetComponent<ChainLink>() != null;
        if (!isPlayer && !isChainLink) return;

        Chain chain = Object.FindFirstObjectByType<Chain>();
        if (chain == null) return;

        //the chain can be fully destroyed (every link cut away), leaving chainLinks empty rather
        //than null - either pickup type can rebuild it from nothing, not just Joint: a Length
        //pickup found with no chain at all just spawns the first link with real length right away.
        bool hasLinks = chain.chainLinks != null && chain.chainLinks.Length > 0;
        ChainLink lastLink = hasLinks ? chain.chainLinks[chain.chainLinks.Length - 1] : null;

        if (pickupType == PickupType.Length)
        {
            if (hasLinks)
            {
                lastLink.SetLength(lastLink.length + lengthIncreaseAmount);
            }
            else
            {
                SpawnLink(chain, 0f, 1.5f);
            }
        }
        else if (pickupType == PickupType.Joint)
        {
            //skip only if there's already a pending (zero-length) joint waiting on a Length pickup -
            //stacking another empty joint on top of it would be redundant. An empty chain has no
            //such thing, so this is exactly how a fully-destroyed chain gets rebuilt.
            bool alreadyPending = hasLinks && lastLink.length <= 0f;

            if (!alreadyPending)
            {
                //joints add no length by themselves - it stays a zero-length "pending joint" marker
                //until a Length pickup extends it into an actual segment
                float angle = hasLinks ? lastLink.angle : 0f;
                SpawnLink(chain, angle, 0f);
            }
        }

        Destroy(gameObject);
    }

    //instantiates a new chain link and appends it via chain.AddLink - length is set before AddLink
    //runs (rather than after) since AddLink triggers a visual refresh that reads the current length
    ChainLink SpawnLink(Chain chain, float angle, float length)
    {
        GameObject newLinkObj = Instantiate(chainLinkPrefab, chain.transform);
        ChainLink newLink = newLinkObj.GetComponent<ChainLink>();

        //start it out matching the current last link's angle so it doesn't visually snap on the
        //first frame (no previous link to match if the chain is empty - default to facing right,
        //Chain.Update() will rotate it toward the mouse aim from there either way)
        newLink.angle = angle;
        newLink.angVel = 0f;
        newLink.length = length;

        chain.AddLink(newLink);
        return newLink;
    }
}
