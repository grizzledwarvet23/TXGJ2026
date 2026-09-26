using UnityEngine;

public class ChainLink : MonoBehaviour
{

    public float length;
    public float width;
    public float angle; //radians
    public float angVel; //how fast rotating
    public float stiffness; //how harad it snaps to parent
    
    public Vector2 start;
    // BoxCollider2D col;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public Vector2 end()
    {
        return start + length * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public void updateCollider()
    {
        transform.position = (start + end()) / 2;
        transform.eulerAngles = new Vector3(0, 0, angle * Mathf.Rad2Deg);
    }
}
