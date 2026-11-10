using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PLAYER_MODE
{
    orthogonal,
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
    
    private Vector3 fpsCameraFacing;

    [Space(4)]
    [Header("Movement (m/s)")]
    [SerializeField] private Transform orthoNorthRef;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float turnSharpness = 12f; //higher = snappier turn towards travel direction
    [SerializeField] private float gravity = -20f;
    private float verticalVelocity;

    [Space(4)]
    [Header("Ortho camera rotation (Q / E)")]
    [SerializeField] private float cameraRotateStep = 90f;      //degrees per key press
    [SerializeField] private float cameraRotateSharpness = 10f; //higher = snappier swing
    private float orthoYaw, orthoYawTarget;
    private Vector3 gimbalBasePosition;
    private Quaternion gimbalBaseRotation, northBaseRotation;

    private InputSystem_Actions playerControls;

    private sHealth health;
    private bool levelComplete;

    private void Awake()
    {
        playerControls = new InputSystem_Actions();

        //RequireComponent doesn't retrofit existing prefabs
        health = GetComponent<sHealth>();
        if (health == null) health = gameObject.AddComponent<sHealth>();
        if (mesh == null) mesh = transform.Find("Mesh");
        if (orthoNorthRef == null) orthoNorthRef = transform.Find("OrthoNorthRef");

        //Rotation is applied as an offset from the prefab's layout
        gimbalBasePosition = orthoCamGimbal.localPosition;
        gimbalBaseRotation = orthoCamGimbal.localRotation;
        northBaseRotation = orthoNorthRef.localRotation;
    }

    private void OnEnable()
    {
        //Inputs
        var input = playerControls.Player;
        input.Enable();
        input.Shift.performed += Shift;
        input.RotateCamera.performed += RotateCamera;

        //Action Handlers
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
        input.RotateCamera.performed -= RotateCamera;
        input.Disable();

        //Action Handlers
        Actions.OnPlayerDeath -= HandlePlayerDeath;
        Actions.OnLevelComplete -= HandleLevelComplete;
        health.OnDied -= Die;
        health.OnHealthChanged -= BroadcastHealth;
    }


    void Start()
    {
        ApplyMode(PLAYER_MODE.orthogonal);
        BroadcastHealth(health.Current);
    }

    void BroadcastHealth(int current)
    {
        Actions.OnPlayerHealthChanged?.Invoke(current, health.maxHealth);
    }

    void Update()
    {
        if (IsDead) return;

        //Camera
        UpdateOrthoCamera();

        //Movement
        Vector2 stick = playerControls.Player.Move.ReadValue<Vector2>();
        Vector3 direction = orthoNorthRef.transform.TransformDirection(new Vector3(stick.x, 0f, stick.y));
        //Ease the body round to face the way we're driving, so FPS mode starts looking that way
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);
            float t = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime); //frame-rate independent lerp
            mesh.rotation = Quaternion.Slerp(mesh.rotation, target, t);
        }
        Vector3 movement = direction * moveSpeed * Time.deltaTime;
        //Gravity applies in every mode - the robot doesn't float when you look away
        if (cC.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity * Time.deltaTime;
        cC.Move(movement);
    }


    void RotateCamera(InputAction.CallbackContext context)
    {
        if (IsDead || levelComplete || PlayerMode != PLAYER_MODE.orthogonal) return;

        float direction = context.ReadValue<float>(); //Q = -1, E = +1
        if (Mathf.Abs(direction) < 0.5f) return;      //key released
        orthoYawTarget += Mathf.Sign(direction) * cameraRotateStep;
    }

    //Swings the gimbal and the movement reference round the robot together,
    //so WASD stays screen-relative whichever way the camera faces
    void UpdateOrthoCamera()
    {
        if (Mathf.Approximately(orthoYaw, orthoYawTarget)) return;

        float t = 1f - Mathf.Exp(-cameraRotateSharpness * Time.deltaTime);
        orthoYaw = Mathf.Lerp(orthoYaw, orthoYawTarget, t);
        if (Mathf.Abs(orthoYawTarget - orthoYaw) < 0.05f) orthoYaw = orthoYawTarget;

        Quaternion yaw = Quaternion.Euler(0f, orthoYaw, 0f);
        orthoCamGimbal.localPosition = yaw * gimbalBasePosition;
        orthoCamGimbal.localRotation = yaw * gimbalBaseRotation;
        orthoNorthRef.localRotation = yaw * northBaseRotation;

        if (PlayerMode == PLAYER_MODE.orthogonal)
        {
            Cam.transform.localPosition = orthoCamGimbal.localPosition;
            Cam.transform.localRotation = orthoCamGimbal.localRotation;
        }
    }

    void Shift(InputAction.CallbackContext context)
    {
        if (IsDead || levelComplete) return;

        if (PlayerMode == PLAYER_MODE.orthogonal) {
            ApplyMode(PLAYER_MODE.console);
            return;
        }

        if (PlayerMode == PLAYER_MODE.console) {
            ApplyMode(PLAYER_MODE.orthogonal);
            return;
        }
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

                Actions.OnShift?.Invoke(newMode);
                break;

            case PLAYER_MODE.console:
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
        ApplyMode(PLAYER_MODE.console); //the robot's last view is its own error log
    }

    void HandleLevelComplete()
    {
        levelComplete = true;
        ApplyMode(PLAYER_MODE.console);
    }
}
