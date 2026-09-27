using UnityEngine;

public class Mitochondria : MonoBehaviour
{
    //for now we just gonna make this rotate in y axis at some speed:

    public float rotationSpeed = 50f;

    //Sorting Layer/Order exist on every Renderer (MeshRenderer included) but the default Inspector
    //doesn't draw a widget for them on MeshRenderer the way it does for SpriteRenderer - setting
    //them here in code works regardless, since the underlying property is always there.
    public string sortingLayer = "Background";
    public int sortingOrder = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //the actual mesh is a child (imported model prefab instance), not necessarily on this
        //same GameObject, so search children too
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            rend.sortingLayerName = sortingLayer;
            rend.sortingOrder = sortingOrder;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        transform.Rotate(0f, rotationSpeed * Time.fixedDeltaTime, 0f);
    }
}
