using UnityEngine;

public class ChainLink : MonoBehaviour
{

    public float length;
    public float width;
    public float angle; //radians
    public float angVel; //how fast rotating
    public float stiffness; //how harad it snaps to parent

    public Vector2 start;
    public Vector2 velocity; //world-space velocity of this link's midpoint, used to check against bullet velocity
    public Chain owner;
    public int index; //this link's position in owner.chainLinks, set by Chain.AssignLinkMetadata()
    // BoxCollider2D col;

    private Vector2 lastMidpoint;
    private bool initialized;

    private SpriteRenderer spriteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //lets size this game objcet to match the length and width of the link
        transform.localScale = new Vector3(length, width, 1);

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = DeflectSideVisual.GetSprite();
        }
    }

    //flips the deflect-side indicator sprite so whichever half currently deflects always shows green,
    //flipping across the rod's own length axis - the same axis the deflect/destroy side check uses.
    public void SetDeflectSideVisual(bool positiveSideDeflects)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipY = !positiveSideDeflects;
        }
    }

    public Vector2 end()
    {
        return start + length * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    //Start() only sizes the visual/collider once from length/width - anything changing length
    //afterward (e.g. a pickup) needs to go through this so the sprite/collider actually resizes too.
    public void SetLength(float newLength)
    {
        length = newLength;
        transform.localScale = new Vector3(length, width, 1);
    }

    public Vector2 rodDir()
    {
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    //perpendicular to the rod - defines the rod's two sides as +rodNormal() and -rodNormal()
    public Vector2 rodNormal()
    {
        Vector2 dir = rodDir();
        return new Vector2(-dir.y, dir.x);
    }

    public Vector2 rodMid()
    {
        return (start + end()) / 2f;
    }

    //which side of the rod's current normal a world point sits on: +1 or -1, never 0
    public float GetSide(Vector2 worldPoint)
    {
        float side = Mathf.Sign(Vector2.Dot(worldPoint - rodMid(), rodNormal()));
        return side == 0f ? 1f : side;
    }

    //true if worldPoint is on whichever side currently deflects, per owner.deflectOnPositiveSide
    public bool IsDeflectSide(Vector2 worldPoint)
    {
        return owner != null && (GetSide(worldPoint) > 0f) == owner.deflectOnPositiveSide;
    }

    public void updateCollider()
    {
        Vector2 midpoint = (start + end()) / 2;

        if (!initialized)
        {
            //avoid a velocity spike on the very first frame before lastMidpoint has a real value
            lastMidpoint = midpoint;
            initialized = true;
        }

        velocity = (midpoint - lastMidpoint) / Time.deltaTime;
        lastMidpoint = midpoint;

        transform.position = midpoint;
        transform.eulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg);
    }
}
