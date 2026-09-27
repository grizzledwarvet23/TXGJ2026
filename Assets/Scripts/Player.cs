using UnityEngine;

public class Player : MonoBehaviour
{

    public float moveSpeed = 5f;
    Rigidbody2D rb;
    Collider2D col;
    public float jumpForce = 5f;
    public int health = 3;

    SpriteRenderer spriteRenderer;
    Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //raycast from center
        //do a raycast to see if we are on the ground
        //we will also use a layer mask to only hit the ground layer
        RaycastHit2D hit = Physics2D.Raycast(col.bounds.center, Vector2.down, col.bounds.extents.y + 0.1f, LayerMask.GetMask("Ground"));
        if(hit.collider != null && Input.GetButtonDown("Jump"))
        {
            Jump();
        }


        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = 4f;
        }
        else
        {
            rb.gravityScale = 3f;
        }

        UpdateWalkAnimation();
    }

    void UpdateWalkAnimation()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        bool isWalking = Mathf.Abs(horizontal) > 0.01f;

        if (isWalking && spriteRenderer != null)
        {
            spriteRenderer.flipX = horizontal < 0f;
        }

        //Idle/Walk sprites are driven by the Player Animator Controller now (PlayerIdle.anim /
        //PlayerWalk.anim, switching on the IsWalking bool) - this just flips the facing direction.
        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(Input.GetAxis("Horizontal") * moveSpeed, rb.linearVelocity.y);

    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if(health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        //reload the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Jump(){
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
    }
}
