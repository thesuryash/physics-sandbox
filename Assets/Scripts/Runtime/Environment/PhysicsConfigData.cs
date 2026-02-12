using System;
using System.Collections.Generic;
using UnityEngine;

// This wrapper matches the JSON root object
[Serializable]
public class PhysicsConfig
{
	public GlobalSettings globalSettings;
	public List<MaterialData> materials;
	public List<InteractionData> interactions;
}

[Serializable]
public class GlobalSettings
{
	public Vector3 gravity;
	public float timeScale;
	public float airDensity;
	public float bounceThreshold; // Minimum velocity for bounce to occur
}

[Serializable]
public class MaterialData
{
	public string id;      // e.g., "Wood"
	public string color;   // e.g., "#8B4513"
    public float staticFriction;
    public float dynamicFriction;
    public float bounciness;
}

[Serializable]
public class InteractionData
{
	public string materialA;
	public string materialB;
	public float staticFriction;
	public float dynamicFriction;
	public float restitution; // Bounciness
}