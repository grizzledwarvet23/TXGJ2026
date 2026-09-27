using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{

    public float moveSpeed = 5f;
    Rigidbody2D rb;
    Collider2D col;
    public float jumpForce = 5f;
    public int health = 3;

    public float coyoteTime = 0.15f; //grace window after walking off a ledge where jump still works
    float coyoteTimer;

    public float jumpLockout = 0.2f; //blocks re-triggering jump right after one, so rapid tapping can't double-jump
    float jumpLockoutTimer;

    SpriteRenderer spriteRenderer;
    Animator animator;

    bool walkFlipX; //base flipX for walk/idle art, only updated while actually moving
    bool isJumping;  //true from Jump() until landing - the jump art is mirrored vs. the walk art

    public float idleSleepDelay = 30f; //seconds standing still before the sleep animation kicks in
    float idleTimer;

    public float iframeDuration = 1f; //how long the player is immune to damage after being hit
    public float iframeFlickerInterval = 0.1f; //how fast the sprite blinks during that window
    bool invincible;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        walkFlipX = true; //default facing right before any input moves the player
    }

    // Update is called once per frame
    void Update()
    {
        //raycast from center
        //do a raycast to see if we are on the ground
        //we will also use a layer mask to only hit the ground layer
        RaycastHit2D hit = Physics2D.Raycast(col.bounds.center, Vector2.down, col.bounds.extents.y + 0.1f, LayerMask.GetMask("Ground"));
        bool grounded = hit.collider != null;

        if (grounded) isJumping = false;

        if (jumpLockoutTimer > 0f) jumpLockoutTimer -= Time.deltaTime;

        //coyote time: keep the jump window open for a short moment after leaving the ground, so
        //walking off a ledge doesn't feel like it eats a jump input a frame too late.
        //Gated by jumpLockoutTimer too - right after Jump() the physics impulse hasn't actually
        //moved the player off the ground yet, so the raycast can still report grounded for a frame
        //or two; without the lockout, a fast enough tap re-fills coyoteTimer before you've visibly
        //left the ground and lets you double-jump.
        bool canRefillCoyote = grounded && jumpLockoutTimer <= 0f;
        coyoteTimer = canRefillCoyote ? coyoteTime : coyoteTimer - Time.deltaTime;

        if (coyoteTimer > 0f && Input.GetButtonDown("Jump"))
        {
            Jump();
            coyoteTimer = 0f; //used up - no double jump off the same grace window
            jumpLockoutTimer = jumpLockout;
        }


        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = 4f;
        }
        else
        {
            rb.gravityScale = 3f;
        }

        UpdateWalkAnimation(grounded);

        //drives the JumpSquat/Rise/FallTransition/Fall/Land state machine in Player.controller -
        //Grounded catches the Fall->Land transition, VerticalVelocity catches Rise->FallTransition
        if (animator != null)
        {
            animator.SetBool("Grounded", grounded);
            animator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
        }
    }

    void UpdateWalkAnimation(bool grounded)
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        bool isWalking = Mathf.Abs(horizontal) > 0.01f;

        if (isWalking) walkFlipX = horizontal > 0f;

        if (spriteRenderer != null)
        {
            //the jump sprites were drawn mirrored relative to the walk sprites, so whatever flipX
            //value is correct for walk/idle needs to be inverted while any jump pose is showing
            spriteRenderer.flipX = isJumping ? !walkFlipX : walkFlipX;
        }

        //standing still (grounded, no movement input) for idleSleepDelay seconds triggers the sleep
        //animation; moving or leaving the ground immediately resets the timer and wakes it back up
        if (isWalking || !grounded)
        {
            idleTimer = 0f;
        }
        else
        {
            idleTimer += Time.deltaTime;
        }

        //Idle/Walk/Sleep sprites are driven by the Player Animator Controller now (PlayerIdle.anim /
        //PlayerWalk.anim / PlayerSleep.anim, switching on IsWalking/IsSleeping) - this just flips
        //the facing direction and feeds those two bools.
        if (animator != null)
        {
            animator.SetBool("IsWalking", isWalking);
            animator.SetBool("IsSleeping", idleTimer >= idleSleepDelay);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(Input.GetAxis("Horizontal") * moveSpeed, rb.linearVelocity.y);

    }

    public void TakeDamage(int damage)
    {
        if (invincible) return;

        health -= damage;
        if(health <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(IFrames());
    }

    IEnumerator IFrames()
    {
        invincible = true;
        float elapsed = 0f;

        while (elapsed < iframeDuration)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(iframeFlickerInterval);
            elapsed += iframeFlickerInterval;
        }

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        invincible = false;
    }

    void Die()
    {
        //reload the scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Jump(){
        rb.AddForce(new Vector2(0, jumpForce), ForceMode2D.Impulse);
        isJumping = true;
        if (animator != null) animator.SetTrigger("JumpTrigger");
    }
}
