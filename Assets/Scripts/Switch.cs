using UnityEngine;

public class Switch : MonoBehaviour
{

    public GameObject door;

    public Sprite activatedSprite;

    private SpriteRenderer sr;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //basically check if its the chain and if its the correct side of the chain, then open the door:
        if(other.CompareTag("ChainLink"))
        {
            ChainLink link = other.GetComponent<ChainLink>();
            if(link != null)
            {
                //check if the link is on the deflect side:
                if(!link.IsDeflectSide(transform.position)) // we do different side, cuz deflect side is for bullets and we want the other side to open the door
                {
                    //open the door:
                    door.SetActive(false);
                    sr.sprite = activatedSprite;
                }
            }
        }
    }
}
