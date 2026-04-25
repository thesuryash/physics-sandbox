using Unity.Mathematics;

namespace PhysicsSandbox.Fields
{
    /// <summary>
    /// Defines a 3D sampling lattice where each cell stores a 3D vector.
    /// Suitable for volume fields such as electric/magnetic force regions.
    /// </summary>
    public readonly struct FieldGrid3D
    {
        public readonly int SizeX;
        public readonly int SizeY;
        public readonly int SizeZ;
        public readonly float3 CellSize;
        public readonly float3 LocalOrigin;

        public int CellCount => SizeX * SizeY * SizeZ;

        public FieldGrid3D(int sizeX, int sizeY, int sizeZ, float3 cellSize, float3 localOrigin)
        {
            SizeX = math.max(1, sizeX);
            SizeY = math.max(1, sizeY);
            SizeZ = math.max(1, sizeZ);
            CellSize = math.max(new float3(0.0001f), cellSize);
            LocalOrigin = localOrigin;
        }

        public int ToIndex(int x, int y, int z) => ((z * SizeY) + y) * SizeX + x;

        public float3 LocalCellCenter(int x, int y, int z)
        {
            return LocalOrigin + new float3(
                (x + 0.5f) * CellSize.x,
                (y + 0.5f) * CellSize.y,
                (z + 0.5f) * CellSize.z);
        }
    }
}
