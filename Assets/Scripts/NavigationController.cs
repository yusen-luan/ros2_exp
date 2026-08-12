using UnityEngine;

[RequireComponent(typeof(LidarSensor))]
[RequireComponent(typeof(RobotDrive))]
public class NavigationController : MonoBehaviour
{
    [SerializeField] private Transform goal;

    [SerializeField] private float goalWeight = 1f;
    [SerializeField] private float avoidWeight = 2f;
    [SerializeField] private float avoidanceRadius = 3f; // rays reporting a hit closer than this push the robot away
    [SerializeField] private float arrivalDistance = 0.5f; // stop once within this distance of the goal
    [SerializeField] private float maxTurnAngle = 60f;     // angle (deg) between facing and desired direction that maps to a full turn command
    [SerializeField] private float symmetryBreakBias = 0.15f; // small rightward nudge so a head-on symmetric obstacle doesn't leave the avoidance vector balanced on a knife-edge
    [SerializeField] private float maxTurnRate = 3f;       // max change in the turn command per second, prevents the steering from flipping sign every frame
    [SerializeField] private float minForwardSpeed = 0.3f; // never fully stop translating, even mid-turn — a moving robot resolves standoffs between two obstacles, a stationary rotating one can't

    private LidarSensor lidar;
    private RobotDrive drive;
    private float currentTurn;

    private void Awake()
    {
        lidar = GetComponent<LidarSensor>();
        drive = GetComponent<RobotDrive>();
    }

    private void Update()
    {
        if (goal == null)
        {
            drive.SetVelocity(0f, 0f);
            return;
        }

        Vector3 toGoal = goal.position - transform.position;
        toGoal.y = 0f;

        if (toGoal.magnitude < arrivalDistance)
        {
            drive.SetVelocity(0f, 0f);
            return;
        }

        Vector3 goalDirection = toGoal.normalized;
        Vector3 avoidDirection = ComputeAvoidanceDirection();

        Vector3 desiredDirection = goalDirection * goalWeight + avoidDirection * avoidWeight;
        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            desiredDirection = goalDirection;
        }
        desiredDirection.Normalize();

        float angle = Vector3.SignedAngle(transform.forward, desiredDirection, Vector3.up);
        float targetTurn = Mathf.Clamp(angle / maxTurnAngle, -1f, 1f);
        currentTurn = Mathf.MoveTowards(currentTurn, targetTurn, maxTurnRate * Time.deltaTime);

        float forward = Mathf.Clamp(1f - Mathf.Abs(angle) / 90f, minForwardSpeed, 1f); // slow down for sharp turns, but always keep creeping forward

        drive.SetVelocity(forward, currentTurn);
    }

    // Sums a repulsion vector away from every ray whose hit is within avoidanceRadius, weighted by closeness.
    private Vector3 ComputeAvoidanceDirection()
    {
        Vector3 avoid = Vector3.zero;
        bool obstacleNearby = false;
        float[] ranges = lidar.Ranges;

        for (int i = 0; i < ranges.Length; i++)
        {
            float distance = ranges[i];
            if (distance >= avoidanceRadius)
            {
                continue;
            }

            obstacleNearby = true;
            Vector3 rayDirection = lidar.GetRayDirection(i);
            rayDirection.y = 0f;

            float closeness = (avoidanceRadius - distance) / avoidanceRadius; // 0..1, stronger when closer
            closeness *= closeness; // squared falloff: the nearest obstacle should clearly dominate rather than being evenly tugged by a farther one still in range
            avoid -= rayDirection.normalized * closeness;
        }

        // A perfectly symmetric obstacle (e.g. approached head-on) makes left/right pushes cancel out, leaving
        // the steering decision to frame-to-frame noise. A small consistent nudge reliably breaks the tie.
        if (obstacleNearby)
        {
            avoid += transform.right * symmetryBreakBias;
        }

        return avoid;
    }
}
