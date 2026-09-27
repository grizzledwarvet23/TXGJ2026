using UnityEngine;
using System.Collections;

//generic multi-frame "shatter into debris" effect - each frame is a set of sprite pieces shown
//together at baked positions, then swapped for the next frame's set. Used for enemy death
//explosions (and anything else that doesn't need per-instance length scaling like ChainExplosion does).
public class ShatterEffect : MonoBehaviour
{
    [System.Serializable]
    public class Frame
    {
        public Sprite[] pieces;
        public Vector2[] offsets; //local offsets (world units), index-aligned with pieces
    }

    public Frame[] frames;
    public float frameDuration = 0.05f;

    //uniform nudge applied to every piece in every frame - tweak on the prefab in the Inspector if
    //the effect looks slightly offset from where the thing that died actually was.
    public Vector2 originOffset;

    SpriteRenderer[] renderers;

    void Awake()
    {
        int maxPieces = 0;
        foreach (Frame f in frames) maxPieces = Mathf.Max(maxPieces, f.pieces.Length);

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
        foreach (Frame f in frames)
        {
            ShowFrame(f);
            yield return new WaitForSeconds(frameDuration);
        }
        Destroy(gameObject);
    }

    void ShowFrame(Frame frame)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i < frame.pieces.Length)
            {
                renderers[i].enabled = true;
                renderers[i].sprite = frame.pieces[i];
                renderers[i].transform.localPosition = frame.offsets[i] + originOffset;
            }
            else
            {
                renderers[i].enabled = false;
            }
        }
    }
}
