using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Four-direction controller for the boy sprite sheet.
/// Keyboard: WASD or arrows to run, Space to jump.
/// Gamepad: left stick or d-pad to run, South button to jump.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoyController : MonoBehaviour
{
    [SerializeField] InputActionAsset inputActions;
    [SerializeField] Sprite[] frames;
    [SerializeField] float moveSpeed = 4f;
    [SerializeField] float idleFrameRate = 6f;
    [SerializeField] float runFrameRate = 10f;
    [SerializeField] float jumpFrameRate = 8f;

    SpriteRenderer spriteRenderer;
    Dictionary<string, Sprite> spritesByName;

    InputActionMap playerMap;
    InputAction moveAction;
    InputAction jumpAction;
    bool ownsPlayerMap;

    string facing = "Down";
    string clipId;
    int frameIndex;
    float frameTimer;
    bool jumping;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spritesByName = new Dictionary<string, Sprite>();

        if (frames != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                Sprite sprite = frames[i];
                if (sprite != null)
                    spritesByName[sprite.name] = sprite;
            }
        }

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
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void OnDestroy()
    {
        if (ownsPlayerMap && playerMap != null)
            playerMap.Dispose();
    }

    void Update()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        if (Mathf.Abs(move.x) < 0.2f)
            move.x = 0f;
        if (Mathf.Abs(move.y) < 0.2f)
            move.y = 0f;
        if (move.sqrMagnitude > 1f)
            move.Normalize();

        bool moving = move.sqrMagnitude > 0f;
        if (moving)
            facing = FacingFrom(move);

        if (!jumping && jumpAction.WasPressedThisFrame())
        {
            jumping = true;
            clipId = "";
        }

        transform.position += new Vector3(move.x, move.y, 0f) * moveSpeed * Time.deltaTime;
        PlayAnimation(moving);
    }

    static string FacingFrom(Vector2 move)
    {
        if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            return move.x > 0f ? "Right" : "Left";

        return move.y > 0f ? "Up" : "Down";
    }

    void PlayAnimation(bool moving)
    {
        string action = jumping ? "Jump" : (moving ? "Run" : "Idle");
        float frameRate = jumping ? jumpFrameRate : (moving ? runFrameRate : idleFrameRate);
        string id = action + facing;

        if (clipId != id)
        {
            clipId = id;
            frameIndex = 0;
            frameTimer = 0f;
        }

        int length = FrameCount(action);
        if (length == 0)
            return;

        frameTimer += Time.deltaTime;
        float frameDuration = 1f / frameRate;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex < length)
                continue;

            if (jumping)
            {
                jumping = false;
                frameIndex = 0;
                frameTimer = 0f;
                clipId = "";
                PlayAnimation(moving);
                return;
            }

            frameIndex = 0;
        }

        Sprite sprite = SpriteAt(action, frameIndex);
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }

    int FrameCount(string action)
    {
        int count = 0;
        while (SpriteAt(action, count) != null)
            count++;
        return count;
    }

    Sprite SpriteAt(string action, int index)
    {
        Sprite sprite;
        if (spritesByName.TryGetValue(action + facing + "_" + index, out sprite))
            return sprite;
        return null;
    }
}
