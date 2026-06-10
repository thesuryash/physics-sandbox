using UnityEngine;

public class Field : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector3Int dimensions = new Vector3Int(5, 5, 5);
    public float spacing = 2.0f;

    [Header("References")]
    [Tooltip("Assign your saved Node prefab here.")]
    public GameObject nodePrefab;


    // A 3D array holding references to the actual Node components
    private Node[,,] grid;

    void Start()
    {
        InitializeGrid();
    }

    void InitializeGrid()
    {
        if (nodePrefab == null)
        {
            Debug.LogError("Node Prefab is missing! Please assign it in the inspector.", this);
            return;
        }

        grid = new Node[dimensions.x, dimensions.y, dimensions.z];

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z; z++)
                {
                    // Calculate local position with spacing
                    Vector3 spawnPos = transform.position + new Vector3(x, y, z) * spacing;

                    // Spawn the prefab and set this Field object as its parent to keep the hierarchy clean
                    GameObject newNodeObj = Instantiate(nodePrefab, spawnPos, Quaternion.identity, transform);
                    newNodeObj.name = $"Node [{x}, {y}, {z}]";

                    // Grab the Node script from the spawned object
                    Node nodeScript = newNodeObj.GetComponent<Node>();

                    if (nodeScript != null)
                    {
                        // Set the initial direction
                        nodeScript.Direction = Vector3.up;

                        // Store it in the array for future manipulation
                        grid[x, y, z] = nodeScript;
                    }
                    else
                    {
                        Debug.LogWarning("The assigned prefab is missing the Node.cs script!");
                    }
                }
            }
        }
    }

    // Helper method to safely get a node at specific coordinates
    public Node GetNode(int x, int y, int z)
    {
        if (x >= 0 && x < dimensions.x && y >= 0 && y < dimensions.y && z >= 0 && z < dimensions.z)
        {
            return grid[x, y, z];
        }
        return null; // Return null if out of bounds
    }

    private void OnDrawGizmos()
    {
        // Draw a simple wireframe box in the editor to visualize the volume of the field
        Vector3 center = transform.position + new Vector3(dimensions.x - 1, dimensions.y - 1, dimensions.z - 1) * spacing * 0.5f;
        Vector3 size = new Vector3(dimensions.x - 1, dimensions.y - 1, dimensions.z - 1) * spacing;

        Gizmos.color = new Color(0, 1, 1, 0.5f); // Semi-transparent cyan
        Gizmos.DrawWireCube(center, size);
    }
}