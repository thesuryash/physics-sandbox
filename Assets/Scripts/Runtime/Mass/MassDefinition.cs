using UnityEngine;

[System.Serializable]
public class MassDefinition
{
    [Header("Inertial")]
    public float density = 1000f; // kg/m^3

    [Header("Surface Profile")]
    [Tooltip("Used by the Physics Manager to calculate friction/bounce against other materials.")]
    public string materialID = "Generic";

    [Header("Electromagnetic")]
    public float conductivity = 0f;
    public bool isMagnetic = false;

    [Header("Thermodynamic")]
    public float specificHeat = 4184f; // J/(kg*K)
    public float meltingPoint = 273f;  // Kelvin

    // --- Dynamic Setters for the Sandbox ---

    public void SetDensity(float newDensity) => density = newDensity;
    public void SetConductivity(float newSigma) => conductivity = newSigma;
    public void SetSpecificHeat(float newC) => specificHeat = newC;
}