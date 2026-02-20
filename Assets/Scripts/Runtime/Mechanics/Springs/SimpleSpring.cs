using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SimpleSpring : MonoBehaviour
{
    [Header("Physics Objects")]
    public Rigidbody anchor;
    public Rigidbody bob;

    [Header("Spring Constants")]
    public float k = 50f;
    public float restLength = 2f;

    [Header("Visual Settings")]
    public Sprite springSprite;   // Just drop your sideways image here!
    public float springWidth = 0.5f;

    private LineRenderer lineRenderer;
    private Material internalMaterial;
    private Texture2D rotatedTexture; // We keep track of this so we can clean it up later

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 1. Setup the Line appearance
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = springWidth;
        lineRenderer.endWidth = springWidth;

        // 2. Set Texture Mode to Stretch 
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;

        // 3. Automatically create a material and ROTATE the Sprite
        if (springSprite != null && springSprite.texture != null)
        {
            internalMaterial = new Material(Shader.Find("Sprites/Default"));

            // Generate a rotated version of the texture purely in code
            rotatedTexture = RotateTexture90Degrees(springSprite.texture);

            internalMaterial.mainTexture = rotatedTexture;
            lineRenderer.material = internalMaterial;
        }
        else
        {
            Debug.LogWarning("No Spring Sprite assigned to " + gameObject.name);
        }
    }

    void FixedUpdate()
    {
        if (anchor == null || bob == null) return;

        // Physics: Hooke's Law
        Vector3 currentVector = bob.position - anchor.position;
        float currentDist = currentVector.magnitude;
        Vector3 direction = currentVector.normalized;
        float displacement = currentDist - restLength;
        Vector3 force = direction * (-k * displacement);

        bob.AddForce(force);
        anchor.AddForce(-force);

        // Visuals: Update Line positions
        lineRenderer.SetPosition(0, anchor.position);
        lineRenderer.SetPosition(1, bob.position);
    }

    // --- THE CODE FIX FOR SIDEWAYS TEXTURES ---
    private Texture2D RotateTexture90Degrees(Texture2D original)
    {
        // Safety check: Unity requires permission to read pixels via code
        if (!original.isReadable)
        {
            Debug.LogError($"[SimpleSpring] Cannot rotate {original.name}. Please select the image in your Project window, check 'Read/Write' in the Inspector, and click Apply.");
            return original;
        }

        // Create a new blank texture with swapped width and height
        Texture2D rotated = new Texture2D(original.height, original.width, original.format, false);

        Color32[] originalPixels = original.GetPixels32();
        Color32[] rotatedPixels = new Color32[originalPixels.Length];

        int w = original.width;
        int h = original.height;

        // Loop through all pixels and swap their X and Y coordinates (90-degree rotation)
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int newX = y;
                int newY = w - 1 - x;
                rotatedPixels[newY * h + newX] = originalPixels[y * w + x];
            }
        }

        // Apply the rotated pixels to the new texture
        rotated.SetPixels32(rotatedPixels);
        rotated.Apply();

        // Clamp it so the edges don't bleed when stretched!
        rotated.wrapMode = TextureWrapMode.Clamp;

        return rotated;
    }

    private void OnDestroy()
    {
        // Clean up our generated assets to prevent memory leaks
        if (internalMaterial != null) Destroy(internalMaterial);
        if (rotatedTexture != null) Destroy(rotatedTexture);
    }
}