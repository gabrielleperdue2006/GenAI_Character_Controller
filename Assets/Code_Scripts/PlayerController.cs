using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Side-view character controller for the pixel sprite sheet.
/// Keyboard: A/D or arrows to run, Space to jump, K to die, R to get up.
/// Gamepad: left stick or d-pad to run, South button to jump, right bumper to die, Start to get up.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 11f;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float deathY = -6f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int DeadHash = Animator.StringToHash("Dead");

    Rigidbody2D body;
    SpriteRenderer spriteRenderer;
    Animator animator;
    CapsuleCollider2D capsule;

    InputActionMap playerMap;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction dieAction;
    InputAction respawnAction;
    bool ownsPlayerMap;

    Vector2 spawnPoint;
    float moveX;
    float coyoteTimer;
    float jumpBufferTimer;
    float leaveGroundTimer;
    bool grounded;
    bool dead;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider2D>();

        body.gravityScale = gravityScale;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        spawnPoint = transform.position;
        BindInput();
    }

    void BindInput()
    {
        if (inputActions != null)
        {
            playerMap = inputActions.FindActionMap("Player", true).Clone();
            ownsPlayerMap = true;
            moveAction = playerMap.FindAction("Move", true);
            jumpAction = playerMap.FindAction("Jump", true);
            moveAction.AddBinding("<Gamepad>/dpad");
        }
        else
        {
            playerMap = new InputActionMap("Player");
            ownsPlayerMap = true;
            moveAction = playerMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s");
            moveAction.AddCompositeBinding("2DVector")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");
            jumpAction = playerMap.AddAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");
        }

        dieAction = new InputAction("Die", InputActionType.Button);
        dieAction.AddBinding("<Keyboard>/k");
        dieAction.AddBinding("<Gamepad>/rightShoulder");

        respawnAction = new InputAction("Respawn", InputActionType.Button);
        respawnAction.AddBinding("<Keyboard>/r");
        respawnAction.AddBinding("<Gamepad>/start");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        dieAction.Enable();
        respawnAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        dieAction.Disable();
        respawnAction.Disable();
    }

    void OnDestroy()
    {
        if (ownsPlayerMap && playerMap != null)
            playerMap.Dispose();

        dieAction.Dispose();
        respawnAction.Dispose();
    }

    void Update()
    {
        ReadInput();
        grounded = leaveGroundTimer > 0f ? false : CheckGrounded();

        if (grounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (!dead && transform.position.y < deathY)
            BeginDeath(true);

        if (dead)
            moveX = 0f;

        if (moveX > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveX < -0.01f)
            spriteRenderer.flipX = true;

        animator.SetFloat(SpeedHash, dead ? 0f : Mathf.Abs(moveX));
        animator.SetBool(GroundedHash, grounded && leaveGroundTimer <= 0f);
        animator.SetBool(DeadHash, dead);
    }

    void ReadInput()
    {
        if (dieAction.WasPressedThisFrame())
        {
            if (dead)
                Respawn();
            else
                BeginDeath(false);
        }

        if (dead)
        {
            if (respawnAction.WasPressedThisFrame())
                Respawn();
            return;
        }

        moveX = moveAction.ReadValue<Vector2>().x;
        if (Mathf.Abs(moveX) < 0.2f)
            moveX = 0f;

        if (jumpAction.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        if (leaveGroundTimer > 0f)
            leaveGroundTimer -= Time.fixedDeltaTime;

        bool onGround = leaveGroundTimer > 0f ? false : CheckGrounded();
        Vector2 velocity = body.linearVelocity;

        if (dead)
        {
            velocity = Vector2.zero;
        }
        else
        {
            velocity.x = moveX * moveSpeed;

            if (jumpBufferTimer > 0f && (onGround || coyoteTimer > 0f))
            {
                velocity.y = jumpSpeed;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                leaveGroundTimer = 0.08f;
                onGround = false;
            }
        }

        body.linearVelocity = velocity;
        grounded = onGround;
    }

    bool CheckGrounded()
    {
        Bounds bounds = capsule.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.04f);
        Vector2 size = new Vector2(Mathf.Max(0.05f, bounds.size.x * 0.6f), 0.12f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, size, 0f);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].isTrigger)
                continue;
            if (hits[i].attachedRigidbody == body)
                continue;
            return true;
        }

        return false;
    }

    void BeginDeath(bool fell)
    {
        if (dead)
            return;

        dead = true;
        moveX = 0f;
        jumpBufferTimer = 0f;
        body.linearVelocity = Vector2.zero;

        if (fell)
            transform.position = spawnPoint;

        animator.SetBool(DeadHash, true);
    }

    void Respawn()
    {
        dead = false;
        moveX = 0f;
        transform.position = spawnPoint;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = gravityScale;
        animator.SetBool(DeadHash, false);
        animator.Play("Idle", 0, 0f);
    }
}
