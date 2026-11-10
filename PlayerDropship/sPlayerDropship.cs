using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class sPlayerDropship : MonoBehaviour
{
    private InputSystem_Actions playerControls;
    private InputAction lookInput;
    private InputAction crouchInput;
    private InputAction jumpInput;

    [SerializeField] private Rigidbody rB;
    [SerializeField] private float rotateSensitivity;
    [SerializeField] private float moveSpeed;

    private Vector3 angularVelocity;

    private void Awake()
    {
        playerControls = new InputSystem_Actions();

        lookInput = playerControls.Player.Look;
        crouchInput = playerControls.Player.Crouch;
        jumpInput = playerControls.Player.Jump;        
    }

    private void OnEnable()
    {
        //Inputs
        var input = playerControls.Player;
        input.Enable();
    }

    private void OnDisable()
    {
        //Disable Inputs
        var input = playerControls.Player;
        input.Disable();
    }

    void FixedUpdate()
    {
        //Falloff
        angularVelocity *= 0.995f;

        //Movement Input
        Vector2 stick = playerControls.Player.Move.ReadValue<Vector2>();
        Vector3 stick3 = new Vector3(
            stick.x, 
            jumpInput.IsPressed() ? 1 : crouchInput.IsPressed() ? -1 : 0, 
            stick.y
        );

        //Look Inputs
        Vector2 look = lookInput.ReadValue<Vector2>().normalized; //look sensitivity?
        Vector3 look3 = new Vector3(-look.y, look.x, 0f);

        //Rotate
        angularVelocity += (look3 * rotateSensitivity * Time.deltaTime);
        transform.Rotate(angularVelocity);

        //Move
        rB.AddForce(stick3 * moveSpeed);
    }
}
