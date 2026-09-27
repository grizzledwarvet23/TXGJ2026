using UnityEngine;
using System.Collections;

//plays the 5-frame chain-link shatter effect: frames 0-1 are the intact rod (main body + end cap)
//cracking, frames 2-4 are it shattering into scattered debris. Spawned by Chain.CutFrom per link.
public class ChainExplosion : MonoBehaviour
{
    public Sprite[] frame1Pieces;
    public Sprite[] frame2Pieces;
    public Sprite[] frame3Pieces;
    public Sprite[] frame4Pieces;
    public Sprite[] frame5Pieces;

    //local offsets (world units, relative to this object's own position/rotation), index-aligned
    //with the frameXPieces arrays above - reproduces the layout from the source spritesheets
    public Vector2[] frame1Offsets;
    public Vector2[] frame2Offsets;
    public Vector2[] frame3Offsets;
    public Vector2[] frame4Offsets;
    public Vector2[] frame5Offsets;

    public float frameDuration = 0.05f;

    //uniform nudge applied to every piece in every frame - tweak this directly on the prefab in the
    //Inspector if the effect looks slightly offset from where the link actually was, rather than
    //needing the per-piece offset math redone.
    public Vector2 originOffset;

    //native size (world units) of the frame1/2 main body piece and end cap piece in the source art -
    //used to stretch the main body to match the actual link that exploded instead of always using
    //the source art's fixed reference length
    public float nativeMainBodyWidth = 1.68f;
    public float nativeEndCapWidth = 0.35f;

    //set by Chain.cs right after instantiating this prefab
    [System.NonSerialized] public float linkLength = 1f;

    SpriteRenderer[] renderers;
    Sprite[][] frames;
    Vector2[][] offsets;

    void Awake()
    {
        frames = new[] { frame1Pieces, frame2Pieces, frame3Pieces, frame4Pieces, frame5Pieces };
        offsets = new[] { frame1Offsets, frame2Offsets, frame3Offsets, frame4Offsets, frame5Offsets };

        int maxPieces = 0;
        foreach (Sprite[] f in frames) maxPieces = Mathf.Max(maxPieces, f.Length);

        renderers = new SpriteRenderer[maxPieces];
        for (int i = 0; i < maxPieces; i++)
        {
            GameObject go = new GameObject("Piece" + i);
            go.transform.SetParent(transform, false);
            renderers[i] = go.AddComponent<SpriteRenderer>();
            renderers[i].enabled = false;
        }
    }

    void Start()
    {
        StartCoroutine(Play());
    }

    IEnumerator Play()
    {
        for (int f = 0; f < frames.Length; f++)
        {
            ShowFrame(f);
            yield return new WaitForSeconds(frameDuration);
        }
        Destroy(gameObject);
    }

    void ShowFrame(int frameIndex)
    {
        Sprite[] pieces = frames[frameIndex];
        Vector2[] offs = offsets[frameIndex];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (i >= pieces.Length)
            {
                renderers[i].enabled = false;
                continue;
            }

            renderers[i].enabled = true;
            renderers[i].sprite = pieces[i];

            //piece 0 on frames 0/1 (array index) is the main body - stretch it to roughly match
            //the actual exploding link's length instead of the source art's fixed reference length.
            //everything else (end cap, all debris) stays at native size.
            if (frameIndex <= 1 && i == 0)
            {
                float stretch = Mathf.Max(0.01f, linkLength - nativeEndCapWidth) / nativeMainBodyWidth;
                renderers[i].transform.localScale = new Vector3(stretch, 1f, 1f);
            }
            else
            {
                renderers[i].transform.localScale = Vector3.one;
            }

            renderers[i].transform.localPosition = offs[i] + originOffset;
        }
    }
}
