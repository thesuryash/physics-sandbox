using UnityEngine;

public class Node : MonoBehaviour
{
    [Header("Node Data")]
    [SerializeField]
    private Vector3 _direction = Vector3.up;

    [Header("Visuals")]
    [Tooltip("Assign the child GameObject that acts as the arrow.")]
    public Transform arrowVisual;
    public float baseLength = 1.0f;

    // A public property to get or set the direction
    public Vector3 Direction
    {
        get { return _direction; }
        set
        {
            // Keep it normalized so the length is strictly controlled by baseLength
            _direction = value.normalized;
            UpdateVisuals();
        }
    }

    void Start()
    {
        UpdateVisuals();
    }

    // Call this whenever the direction changes to rotate the arrow
    public void UpdateVisuals()
    {
        if (arrowVisual == null) return;

        // If the direction is essentially zero, hide the arrow or ignore
        if (_direction.sqrMagnitude < 0.001f) return;

        // Rotate the arrow to point in the direction vector
        arrowVisual.rotation = Quaternion.LookRotation(_direction);

        // Optional: Scale the arrow based on a magnitude if you want later
        // arrowVisual.localScale = new Vector3(1, 1, baseLength); 
    }

    // Example of how you might manipulate it via Unity Physics or Clicks
    private void OnDrawGizmosSelected()
    {
        // Draws a ray in the editor to easily see the node's current direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, _direction * baseLength);
    }
}