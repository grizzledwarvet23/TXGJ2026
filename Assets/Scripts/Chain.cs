using UnityEngine;
using System.Collections;

public class Chain : MonoBehaviour
{
    public ChainLink[] chainLinks;

    public Transform anchorPoint;
    private Vector2 anchor;

    public float damping;

    public float gravity;

    public float baseTurnSpeed=8f;
    private float turnSpeed;

    //totalLength at which turnSpeed == baseTurnSpeed - the reference point everything scales from
    public float referenceLength = 3f;
    public float lengthSpeedExponent = 1f;

    public float targetAngle = 0;

    //how many in-between angles to check when a link swings, so a fast swing can't skip
    //past a thin wall/corner between one frame's angle and the next
    public int collisionSubsteps = 6;

    //how big each search step is when resolving a link that got dragged into geometry by pure
    //translation (see ResolveEmbedding). Smaller = smoother/less visible correction, but takes
    //more overlap checks to find clearance on a deep overlap.
    public float embedResolveStepDegrees = 1f;

    //shrinks the box used for every overlap check by this much on each axis, so a hair's-width of
    //contact right at a surface doesn't flicker between "blocked"/"clear" frame to frame and cause jitter.
    public float collisionSkin = 0.02f;

    //which side of the rod (relative to its own current rodNormal, so it rotates with the swing)
    //deflects a bullet vs. cuts the chain. Bullet.cs reads this. Toggle with E.
    public bool deflectOnPositiveSide = true;

    [Header("Chain break VFX")]
    public GameObject chainExplosionPrefab;
    public float cascadeDelay = 0.15f; //time between each successive link exploding, chain-reaction style

    [Header("Guaranteed starting chain")]
    //if a scene's chainLinks array is empty at spawn (deleted, never set up, etc.), we auto-spawn
    //one starting link from this prefab instead of leaving the player with no chain at all
    public GameObject defaultChainLinkPrefab;
    public float defaultChainLinkLength = 1.5f;

    //captured at Awake so ResetToDefault() knows what "basics" means - the chain as it existed
    //before any Length/Joint pickups grew it, so a room transition can strip that growth back off
    private int defaultLinkCount;
    private float[] defaultLinkLengths;

    void Awake()
    {
        SanitizeChainLinks();
        EnsureStartingChain();
        AssignLinkMetadata();
        CaptureDefaultState();
    }

    void EnsureStartingChain()
    {
        if (chainLinks.Length > 0 || defaultChainLinkPrefab == null) return;

        GameObject linkObj = Instantiate(defaultChainLinkPrefab, transform);
        ChainLink link = linkObj.GetComponent<ChainLink>();
        link.angle = 0f;
        link.angVel = 0f;
        link.length = defaultChainLinkLength;

        chainLinks = new ChainLink[] { link };
    }

    //deleting a chain link's GameObject from the scene doesn't shrink the chainLinks array in the
    //Inspector - it just leaves a null ("None") slot behind, which would otherwise crash the very
    //first read of chainLinks[i].something. Strip those out so an empty/partially-empty array just
    //behaves as "no chain yet" (which Pickup.cs already knows how to rebuild from).
    void SanitizeChainLinks()
    {
        if (chainLinks == null)
        {
            chainLinks = new ChainLink[0];
            return;
        }

        int validCount = 0;
        foreach (ChainLink l in chainLinks)
        {
            if (l != null) validCount++;
        }

        if (validCount == chainLinks.Length) return;

        ChainLink[] cleaned = new ChainLink[validCount];
        int idx = 0;
        foreach (ChainLink l in chainLinks)
        {
            if (l != null) cleaned[idx++] = l;
        }
        chainLinks = cleaned;
    }

    void CaptureDefaultState()
    {
        defaultLinkCount = chainLinks.Length;
        defaultLinkLengths = new float[defaultLinkCount];
        for (int i = 0; i < defaultLinkCount; i++)
        {
            defaultLinkLengths[i] = chainLinks[i].length;
        }
    }

    //strips off any links/length gained from pickups, back down to how the chain looked at the
    //start of the game - call this on a room transition so growth doesn't carry between rooms.
    public void ResetToDefault()
    {
        if (chainLinks.Length > defaultLinkCount)
        {
            CutFrom(defaultLinkCount);
        }

        for (int i = 0; i < chainLinks.Length && i < defaultLinkLengths.Length; i++)
        {
            chainLinks[i].SetLength(defaultLinkLengths[i]);
        }
    }

    void AssignLinkMetadata()
    {
        for (int i = 0; i < chainLinks.Length; i++)
        {
            chainLinks[i].owner = this;
            chainLinks[i].index = i;
        }
    }

    void Update()
    {
        if (chainLinks == null || chainLinks.Length == 0) return;

        if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
        {
            deflectOnPositiveSide = !deflectOnPositiveSide;
        }

        foreach (ChainLink l in chainLinks)
        {
            l.SetDeflectSideVisual(deflectOnPositiveSide);
        }

        //turn speed depends only on the chain's total length, not how many links it's split into -
        //6 links of length 1 and 2 links of length 3 both total 6, and should turn at the same speed
        float totalLength = 0f;
        foreach (ChainLink l in chainLinks)
        {
            totalLength += l.length;
        }

        turnSpeed = baseTurnSpeed * Mathf.Pow(referenceLength / totalLength, lengthSpeedExponent);
        //but lets limit how slow it can get, so a long chain is still controllable
        //turnSpeed = Mathf.Max(turnSpeed, 0.5f);

        anchor = anchorPoint.position;
        ChainLink baseLink = chainLinks[0];

        float oldAngle = baseLink.angle;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        Vector2 dir = (Vector2)mouseWorld - anchor;
        float baseLinkTargetAngle = Mathf.Atan2(dir.y, dir.x);

        float candidateBaseAngle = rotateToward(oldAngle, baseLinkTargetAngle, turnSpeed * Time.deltaTime);

        Vector2 currentStart = anchor;
        baseLink.start = currentStart;
        bool baseBlocked = AdvanceAngle(baseLink, oldAngle, candidateBaseAngle);
        baseLink.angVel = baseBlocked ? 0f : (baseLink.angle - oldAngle) / Time.deltaTime;
        ResolveEmbedding(baseLink, baseLinkTargetAngle);
        currentStart = baseLink.end();

        for(int i = 1; i < chainLinks.Length; i++)
        {
            ChainLink link = chainLinks[i];
            ChainLink parent = chainLinks[i - 1];

            float prevAngle = link.angle;
            float diff = wrapAngle(parent.angle - link.angle);
            float accel = diff * link.stiffness - (link.angVel - parent.angVel) * damping + gravity * Mathf.Cos(link.angle);
            link.angVel += accel * Time.deltaTime;
            float candidateAngle = prevAngle + link.angVel * Time.deltaTime;

            link.start = currentStart;
            bool blocked = AdvanceAngle(link, prevAngle, candidateAngle);
            if (blocked) link.angVel = 0f;
            ResolveEmbedding(link, parent.angle);

            currentStart = link.end();
        }

        foreach (ChainLink l in chainLinks)
        {
            l.updateCollider();
        }
    }

    //tries to rotate the link to candidateAngle; if that sweep would cross into the Ground layer,
    //holds it at the last safe angle instead so the chain can't slide/sweep through walls or floors.
    //checks the rod's actual length x width footprint, not just its centerline - a thin linecast can
    //miss a wall the rod's edge is clipping through when swinging near-parallel to it.
    //steps through the rotation in small increments instead of one big jump, so a fast swing can't
    //skip past a thin corner between the old angle and the new one (classic tunneling).
    bool AdvanceAngle(ChainLink link, float safeAngle, float candidateAngle)
    {
        float from = safeAngle;
        float delta = wrapAngle(candidateAngle - safeAngle);
        float lastGoodAngle = safeAngle;

        for (int step = 1; step <= collisionSubsteps; step++)
        {
            float t = (float)step / collisionSubsteps;
            float testAngle = from + delta * t;

            if (IsBlocked(link, testAngle))
            {
                link.angle = lastGoodAngle;
                return true;
            }

            lastGoodAngle = testAngle;
        }

        link.angle = lastGoodAngle;
        return false;
    }

    //AdvanceAngle only catches penetration caused by ROTATING into a wall. Just walking forward
    //drags a link's start point (inherited from the anchor/player) without changing its angle at
    //all, so a link can end up embedded purely from translation. If that happens, search outward
    //in both directions from the current angle for the nearest clear orientation and snap to it -
    //this is what makes the chain "tilt up and over" an obstacle as you keep walking into it,
    //since flatter angles keep getting blocked and the search is forced steeper each frame.
    //referenceAngle (the parent link's angle, or the aim angle for the base link) breaks ties: a
    //blocked link prefers whichever direction folds it closer to the rest of the chain, instead of
    //always trying "up" first - that arbitrary up-bias was what caused links further down the chain
    //to cascade into ever-bigger upward corrections when aiming into the ground.
    void ResolveEmbedding(ChainLink link, float referenceAngle)
    {
        if (!IsBlocked(link, link.angle)) return;

        float stepRad = embedResolveStepDegrees * Mathf.Deg2Rad;
        int maxSteps = Mathf.Max(1, Mathf.CeilToInt(180f / embedResolveStepDegrees));

        bool preferIncreasing = wrapAngle(referenceAngle - link.angle) >= 0f;

        for (int i = 1; i <= maxSteps; i++)
        {
            float delta = stepRad * i;

            float first = preferIncreasing ? link.angle + delta : link.angle - delta;
            float second = preferIncreasing ? link.angle - delta : link.angle + delta;

            if (!IsBlocked(link, first))
            {
                link.angle = first;
                link.angVel = 0f;
                return;
            }

            if (!IsBlocked(link, second))
            {
                link.angle = second;
                link.angVel = 0f;
                return;
            }
        }

        //no clear angle found anywhere in range - leave it be rather than guess further
    }

    bool IsBlocked(ChainLink link, float testAngle)
    {
        Vector2 testEnd = link.start + link.length * new Vector2(Mathf.Cos(testAngle), Mathf.Sin(testAngle));
        Vector2 testMid = (link.start + testEnd) / 2f;
        float rotDeg = testAngle * Mathf.Rad2Deg;

        Vector2 skinnedSize = new Vector2(
            Mathf.Max(0.01f, link.length - collisionSkin),
            Mathf.Max(0.01f, link.width - collisionSkin));

        return Physics2D.OverlapBox(testMid, skinnedSize, rotDeg, LayerMask.GetMask("Ground")) != null;
    }

    //removes chainLinks[index..] from the live simulation immediately (so physics/aiming/collision
    //stop treating them as part of the chain right away), then destroys their GameObjects one at a
    //time with a short delay between each - a cascade/chain-reaction look instead of an instant
    //simultaneous wipe. Each one spawns a ChainExplosion at its own position/length first.
    public void CutFrom(int index)
    {
        if (chainLinks == null || index < 0 || index >= chainLinks.Length) return;

        int removedCount = chainLinks.Length - index;
        ChainLink[] toDestroy = new ChainLink[removedCount];
        System.Array.Copy(chainLinks, index, toDestroy, 0, removedCount);

        System.Array.Resize(ref chainLinks, index);
        RefreshLinkVisuals();

        StartCoroutine(CascadeDestroy(toDestroy));
    }

    IEnumerator CascadeDestroy(ChainLink[] toDestroy)
    {
        for (int i = 0; i < toDestroy.Length; i++)
        {
            ChainLink link = toDestroy[i];
            if (link != null)
            {
                SpawnExplosion(link);
                Destroy(link.gameObject);
            }

            if (i < toDestroy.Length - 1)
            {
                yield return new WaitForSeconds(cascadeDelay);
            }
        }
    }

    void SpawnExplosion(ChainLink link)
    {
        if (chainExplosionPrefab == null) return;

        //a zero-length "pending joint" (added by a Joint pickup, not yet given length) renders as
        //fully invisible - exploding it would look like an explosion appearing from nowhere, since
        //there was nothing visible there to begin with. Skip the VFX, just remove it silently.
        if (link.length <= 0.01f) return;

        //the offsets baked into ChainExplosion are anchored to the source art's end-cap piece
        //position, not the rod's center - so this has to spawn at link.end(), not link.rodMid()
        GameObject fx = Instantiate(chainExplosionPrefab, link.end(), Quaternion.Euler(0f, 0f, link.angle * Mathf.Rad2Deg));
        ChainExplosion explosion = fx.GetComponent<ChainExplosion>();
        if (explosion != null)
        {
            explosion.linkLength = link.length;
        }
    }

    //appends newLink as the new last link in the chain (used by the Joint pickup)
    public void AddLink(ChainLink newLink)
    {
        int oldLength = chainLinks.Length;
        System.Array.Resize(ref chainLinks, oldLength + 1);
        chainLinks[oldLength] = newLink;
        AssignLinkMetadata();
        RefreshLinkVisuals();
    }

    //re-lays out every remaining link's visual - needed because whether a link is the "last" one
    //(plain end cap vs. jointed end cap) can change whenever the chain grows or shrinks.
    void RefreshLinkVisuals()
    {
        foreach (ChainLink l in chainLinks)
        {
            l.ApplyLength();
        }
    }

    public float wrapAngle(float a)
    {
        return Mathf.Atan2(Mathf.Sin(a), Mathf.Cos(a));
    }

    public float rotateToward(float current, float target, float maxStep)
    {
        float d = wrapAngle(target - current);
        return current + Mathf.Clamp(d, -maxStep, maxStep);

    }


}
