using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour
{

    public float speed;
    public float deflectPushOutBuffer = 0.5f; //extra clearance added on top of the rod's own half-width
    public float deflectSpeedMultiplier = 1.75f; //deflected bullets fly out faster, to sell the hit
    private Rigidbody2D rb;
    private bool resolved; //true once this bullet has committed to destroying/hitting something this frame
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    //lets have a cooldown after deflecting for a bit:
    bool canDeflect = true;
    float deflectCooldown = 0.2f;

    bool hasBeenDeflected = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        StartCoroutine(DestroyAfterTime(5f));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = transform.right * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //Destroy() doesn't take effect until end of frame, so a bullet overlapping two colliders
        //at once (e.g. two adjacent chain links) can still get a second OnTriggerEnter2D this same
        //frame after it already resolved - ignore anything past the first hit.
        if (resolved) return;

        ChainLink link = other.GetComponent<ChainLink>();
        if (link != null)
        {
            HandleChainHit(link);
            return;
        }

        if(other.CompareTag("Player"))
        {
            Player p = other.GetComponent<Player>();
            p.TakeDamage(1);
            resolved = true;
            Destroy(gameObject);
        }

        //if ground, destroy bullet
        if(other.CompareTag("Ground"))
        {
            resolved = true;
            Destroy(gameObject);
        }

        //interface-based rather than a hardcoded Enemy/tag check, so anything damageable (Enemy,
        //MitochondriaBoss, ...) can be hit this way without Bullet needing to know about it
        if (hasBeenDeflected)
        {
            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(1);
                resolved = true;
                Destroy(gameObject);
            }
        }
    }

    IEnumerator ResetDeflectCooldown()
    {
        yield return new WaitForSeconds(deflectCooldown);
        canDeflect = true;
    }

    void HandleChainHit(ChainLink link)
    {
        if(!canDeflect)
        {
            return;
        }
        canDeflect = false;
        StartCoroutine(ResetDeflectCooldown());

        //deflect vs destroy is now purely about which side of the rod got hit, not momentum -
        //whichever side currently matches Chain.deflectOnPositiveSide (toggled with E) deflects.
        //ChainLink owns this geometry now, so any other object can ask it the same question.
        Vector2 bulletDir = rb.linearVelocity.normalized;
        Vector2 rodNormal = link.rodNormal();
        float side = link.GetSide(transform.position);
        bool onDeflectSide = link.IsDeflectSide(transform.position);

        if (onDeflectSide)
        {
            //now that this deflects, it can damage enemy:
            hasBeenDeflected = true;

            //bat it away off the rod's orientation
            Vector2 newDir = Vector2.Reflect(bulletDir, rodNormal.normalized);

            //FixedUpdate rebuilds velocity from transform.right every step, so the rotation has to
            //change too, otherwise the very next physics step snaps velocity back to the old direction
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg);

            //FixedUpdate rebuilds velocity from `speed` every step, so boost speed itself here -
            //otherwise the multiplier would only last one physics step before being overwritten
            speed *= deflectSpeedMultiplier;
            rb.linearVelocity = newDir * speed;

            //OnTriggerEnter2D fires after the bullet has already advanced partway into the rod this
            //frame, so it's still overlapping the collider when we flip its direction - without this
            //it just spins in place still tangled in the rod instead of visibly bouncing away.
            //push it out along the side it's actually on, far enough to clear the rod's own width -
            //a flat small constant wasn't enough to guarantee real separation on a wider rod.
            float pushDistance = (link.width * 0.5f) + deflectPushOutBuffer;
            transform.position += (Vector3)(rodNormal.normalized * side * pushDistance);
        }
        else
        {
            //hit the destroy side of the rod -> this link and everything after it breaks off
            if (link.owner != null)
            {
                link.owner.CutFrom(link.index);
            }
            resolved = true;
            Destroy(gameObject);
        }
    }
}
