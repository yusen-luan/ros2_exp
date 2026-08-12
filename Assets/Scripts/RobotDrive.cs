using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RobotDrive : MonoBehaviour
{
    [SerializeField] private float maxForwardSpeed = 2f;   // meters/second
    [SerializeField] private float maxTurnSpeed = 90f;      // degrees/second

    private Rigidbody rb;
    private float forwardInput;  // -1..1, set via SetVelocity
    private float turnInput;     // -1..1, set via SetVelocity

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Robot should only translate on the floor plane and rotate around Y (yaw) — lock the rest so it can't tip over.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    // forwardSpeed and turnSpeed are normalized (-1..1); mirrors a ROS cmd_vel style interface (linear.x, angular.z).
    public void SetVelocity(float forwardSpeed, float turnSpeed)
    {
        forwardInput = Mathf.Clamp(forwardSpeed, -1f, 1f);
        turnInput = Mathf.Clamp(turnSpeed, -1f, 1f);
    }

    private void FixedUpdate()
    {
        float yawDelta = turnInput * maxTurnSpeed * Time.fixedDeltaTime;
        Quaternion newRotation = rb.rotation * Quaternion.Euler(0f, yawDelta, 0f);
        rb.MoveRotation(newRotation);

        Vector3 newPosition = rb.position + transform.forward * (forwardInput * maxForwardSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }
}
