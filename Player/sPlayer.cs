using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


public enum PLAYER_MODE
{
    orthogonal,
    firstPerson,
    console,
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(sHealth))]

public class sPlayer : MonoBehaviour
{
    public PLAYER_MODE PlayerMode = PLAYER_MODE.orthogonal;
    public Camera Cam;
    public CharacterController cC;
    public bool IsDead { get; private set; }

    [SerializeField] private Transform orthoCamGimbal;
    [SerializeField] private Transform mesh;
    private InputAction lookInput;
    private Vector3 fpsCameraFacing;

    [Space(4)]
    [Header("Movement (m/s)")]
    [SerializeField] private Transform orthoNorthRef;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float turnSharpness = 12f; //higher = snappier turn towards travel direction
    [SerializeField] private float gravity = -20f;
    private float verticalVelocity;

    [Space(4)]
    [Header("Look (degrees per mouse pixel)")]
    [SerializeField] private float lookSensitivity = 0.15f;

    [Space(4)]
    [Header("Shift transition (s)")]
    [SerializeField] private float shiftDuration = 0.25f;
    public bool IsShifting { get; private set; }
    private Coroutine shiftRoutine;

    private InputSystem_Actions playerControls;
    private sHealth health;
    private bool levelComplete;

    private void Awake()
    {
        playerControls = new InputSystem_Actions();
        lookInput = playerControls.Player.Look;
        //RequireComponent doesn't retrofit existing prefabs
        health = GetComponent<sHealth>();
        if (health == null) health = gameObject.AddComponent<sHealth>();
        if (mesh == null) mesh = transform.Find("Mesh");
        if (orthoNorthRef == null) orthoNorthRef = transform.Find("OrthoNorthRef");
    }

    private void OnEnable()
    {
        //Inputs
        var input = playerControls.Player;
        input.Enable();
        input.Shift.performed += Shift;

        //Action Handlers
        Actions.OnRequestShift += HandleRequestShift;
        Actions.OnPlayerDeath += HandlePlayerDeath;
        Actions.OnLevelComplete += HandleLevelComplete;
        health.OnDied += Die;
        health.OnHealthChanged += BroadcastHealth;
    }

    private void OnDisable()
    {
        //Disable Inputs
        var input = playerControls.Player;
        input.Shift.performed -= Shift;
        input.Disable();

        //Action Handlers
        Actions.OnRequestShift -= HandleRequestShift;
        Actions.OnPlayerDeath -= HandlePlayerDeath;
        Actions.OnLevelComplete -= HandleLevelComplete;
        health.OnDied -= Die;
        health.OnHealthChanged -= BroadcastHealth;
    }


    void Start()
    {
        ShiftModeTo(PLAYER_MODE.orthogonal, instant: true);
        BroadcastHealth(health.Current);
    }

    void BroadcastHealth(int current)
    {
        Actions.OnPlayerHealthChanged?.Invoke(current, health.maxHealth);
    }

    void Update()
    {
        if (IsDead) return;

        Vector3 movement = Vector3.zero;

        //Mid-shift: no driving or looking, but gravity still applies
        if (!IsShifting) switch (PlayerMode)
        {
            case PLAYER_MODE.orthogonal:
                movement = Movement();
                break;

            case PLAYER_MODE.firstPerson:
                Look();
                break;

            case PLAYER_MODE.console:
                //no movement or looking, just "console commands"
                break;

            default:
                break;
        }

        //Gravity applies in every mode - the robot doesn't float when you look away
        if (cC.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity * Time.deltaTime;

        cC.Move(movement);
    }

    Vector3 Movement()
    {
        Vector2 stick = playerControls.Player.Move.ReadValue<Vector2>();
        Vector3 direction = orthoNorthRef.transform.TransformDirection(new Vector3(stick.x, 0f, stick.y));

        //Ease the body round to face the way we're driving, so FPS mode starts looking that way
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime); //frame-rate independent lerp
            mesh.rotation = Quaternion.Slerp(mesh.rotation, target, t);
        }

        return direction * moveSpeed * Time.deltaTime;
    }

    void Look()
    {
        Vector2 look = lookInput.ReadValue<Vector2>() * lookSensitivity;
        fpsCameraFacing += new Vector3(-look.y, look.x);
        fpsCameraFacing = new Vector3(Mathf.Clamp(fpsCameraFacing.x, -60f, 60f), fpsCameraFacing.y, 0f); //no roll
        Cam.transform.localRotation = Quaternion.Euler(fpsCameraFacing);

        //Turn the body (not the root - that would spin the ortho camera gimbal too)
        mesh.localRotation = Quaternion.Euler(0f, fpsCameraFacing.y, 0f);
    }

    void Shift(InputAction.CallbackContext context)
    {
        if (IsDead || levelComplete || IsShifting) return;

        if (PlayerMode == PLAYER_MODE.orthogonal) {
            ShiftModeTo(PLAYER_MODE.firstPerson);
            return;
        }

        if (PlayerMode == PLAYER_MODE.firstPerson) {
            ShiftModeTo(PLAYER_MODE.console);
            return;
        }

        if (PlayerMode == PLAYER_MODE.console) {
            ShiftModeTo(PLAYER_MODE.orthogonal);
            return;
        }
    }

    void HandleRequestShift(PLAYER_MODE newMode)
    {
        if (IsDead || levelComplete || IsShifting) return;
        ShiftModeTo(newMode);
    }

    void ShiftModeTo(PLAYER_MODE newMode, bool instant = false)
    {
        if (shiftRoutine != null) StopCoroutine(shiftRoutine);
        shiftRoutine = null;
        IsShifting = false;

        if (instant)
        {
            ApplyMode(newMode);
            return;
        }

        shiftRoutine = StartCoroutine(ShiftRoutine(newMode));
    }

    IEnumerator ShiftRoutine(PLAYER_MODE newMode)
    {
        IsShifting = true;
        Actions.OnShiftStarted?.Invoke(PlayerMode, newMode);

        //Transition animations go here, before the camera changes, e.g.
        //  orthogonal -> firstPerson: plant feet, raise turret
        //  * -> console: scanlines blink (sHUD can listen to OnShiftStarted)
        //Swap the wait for "yield return <your animation coroutine>" when they exist.
        yield return new WaitForSeconds(shiftDuration);

        IsShifting = false;
        shiftRoutine = null;
        ApplyMode(newMode);
    }

    void ApplyMode(PLAYER_MODE newMode)
    {
        PlayerMode = newMode;

        switch (newMode)
        {
            case PLAYER_MODE.orthogonal:
                Cam.transform.localPosition = orthoCamGimbal.localPosition;
                Cam.transform.localRotation = orthoCamGimbal.localRotation;
                Cam.orthographic = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Actions.OnShift?.Invoke(newMode);
                break;

            case PLAYER_MODE.firstPerson:
                fpsCameraFacing = new Vector3(0f, mesh.localEulerAngles.y, 0f);
                Cam.transform.localPosition = Vector3.up;
                Cam.transform.localRotation = Quaternion.Euler(fpsCameraFacing);
                Cam.orthographic = false;
                Cam.nearClipPlane = 1f;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                Actions.OnShift?.Invoke(newMode);
                break;

            case PLAYER_MODE.console:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Actions.OnShift?.Invoke(newMode);
                break;

            default:
                print("Error, unknown mode!");
                break;
        }
    }

    void Die()
    {
        Actions.OnPlayerDeath?.Invoke();
    }

    void HandlePlayerDeath()
    {
        IsDead = true;
        ShiftModeTo(PLAYER_MODE.console, instant: true); //the robot's last view is its own error log
    }

    void HandleLevelComplete()
    {
        levelComplete = true;
        ShiftModeTo(PLAYER_MODE.console, instant: true);
    }
}
