using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// --- Area Estimators ---
public interface IAreaEstimator
{
	float EstimateArea(Mesh mesh, Vector3 dir);
}

/**
 * Estimates the projected area of a mesh using a rasterized silhouette approach.
 * This checks overlapping triangles against a grid to find the true 2D silhouette area,
 * accounting for concavities and holes.
 */
public class RasterSilhouetteAreaEstimator : IAreaEstimator
{
	public int resolution = 100; // Default resolution of the grid

	public float EstimateArea(Mesh mesh, Vector3 dir)
	{
		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;

		// 1. Create a rotation that looks in the target direction
		Quaternion projectionRot = Quaternion.Inverse(Quaternion.LookRotation(dir));

		Vector2 minBounds = new Vector2(float.MaxValue, float.MaxValue);
		Vector2 maxBounds = new Vector2(float.MinValue, float.MinValue);
		Vector2[] projectedVerts = new Vector2[vertices.Length];

		// 2. Project all vertices onto a 2D plane perpendicular to the direction
		for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 localPt = projectionRot * vertices[i];
			projectedVerts[i] = new Vector2(localPt.x, localPt.y);

			minBounds.x = Mathf.Min(minBounds.x, projectedVerts[i].x);
			minBounds.y = Mathf.Min(minBounds.y, projectedVerts[i].y);
			maxBounds.x = Mathf.Max(maxBounds.x, projectedVerts[i].x);
			maxBounds.y = Mathf.Max(maxBounds.y, projectedVerts[i].y);
		}

		// 3. Setup the raster grid
		float width = maxBounds.x - minBounds.x;
		float height = maxBounds.y - minBounds.y;
		if (width <= 0 || height <= 0) return 0f;

		float cellSize = Mathf.Max(width, height) / resolution;
		int gridX = Mathf.CeilToInt(width / cellSize);
		int gridY = Mathf.CeilToInt(height / cellSize);

		bool[,] grid = new bool[gridX, gridY];
		int hitCount = 0;

		// 4. Rasterize triangles (A simple bounding-box Barycentric approach)
		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector2 p1 = projectedVerts[triangles[i]];
			Vector2 p2 = projectedVerts[triangles[i + 1]];
			Vector2 p3 = projectedVerts[triangles[i + 2]];

			// Triangle bounds
			int minX = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(p1.x, p2.x, p3.x) - minBounds.x) / cellSize));
			int maxX = Mathf.Min(gridX - 1, Mathf.CeilToInt((Mathf.Max(p1.x, p2.x, p3.x) - minBounds.x) / cellSize));
			int minY = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(p1.y, p2.y, p3.y) - minBounds.y) / cellSize));
			int maxY = Mathf.Min(gridY - 1, Mathf.CeilToInt((Mathf.Max(p1.y, p2.y, p3.y) - minBounds.y) / cellSize));

			for (int x = minX; x <= maxX; x++)
			{
				for (int y = minY; y <= maxY; y++)
				{
					if (grid[x, y]) continue; // Already filled

					Vector2 testPoint = new Vector2(
						minBounds.x + (x + 0.5f) * cellSize,
						minBounds.y + (y + 0.5f) * cellSize
					);

					if (IsPointInTriangle(testPoint, p1, p2, p3))
					{
						grid[x, y] = true;
						hitCount++;
					}
				}
			}
		}

		// Area is the number of filled cells multiplied by the area of one cell
		return hitCount * (cellSize * cellSize);
	}

	private bool IsPointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
	{
		float d1 = Sign(pt, v1, v2);
		float d2 = Sign(pt, v2, v3);
		float d3 = Sign(pt, v3, v1);
		bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
		bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
		return !(hasNeg && hasPos);
	}

	private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
	{
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}
}

/**
 * Estimates projected area by computing the 2D convex hull of the projected vertices.
 * This is incredibly fast but overestimates area for objects with holes or concavities.
 */
public class ConvexHullProjectedAreaEstimator : IAreaEstimator
{
	public int hullAlgorithm = 0; // 0 = Monotone Chain

	public float EstimateArea(Mesh mesh, Vector3 dir)
	{
		Vector3[] vertices = mesh.vertices;
		if (vertices.Length < 3) return 0f;

		Quaternion projectionRot = Quaternion.Inverse(Quaternion.LookRotation(dir));
		List<Vector2> projectedVerts = new List<Vector2>(vertices.Length);

		foreach (Vector3 v in vertices)
		{
			Vector3 localPt = projectionRot * v;
			projectedVerts.Add(new Vector2(localPt.x, localPt.y));
		}

		// 1. Get Convex Hull (Monotone Chain algorithm)
		List<Vector2> hull = GetConvexHull(projectedVerts);

		// 2. Calculate Area using the Shoelace formula
		return CalculatePolygonArea(hull);
	}

	private List<Vector2> GetConvexHull(List<Vector2> points)
	{
		points = points.OrderBy(p => p.x).ThenBy(p => p.y).ToList();
		List<Vector2> hull = new List<Vector2>();

		// Lower hull
		foreach (var pt in points)
		{
			while (hull.Count >= 2 && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], pt) <= 0)
				hull.RemoveAt(hull.Count - 1);
			hull.Add(pt);
		}

		// Upper hull
		int lowerCount = hull.Count;
		for (int i = points.Count - 2; i >= 0; i--)
		{
			var pt = points[i];
			while (hull.Count > lowerCount && CrossProduct(hull[hull.Count - 2], hull[hull.Count - 1], pt) <= 0)
				hull.RemoveAt(hull.Count - 1);
			hull.Add(pt);
		}

		hull.RemoveAt(hull.Count - 1); // Remove duplicate start/end point
		return hull;
	}

	private float CrossProduct(Vector2 o, Vector2 a, Vector2 b)
	{
		return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
	}

	private float CalculatePolygonArea(List<Vector2> poly)
	{
		float area = 0f;
		int j = poly.Count - 1;
		for (int i = 0; i < poly.Count; i++)
		{
			area += (poly[j].x + poly[i].x) * (poly[j].y - poly[i].y);
			j = i;
		}
		return Mathf.Abs(area / 2f);
	}
}

// --- Drag Coefficient (Cd) Estimators ---
public interface ICdEstimator
{
	float EstimateCd(Mesh mesh, Vector3 dir, float area);
}

/**
 * Estimates Cd by back-calculating from a known terminal velocity.
 */
public class FittedCdEstimator : ICdEstimator
{
	public List<float> measuredVt;
	public float mass;
	public float rho = 1.225f; // Sea-level air density

	public float EstimateCd(Mesh mesh, Vector3 dir, float area)
	{
		if (measuredVt == null || measuredVt.Count == 0 || area <= 0f) return 1.0f;

		// Take the average terminal velocity from the measured list
		float avgVt = measuredVt.Average();
		if (avgVt <= 0) return 1.0f;

		// Force of Gravity = Drag Force at Terminal Velocity
		// m * g = 0.5 * rho * v^2 * Cd * A
		// Cd = (2 * m * g) / (rho * v^2 * A)

		float gravity = Mathf.Abs(Physics.gravity.y);
		float cd = (2f * mass * gravity) / (rho * avgVt * avgVt * area);

		return cd;
	}
}

/**
 * Estimates Cd using heuristics based on how much of the mesh is "catching" air.
 */
public class HeuristicCdEstimator : ICdEstimator
{
	public float baseCd = 0.5f; // Roughly a sphere
	public float cavityBoost = 0.8f; // Extra drag for cupped shapes (like a parachute)

	public float EstimateCd(Mesh mesh, Vector3 dir, float area)
	{
		Vector3[] normals = mesh.normals;
		int[] triangles = mesh.triangles;

		float flatFacingArea = 0f;
		float totalProjectedArea = 0f;

		// Analyze faces pointing toward the airflow
		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 n1 = normals[triangles[i]];
			Vector3 n2 = normals[triangles[i + 1]];
			Vector3 n3 = normals[triangles[i + 2]];

			// Average normal of the face
			Vector3 faceNormal = (n1 + n2 + n3).normalized;

			// Dot product against the wind direction (dir)
			float alignment = Vector3.Dot(faceNormal, -dir);

			if (alignment > 0)
			{
				totalProjectedArea += alignment;
				// If it's highly perpendicular to the wind, it acts like a flat plate/cavity
				if (alignment > 0.8f)
				{
					flatFacingArea += alignment;
				}
			}
		}

		// Calculate a ratio of how "flat" or "cupped" the object is toward the wind
		float flatRatio = totalProjectedArea > 0 ? (flatFacingArea / totalProjectedArea) : 0f;

		return baseCd + (cavityBoost * flatRatio);
	}
}