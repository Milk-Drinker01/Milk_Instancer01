using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float walkingSpeed = 3;
    public float runningMultiplier = 1.65f;
    public float acceleration = 5;
    Transform camera;
    float yRot;
    CharacterController cc;
    private void Awake()
    {
        camera = transform.GetChild(0);
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        transform.Rotate(0, Input.GetAxis("Mouse X"), 0);
        yRot -= Input.GetAxis("Mouse Y");
        yRot = Mathf.Clamp(yRot, -80, 80);
        camera.localEulerAngles = new Vector3(yRot, 0, 0);
        setInput();
        move();
    }
    Vector2 inputDirection = Vector2.zero;
    Vector2 velocityXZ = Vector2.zero;
    Vector3 velocity = Vector3.zero;
    float speedTarget;
    public void setInput()
    {
        bool[] inputs = new bool[]
            {
                Input.GetKey(KeyCode.W),
                Input.GetKey(KeyCode.A),
                Input.GetKey(KeyCode.S),
                Input.GetKey(KeyCode.D),
                Input.GetKey(KeyCode.LeftShift)
            };
        speedTarget = 0;
        inputDirection = Vector2.zero;
        if (inputs[0])
        {
            inputDirection.y += 1;
            speedTarget = walkingSpeed;
        }
        if (inputs[1])
        {
            inputDirection.x -= 1;
            speedTarget = walkingSpeed;
        }
        if (inputs[2])
        {
            inputDirection.y -= 1;
            speedTarget = walkingSpeed;
        }
        if (inputs[3])
        {
            inputDirection.x += 1;
            speedTarget = walkingSpeed;
        }
        if (inputs[4])
        {
            speedTarget *= runningMultiplier;
        }
    }
    void move()
    {
        if (cc.isGrounded)
        {
            velocity.y = 0;
        }
        Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
        Vector2 right = new Vector2(transform.right.x, transform.right.z);
        Vector2 inputDir = Vector3.Normalize(right * inputDirection.x + forward * inputDirection.y);
        velocityXZ = Vector2.MoveTowards(velocityXZ, inputDir.normalized * speedTarget, Time.deltaTime * acceleration);
        //velocityXZ = Vector2.ClampMagnitude(velocityXZ, speedTarget);
        velocity.x = velocityXZ.x * Time.deltaTime;
        velocity.z = velocityXZ.y * Time.deltaTime;
        velocity.y += -9.81f * Time.deltaTime * Time.deltaTime;

        cc.enabled = true;
        cc.Move(velocity);
        cc.enabled = false;
    }
}
