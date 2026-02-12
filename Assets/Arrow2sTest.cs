using UnityEngine;

public class ArrowTest : MonoBehaviour
{
    public Arrow2D targetArrow;

    [Header("Test Settings")]
    public float magnitudeSpeed = 2f;
    public float minLength = 0.5f;
    public float maxLength = 5f;

    void Update()
    {
        if (targetArrow == null) return;
            
        // 1. Oscillate the length using a Sine wave
        float newLength = minLength + (Mathf.Sin(Time.time * magnitudeSpeed) + 1f) / 2f * (maxLength - minLength);

        // 2. Change color based on length (Green is short/weak, Red is long/strong)
        Color newColor = Color.Lerp(Color.green, Color.red, (newLength - minLength) / (maxLength - minLength));

        // 3. Update the arrow
        targetArrow.SetData(newLength, newColor);
    }
}