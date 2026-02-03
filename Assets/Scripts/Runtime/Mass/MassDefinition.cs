using UnityEngine;

[System.Serializable]
public class MassDefinition
{
    [Header("Inertial")]
    public float density = 1000f;

    [Header("Surface")]
    public float restitution = 0.5f;
    public float staticFriction = 0.6f;
    public float dynamicFriction = 0.4f;

    [Header("Electromagnetic")]
    public float conductivity = 0f;
    public bool isMagnetic = false;

    [Header("Thermodynamic")]
    public float specificHeat = 4184f;
    public float meltingPoint = 273f;

    // --- Dynamic Setters for the Sandbox ---

    public void SetDensity(float newDensity) => density = newDensity;
    public void SetRestitution(float newBounciness) => restitution = Mathf.Clamp01(newBounciness);
    public void SetConductivity(float newSigma) => conductivity = newSigma;
    public void SetSpecificHeat(float newC) => specificHeat = newC;
}