using UnityEngine;

public class LidarSensor : MonoBehaviour
{
    [SerializeField] private int numRays = 36;
    [SerializeField] private float fieldOfView = 360f;   // degrees covered by the scan, centered on transform.forward
    [SerializeField] private float maxRange = 10f;        // meters
    [SerializeField] private LayerMask obstacleMask = ~0; // what the rays can hit; exclude the robot's own layer if it starts giving false hits

    // Distance reported by each ray this scan; equals maxRange where nothing was hit. This is the simulated "/scan" data.
    public float[] Ranges { get; private set; }

    private void Awake()
    {
        Ranges = new float[numRays];
    }

    private void Update()
    {
        Scan();
    }

    private void Scan()
    {
        for (int i = 0; i < numRays; i++)
        {
            Vector3 direction = GetRayDirection(i);
            if (Physics.Raycast(transform.position, direction, out RaycastHit hit, maxRange, obstacleMask))
            {
                Ranges[i] = hit.distance;
            }
            else
            {
                Ranges[i] = maxRange;
            }
        }
    }

    // Angle 0 is straight ahead (transform.forward); rays are spread evenly across fieldOfView around that.
    public Vector3 GetRayDirection(int rayIndex)
    {
        float angleStep = numRays > 1 ? fieldOfView / (numRays - 1) : 0f;
        float angle = -fieldOfView / 2f + rayIndex * angleStep;
        return Quaternion.Euler(0f, angle, 0f) * transform.forward;
    }

    private void OnDrawGizmos()
    {
        if (Ranges == null || Ranges.Length != numRays)
        {
            return;
        }

        for (int i = 0; i < numRays; i++)
        {
            Vector3 direction = GetRayDirection(i);
            float distance = Ranges[i];
            Gizmos.color = distance < maxRange ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + direction * distance);
        }
    }
}
