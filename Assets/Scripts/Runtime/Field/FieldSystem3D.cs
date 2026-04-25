using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace PhysicsSandbox.Fields
{
    /// <summary>
    /// Volume field simulator on a 3D lattice.
    /// Grid can move in world space with this GameObject transform.
    /// </summary>
    public sealed class FieldSystem3D : MonoBehaviour, IDisposable
    {
        [Header("Grid Resolution")]
        [SerializeField, Min(1)] private int sizeX = 10;
        [SerializeField, Min(1)] private int sizeY = 10;
        [SerializeField, Min(1)] private int sizeZ = 10;

        [Header("Cell Size")]
        [SerializeField] private Vector3 cellSize = new(1f, 1f, 1f);
        [SerializeField] private Vector3 localOrigin = new(-5f, -5f, -5f);

        [Header("Runtime")]
        [SerializeField] private bool updateInLateUpdate = true;

        private FieldGrid3D grid;
        private NativeArray<float3> localCellPositions;
        private NativeArray<float3> worldCellPositions;
        private NativeArray<float3> cellVectors;
        private NativeList<FieldSourceData> sourceBuffer;

        public FieldGrid3D Grid => grid;
        public NativeArray<float3>.ReadOnly Positions => worldCellPositions.AsReadOnly();
        public NativeArray<float3>.ReadOnly Vectors => cellVectors.AsReadOnly();

        private void OnEnable() => Initialize();

        private void OnDisable() => Dispose();

        private void LateUpdate()
        {
            if (updateInLateUpdate)
            {
                Recompute();
            }
        }

        public void Initialize()
        {
            Dispose();

            grid = new FieldGrid3D(sizeX, sizeY, sizeZ, cellSize, localOrigin);
            int cellCount = grid.CellCount;

            localCellPositions = new NativeArray<float3>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            worldCellPositions = new NativeArray<float3>(cellCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            cellVectors = new NativeArray<float3>(cellCount, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            sourceBuffer = new NativeList<FieldSourceData>(32, Allocator.Persistent);

            int index = 0;
            for (int z = 0; z < grid.SizeZ; z++)
            {
                for (int y = 0; y < grid.SizeY; y++)
                {
                    for (int x = 0; x < grid.SizeX; x++)
                    {
                        localCellPositions[index++] = grid.LocalCellCenter(x, y, z);
                    }
                }
            }
        }

        public void Recompute()
        {
            if (!localCellPositions.IsCreated || !worldCellPositions.IsCreated || !cellVectors.IsCreated)
            {
                Initialize();
            }

            GatherSources();

            float4x4 localToWorld = transform.localToWorldMatrix;
            var job = new FieldAccumulationJob
            {
                LocalPositions = localCellPositions,
                Sources = sourceBuffer.AsArray(),
                LocalToWorld = localToWorld,
                ResultPositions = worldCellPositions,
                ResultVectors = cellVectors,
            };

            JobHandle handle = job.Schedule(grid.CellCount, 64);
            handle.Complete();
        }

        private void GatherSources()
        {
            sourceBuffer.Clear();

            FieldSourceBase[] sources = FindObjectsByType<FieldSourceBase>(FindObjectsSortMode.None);
            foreach (FieldSourceBase source in sources)
            {
                if (source.isActiveAndEnabled)
                {
                    sourceBuffer.Add(source.ToData());
                }
            }
        }

        public void Dispose()
        {
            if (localCellPositions.IsCreated) localCellPositions.Dispose();
            if (worldCellPositions.IsCreated) worldCellPositions.Dispose();
            if (cellVectors.IsCreated) cellVectors.Dispose();
            if (sourceBuffer.IsCreated) sourceBuffer.Dispose();
        }

        [BurstCompile]
        private struct FieldAccumulationJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float3> LocalPositions;
            [ReadOnly] public NativeArray<FieldSourceData> Sources;
            [ReadOnly] public float4x4 LocalToWorld;
            [WriteOnly] public NativeArray<float3> ResultPositions;
            [WriteOnly] public NativeArray<float3> ResultVectors;

            public void Execute(int index)
            {
                float3 worldPos = math.mul(LocalToWorld, new float4(LocalPositions[index], 1f)).xyz;
                float3 total = float3.zero;

                for (int i = 0; i < Sources.Length; i++)
                {
                    FieldSourceData s = Sources[i];
                    float3 delta = worldPos - s.Position;
                    float distance = math.length(delta);

                    if (distance > s.Radius)
                    {
                        continue;
                    }

                    float normalizedDistance = math.saturate(distance / math.max(0.0001f, s.Radius));
                    float attenuation = math.pow(1f - normalizedDistance, math.max(0f, s.Falloff));

                    switch (s.Kind)
                    {
                        case FieldSourceKind.Radial:
                            total += math.normalizesafe(delta) * s.Strength * attenuation;
                            break;
                        case FieldSourceKind.Uniform:
                            total += s.Direction * s.Strength * attenuation;
                            break;
                        case FieldSourceKind.Vortex:
                            float3 tangent = math.normalizesafe(math.cross(math.normalizesafe(s.Direction), delta));
                            total += tangent * s.Strength * attenuation;
                            break;
                    }
                }

                ResultPositions[index] = worldPos;
                ResultVectors[index] = total;
            }
        }
    }
}
