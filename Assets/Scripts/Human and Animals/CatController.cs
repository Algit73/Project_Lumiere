using UnityEngine;

public class CatController : Interactive
{
    public Animator catAnimator; // Reference to the Animator component
    public float moveSpeed = 3f; // Movement speed (adjust as needed)

    [SerializeField] private Points points; // Reference to the Points scriptable object

    private int currentWaypointIndex = 0; // Index of the current waypoint

    private void Update()
    {
        // Check if there are waypoints to follow
        // if (currentWaypointIndex < points.waypoints.Length)
        // {
        //     // Move towards the current waypoint
        //     Vector3 targetPosition = points.waypoints[currentWaypointIndex].position;
        //     transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        //     // Check if we've reached the current waypoint
        //     if (transform.position == targetPosition)
        //     {
        //         // Switch to the "walk" animation
        //         SetWalkAnimation();

        //         // Move to the next waypoint
        //         currentWaypointIndex++;
        //     }
        // }
        // else
        // {
        //     // No more waypoints, switch to idle animation
        //     SetIdleAnimation();
        // }
    }

    public override void ResetObject()
    {

    }

    public override void Action()
    {
        
    }

    // Call this method to set the cat's animation to "walk"
    public void SetWalkAnimation()
    {
        catAnimator.SetTrigger("Walk");
    }

    // Call this method to set the cat's animation to "idle"
    public void SetIdleAnimation()
    {
        catAnimator.SetTrigger("Idle");
    }
}
