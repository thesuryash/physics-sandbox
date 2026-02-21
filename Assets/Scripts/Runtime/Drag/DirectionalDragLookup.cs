using UnityEngine;
using System.Collections.Generic;
using System.IO;

/**
 * Represents a lookup table for directional drag coefficients and related properties.
 */
public class DirectionalDragLookup
{
    private List<DirectionSample> samples;
    public bool hasArea;
    public bool hasCd;
    public bool hasK;

    // Constructor to initialize the lookup table
    public DirectionalDragLookup(List<DirectionSample> bakedSamples)
    {
        this.samples = bakedSamples;
        hasArea = true;
        hasCd = true;
        hasK = true;
    }

    /**
     * Serializes the baked samples into a raw binary file.
     */
    public void SaveToBinary(string filePath)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        {
            writer.Write(samples.Count);
            foreach (var sample in samples)
            {
                // Write Vector3 components
                writer.Write(sample.dir.x);
                writer.Write(sample.dir.y);
                writer.Write(sample.dir.z);

                // Write physics data
                writer.Write(sample.area);
                writer.Write(sample.cd);
                writer.Write(sample.k);
            }
        }
        Debug.Log($"Successfully baked {samples.Count} drag samples to {filePath}");
    }

    /**
     * Deserializes the binary file back into a usable lookup table.
     */
    public static DirectionalDragLookup LoadFromBinary(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"No baked drag data found at {filePath}!");
            return null;
        }

        List<DirectionSample> loadedSamples = new List<DirectionSample>();

        using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                DirectionSample s = new DirectionSample();
                s.dir = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                s.area = reader.ReadSingle();
                s.cd = reader.ReadSingle();
                s.k = reader.ReadSingle();
                loadedSamples.Add(s);
            }
        }
        return new DirectionalDragLookup(loadedSamples);
    }

    /**
     * Retrieves the area for the specified direction vector.
     */
    public float GetArea(Vector3 vHat)
    {
        if (samples == null || samples.Count == 0) return 0f;

        Weights weights = GetBlendWeights(vHat);
        float blendedArea = 0f;
        for (int i = 0; i < weights.idx.Length; i++)
        {
            blendedArea += samples[weights.idx[i]].area * weights.w[i];
        }
        return blendedArea;
    }

    /**
     * Retrieves the drag coefficient (Cd) for the specified direction vector.
     */
    public float GetCd(Vector3 vHat)
    {
        if (samples == null || samples.Count == 0) return 0f;

        Weights weights = GetBlendWeights(vHat);
        float blendedCd = 0f;
        for (int i = 0; i < weights.idx.Length; i++)
        {
            blendedCd += samples[weights.idx[i]].cd * weights.w[i];
        }
        return blendedCd;
    }

    /**
     * Retrieves the drag constant (K) for the specified direction vector.
     */
    public float GetK(Vector3 vHat)
    {
        if (samples == null || samples.Count == 0) return 0f;

        Weights weights = GetBlendWeights(vHat);
        float blendedK = 0f;
        for (int i = 0; i < weights.idx.Length; i++)
        {
            blendedK += samples[weights.idx[i]].k * weights.w[i];
        }
        return blendedK;
    }

    /**
     * Computes the blending weights for the directional samples based on the input vector.
     * Uses a low-allocation insertion sort to maintain high performance in FixedUpdate.
     */
    public Weights GetBlendWeights(Vector3 vHat, int topN = 3, float p = 4f)
    {
        Weights weights = new Weights { idx = new int[topN], w = new float[topN] };
        float[] topDots = new float[topN];

        // Initialize with lowest possible dot product values
        for (int i = 0; i < topN; i++) topDots[i] = -2f;

        // Find the top N closest directions
        for (int i = 0; i < samples.Count; i++)
        {
            float dot = Vector3.Dot(vHat, samples[i].dir);

            for (int j = 0; j < topN; j++)
            {
                if (dot > topDots[j])
                {
                    // Shift lower values down
                    for (int k = topN - 1; k > j; k--)
                    {
                        topDots[k] = topDots[k - 1];
                        weights.idx[k] = weights.idx[k - 1];
                    }
                    topDots[j] = dot;
                    weights.idx[j] = i;
                    break;
                }
            }
        }

        // Calculate inverse distance weighting
        float sumWeights = 0f;
        for (int i = 0; i < topN; i++)
        {
            // Max(0, dot) ensures we don't factor in directions pointing away
            float weight = Mathf.Pow(Mathf.Max(0, topDots[i]), p);
            weights.w[i] = weight;
            sumWeights += weight;
        }

        // Normalize weights so they equal 1.0
        if (sumWeights > 0)
        {
            for (int i = 0; i < topN; i++) weights.w[i] /= sumWeights;
        }
        else
        {
            weights.w[0] = 1f; // Fallback if exactly 0
        }

        return weights;
    }
}

public class LookupBuilder
{
    public int directionCount;
    public List<Vector3> directions;

    public DirectionalDragLookup Build(Mesh mesh, DragSettings s, IAreaEstimator areaEst, ICdEstimator cdEst)
    {
        directions = GenerateIcosphereDirections(2); // Default to subdiv 2 (162 directions)
        directionCount = directions.Count;

        List<DirectionSample> bakedSamples = new List<DirectionSample>();

        foreach (Vector3 dir in directions)
        {
            float area = areaEst != null ? areaEst.EstimateArea(mesh, dir) : 1f;
            float cd = cdEst != null ? cdEst.EstimateCd(mesh, dir, area) : s.globalCd;

            // Calculate K (0.5 * rho * Cd * Area)
            float k = 0.5f * s.airDensity * cd * area;

            bakedSamples.Add(new DirectionSample
            {
                dir = dir,
                area = area,
                cd = cd,
                k = k
            });
        }

        return new DirectionalDragLookup(bakedSamples);
    }

    /**
     * Generates the face normals for a unit icosahedron centered at the origin.
     */
    public List<Vector3> GenerateIcosahedronFaceNormals()
    {
        List<Vector3> normals = new List<Vector3>();
        float t = (1f + Mathf.Sqrt(5f)) / 2f; // Golden ratio

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-1,  t,  0).normalized, new Vector3( 1,  t,  0).normalized,
            new Vector3(-1, -t,  0).normalized, new Vector3( 1, -t,  0).normalized,
            new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized,
            new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
            new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized,
            new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized
        };

        int[] faces = new int[]
        {
            0, 11, 5,   0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
            1, 5, 9,    5, 11, 4,  11, 10, 2, 10, 7, 6,  7, 1, 8,
            3, 9, 4,    3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
            4, 9, 5,    2, 4, 11,  6, 2, 10,  8, 6, 7,   9, 8, 1
        };

        for (int i = 0; i < faces.Length; i += 3)
        {
            Vector3 v1 = vertices[faces[i]];
            Vector3 v2 = vertices[faces[i + 1]];
            Vector3 v3 = vertices[faces[i + 2]];

            // Face center (normal of the origin-centered sphere)
            normals.Add(((v1 + v2 + v3) / 3f).normalized);
        }

        return normals;
    }

    /**
     * Generates direction vectors for an icosphere (subdivided icosahedron).
     */
    public List<Vector3> GenerateIcosphereDirections(int subdiv)
    {
        if (subdiv == 0) return GenerateIcosahedronFaceNormals();

        // Start with base icosahedron vertices to form a sphere
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        List<Vector3> verts = new List<Vector3>()
        {
            new Vector3(-1,  t,  0).normalized, new Vector3( 1,  t,  0).normalized,
            new Vector3(-1, -t,  0).normalized, new Vector3( 1, -t,  0).normalized,
            new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized,
            new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
            new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized,
            new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized
        };

        List<int> triangles = new List<int>()
        {
            0, 11, 5,   0, 5, 1,   0, 1, 7,   0, 7, 10,  0, 10, 11,
            1, 5, 9,    5, 11, 4,  11, 10, 2, 10, 7, 6,  7, 1, 8,
            3, 9, 4,    3, 4, 2,   3, 2, 6,   3, 6, 8,   3, 8, 9,
            4, 9, 5,    2, 4, 11,  6, 2, 10,  8, 6, 7,   9, 8, 1
        };

        Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();

        // Local function to get or create middle point
        int GetMiddlePoint(int p1, int p2)
        {
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            if (middlePointIndexCache.TryGetValue(key, out int ret)) return ret;

            Vector3 point1 = verts[p1];
            Vector3 point2 = verts[p2];
            Vector3 middle = new Vector3(
                (point1.x + point2.x) / 2f,
                (point1.y + point2.y) / 2f,
                (point1.z + point2.z) / 2f
            ).normalized; // Push out to sphere surface

            verts.Add(middle);
            int index = verts.Count - 1;
            middlePointIndexCache.Add(key, index);
            return index;
        }

        // Subdivide
        for (int i = 0; i < subdiv; i++)
        {
            List<int> faces2 = new List<int>();
            for (int j = 0; j < triangles.Count; j += 3)
            {
                int a = GetMiddlePoint(triangles[j], triangles[j + 1]);
                int b = GetMiddlePoint(triangles[j + 1], triangles[j + 2]);
                int c = GetMiddlePoint(triangles[j + 2], triangles[j]);

                faces2.AddRange(new int[] { triangles[j], a, c });
                faces2.AddRange(new int[] { triangles[j + 1], b, a });
                faces2.AddRange(new int[] { triangles[j + 2], c, b });
                faces2.AddRange(new int[] { a, b, c });
            }
            triangles = faces2;
        }

        return verts;
    }
}