using UnityEngine;

public class CatCharacter : MonoBehaviour
{
    public Animator catAnimator; // Reference to the Animator component
    public float moveSpeed = 3f; // Movement speed (adjust as needed)

    [SerializeField]    
    private Vector3[] waypoints; // Array of specified points to move to
    private int currentWaypointIndex = 0; // Index of the current waypoint

    private void Start()
    {
        // Initialize waypoints (you can set these in the Inspector)
        // waypoints = new Vector3[]
        // {
        //     new Vector3(2f, 0f, 0f),
        //     new Vector3(5f, 0f, 3f),
        //     // Add more waypoints as needed
        // };
    }

    private void Update()
    {
        // Check if there are waypoints to follow
        if (currentWaypointIndex < waypoints.Length)
        {
            // Move towards the current waypoint
            Vector3 targetPosition = waypoints[currentWaypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Check if we've reached the current waypoint
            if (transform.position == targetPosition)
            {
                // Switch to the "walk" animation
                catAnimator.SetTrigger("Walk");

                // Move to the next waypoint
                currentWaypointIndex++;
            }
        }
        else
        {
            // No more waypoints, switch to idle animation
            catAnimator.SetTrigger("Idle");
        }
    }
}
