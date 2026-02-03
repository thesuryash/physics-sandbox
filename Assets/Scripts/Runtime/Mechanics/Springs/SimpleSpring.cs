using UnityEngine;

public class SimpleSpring : MonoBehaviour
{
    [Header("Physics Objects")]
    public Rigidbody anchor; // The ceiling or fixed point
    public Rigidbody bob;    // The mass hanging down

    [Header("Spring Constants")]
    public float k = 50f;          // Stiffness (N/m)
    public float restLength = 2f;  // L0

    void FixedUpdate()
    {
        if (anchor == null || bob == null) return;

        // 1. Get the vector from Anchor to Bob
        Vector3 currentVector = bob.position - anchor.position;
        float currentDist = currentVector.magnitude;
        Vector3 direction = currentVector.normalized;

        // 2. Calculate displacement (x)
        float displacement = currentDist - restLength;

        // 3. Hooke's Law: F = -k * x
        // We pull the Bob TOWARDS the Anchor, so force is opposite to displacement
        Vector3 force = direction * (-k * displacement);

        // 4. Apply Force
        bob.AddForce(force);

        // Newton's 3rd Law: Apply equal/opposite force to anchor 
        // (Useful if the anchor isn't static, like a double-spring system)
        anchor.AddForce(-force);
    }
}