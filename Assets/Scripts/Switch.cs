using System;
using UnityEngine;

public class Switch : MonoBehaviour
{

    public GameObject door;

    public Sprite activatedSprite;

    //when false, this switch ignores chain hits entirely - used by encounters (e.g.
    //MitochondriaBossFight) where only one of several switches should be usable at a time
    public bool isActive = true;

    //dims the switch while it's inactive, so it's visually obvious which one is currently live
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    private Color activeColor;

    //fired whenever this switch gets triggered by the correct chain side, regardless of whether a
    //single `door` is wired up - lets a bigger controller react too, on top of the single-door
    //behavior below
    public event Action OnActivated;

    private SpriteRenderer sr;


    //assigned in Awake (not Start) so SetActive() called from another object's own Start() - order
    //between different objects' Start() calls isn't guaranteed - never sees a null sr
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        activeColor = sr.color;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyActiveVisual();
    }

    //use this instead of setting isActive directly if you also want the dimmed/undimmed look to
    //follow along - plain field assignment still works, it just won't update the tint
    public void SetActive(bool active)
    {
        isActive = active;
        ApplyActiveVisual();
    }

    void ApplyActiveVisual()
    {
        sr.color = isActive ? activeColor : inactiveColor;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        //basically check if its the chain and if its the correct side of the chain, then open the door:
        if(other.CompareTag("ChainLink"))
        {
            ChainLink link = other.GetComponent<ChainLink>();
            if(link != null)
            {
                //check if the link is on the deflect side:
                if(!link.IsDeflectSide(transform.position)) // we do different side, cuz deflect side is for bullets and we want the other side to open the door
                {
                    //open the door - if it has a Door script, let it play its blow-up animation;
                    //otherwise just deactivate it like before
                    Door doorScript = door != null ? door.GetComponent<Door>() : null;
                    if (doorScript != null)
                    {
                        doorScript.Explode();
                    }
                    else if (door != null)
                    {
                        door.SetActive(false);
                    }

                    sr.sprite = activatedSprite;

                    OnActivated?.Invoke();
                }
            }
        }
    }
}
