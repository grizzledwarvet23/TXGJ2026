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

    [Header("Visual composite (leave empty to keep the single-sprite fallback)")]
    public Sprite startCapSprite;      //Chain_Split-2 - fixed size, drawn at the link's start
    public Sprite middleSprite;        //Chain_Split-1 - stretched to fill whatever length is left
    public Sprite endCapJointSprite;   //Chain_Split-4 - has the joint dot, used when another link follows this one
    public Sprite endCapPlainSprite;   //Chain_Split-3 - no dot, used only when this is the chain's actual last link

    private SpriteRenderer startCapRenderer;
    private SpriteRenderer middleRenderer;
    private SpriteRenderer endCapRenderer;

    private BoxCollider2D boxCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (startCapSprite != null && middleSprite != null && endCapJointSprite != null && endCapPlainSprite != null)
        {
            BuildCompositeVisual();
            if (spriteRenderer != null) spriteRenderer.enabled = false;
        }

        ApplyLength();
    }

    void BuildCompositeVisual()
    {
        startCapRenderer = CreateChildRenderer("StartCap", startCapSprite);
        middleRenderer = CreateChildRenderer("Middle", middleSprite);
        endCapRenderer = CreateChildRenderer("EndCap", endCapJointSprite);
    }

    //true if nothing comes after this link in the chain right now - can change at runtime
    //(a Joint pickup appends a link, or CutFrom shortens the chain), so this is re-checked
    //in ApplyLength() rather than decided once.
    bool IsLastLink()
    {
        return owner == null || owner.chainLinks == null || owner.chainLinks.Length == 0
            || index == owner.chainLinks.Length - 1;
    }

    SpriteRenderer CreateChildRenderer(string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        if (spriteRenderer != null)
        {
            sr.sortingLayerID = spriteRenderer.sortingLayerID;
            sr.sortingOrder = spriteRenderer.sortingOrder;
        }
        return sr;
    }

    //links below this length are treated as a "pending joint" (added by a Joint pickup, not yet
    //given any length by a Length pickup) and just show the joint-dot marker on its own.
    private const float PendingLengthThreshold = 0.01f;

    //lays out the collider and either the composite pieces or the single-sprite fallback for the
    //current length/width, and picks which end cap sprite applies right now. Call whenever length
    //changes (Start, SetLength) or the chain's link count changes (Chain.AddLink/CutFrom), since
    //either can change whether this link is still the last one.
    public void ApplyLength()
    {
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(Mathf.Max(length, 0.01f), width);
        }

        if (startCapRenderer == null)
        {
            //no composite sprites assigned - fall back to stretching the single sprite via transform scale
            transform.localScale = new Vector3(Mathf.Max(length, 0.01f), width, 1);
            return;
        }

        //composite is active, so the root transform shouldn't stretch - each child is sized on its own
        transform.localScale = Vector3.one;

        if (length <= PendingLengthThreshold)
        {
            //no length yet - stay fully invisible rather than drawing our own joint-dot marker.
            //the PREVIOUS link's end cap already switched to endCapJointSprite (it's no longer the
            //last link now that this one exists), which sits at this exact same point and already
            //marks it as a joint - drawing a second dot here would just double it up.
            startCapRenderer.gameObject.SetActive(false);
            middleRenderer.gameObject.SetActive(false);
            endCapRenderer.gameObject.SetActive(false);
            return;
        }

        startCapRenderer.gameObject.SetActive(true);
        middleRenderer.gameObject.SetActive(true);
        endCapRenderer.gameObject.SetActive(true);

        Sprite endSprite = IsLastLink() ? endCapPlainSprite : endCapJointSprite;
        endCapRenderer.sprite = endSprite;

        float startCapWidth = startCapSprite.bounds.size.x;
        float endCapWidth = endSprite.bounds.size.x;

        //if the two caps alone don't fit inside this link's own length, shrink them together
        //instead of letting them overlap/overhang past the link's actual bounds
        float capScale = 1f;
        float totalCapWidth = startCapWidth + endCapWidth;
        if (totalCapWidth > length)
        {
            capScale = length / totalCapWidth;
            startCapWidth *= capScale;
            endCapWidth *= capScale;
        }

        float middleWidth = Mathf.Max(0f, length - startCapWidth - endCapWidth);

        startCapRenderer.transform.localPosition = new Vector3(-length / 2f + startCapWidth / 2f, 0f, 0f);
        startCapRenderer.transform.localScale = new Vector3(capScale, width / startCapSprite.bounds.size.y, 1f);

        endCapRenderer.transform.localPosition = new Vector3(length / 2f - endCapWidth / 2f, 0f, 0f);
        endCapRenderer.transform.localScale = new Vector3(capScale, width / endSprite.bounds.size.y, 1f);

        middleRenderer.transform.localPosition = Vector3.zero;
        middleRenderer.transform.localScale = new Vector3(
            middleWidth / middleSprite.bounds.size.x,
            width / middleSprite.bounds.size.y,
            1f);
    }

    //flips the sprite to keep it in sync with which side currently deflects, so whichever color is
    //drawn on that side in the source art always tracks the deflect side as E changes it (rather
    //than the color always winning out regardless of which side is active - that's what the old
    //negated version did, since it and the gameplay check were driven by the same flag and canceled out).
    public void SetDeflectSideVisual(bool positiveSideDeflects)
    {
        bool flip = positiveSideDeflects;

        if (spriteRenderer != null) spriteRenderer.flipY = flip;
        if (startCapRenderer != null) startCapRenderer.flipY = flip;
        if (middleRenderer != null) middleRenderer.flipY = flip;
        if (endCapRenderer != null) endCapRenderer.flipY = flip;
    }

    public Vector2 end()
    {
        return start + length * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    //anything changing length after Start() (e.g. a pickup) needs to go through this so the
    //collider and visual (composite or fallback) actually resize too.
    public void SetLength(float newLength)
    {
        length = newLength;
        ApplyLength();
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
