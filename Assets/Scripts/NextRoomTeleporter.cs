using UnityEngine;

public class NextRoomTeleporter : MonoBehaviour
{
    public Transform teleportTarget;
    public GameObject currentRoom;
    public GameObject nextRoom;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //if current room null, its the parent:
        if(currentRoom == null)
        {
            currentRoom = transform.parent.gameObject;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            other.transform.position = teleportTarget.position;
            //if next room active, deactive:
            if(nextRoom.activeSelf)
            {
                nextRoom.SetActive(false);
            }
            nextRoom.SetActive(true);
            currentRoom.SetActive(false);
        }
    }
}
