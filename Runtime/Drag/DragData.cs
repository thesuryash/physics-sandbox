using UnityEngine;
using System;

public enum DragModel
{
    Linear,
    Quadratic
}

[Serializable] // Makes it visible in the Unity Inspector
public class DragSettings
{
    public DragModel model;
    public float airDensity;
    public float linearCoeff_b;
    public float globalCd;
    public bool useDirectionalLookup;
    public float blendSharpness;
}

public struct DirectionSample
{
    public Vector3 dir;
    public float area;
    public float cd;
    public float k;
}

public struct Weights
{
    public int[] idx;
    public float[] w;
}