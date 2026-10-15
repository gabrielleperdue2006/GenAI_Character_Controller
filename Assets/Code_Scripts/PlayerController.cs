using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Side-view character controller for the pixel sprite sheet.
/// Stands at the height it is placed and jumps back to that height.
/// Keyboard: A/D or arrows to run, Space to jump, K to die, R to get up.
/// Gamepad: left stick or d-pad to run, South button to jump, right bumper to die, Start to get up.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float jumpSpeed = 11f;
    [SerializeField] float gravityScale = 3f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float deathY = -6f;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");
    static readonly int DeadHash = Animator.StringToHash("Dead");

    Rigidbody2D body;
    SpriteRenderer spriteRenderer;
    Animator animator;

    InputActionMap playerMap;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction dieAction;
    InputAction respawnAction;
    bool ownsPlayerMap;

    Vector2 spawnPoint;
    float standY;
    float moveX;
    float verticalSpeed;
    float jumpBufferTimer;
    float leaveGroundTimer;
    bool grounded;
    bool jumping;
    bool dead;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.linearVelocity = Vector2.zero;

        spawnPoint = transform.position;
        standY = spawnPoint.y;
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
        grounded = !jumping && leaveGroundTimer <= 0f;

        if (!dead && transform.position.y < deathY)
            BeginDeath(true);

        if (dead)
            moveX = 0f;

        if (moveX > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveX < -0.01f)
            spriteRenderer.flipX = true;

        animator.SetFloat(SpeedHash, dead ? 0f : Mathf.Abs(moveX));
        animator.SetBool(GroundedHash, grounded);
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

        if (dead)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (!jumping && jumpBufferTimer > 0f)
        {
            jumping = true;
            verticalSpeed = jumpSpeed;
            jumpBufferTimer = 0f;
            leaveGroundTimer = 0.08f;
        }

        float nextY = standY;
        if (jumping)
        {
            verticalSpeed += Physics2D.gravity.y * gravityScale * Time.fixedDeltaTime;
            nextY = transform.position.y + verticalSpeed * Time.fixedDeltaTime;
            if (verticalSpeed <= 0f && nextY <= standY)
            {
                nextY = standY;
                verticalSpeed = 0f;
                jumping = false;
            }
        }

        Vector3 position = transform.position;
        position.x += moveX * moveSpeed * Time.fixedDeltaTime;
        position.y = nextY;
        transform.position = position;
        body.linearVelocity = Vector2.zero;
        grounded = !jumping;
    }

    void BeginDeath(bool fell)
    {
        if (dead)
            return;

        dead = true;
        jumping = false;
        moveX = 0f;
        verticalSpeed = 0f;
        jumpBufferTimer = 0f;
        body.linearVelocity = Vector2.zero;

        if (fell)
        {
            transform.position = spawnPoint;
            standY = spawnPoint.y;
        }

        animator.SetBool(DeadHash, true);
    }

    void Respawn()
    {
        dead = false;
        jumping = false;
        moveX = 0f;
        verticalSpeed = 0f;
        standY = spawnPoint.y;
        transform.position = spawnPoint;
        body.linearVelocity = Vector2.zero;
        animator.SetBool(DeadHash, false);
        animator.Play("Idle", 0, 0f);
    }
}
