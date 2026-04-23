using Unity.Mathematics;
using UnityEngine;

namespace PhysicsSandbox.Fields
{
    public enum FieldSourceKind : byte
    {
        Radial = 0,
        Uniform = 1,
        Vortex = 2,
    }

    /// <summary>
    /// Compact blittable source data copied into NativeArrays for Burst jobs.
    /// </summary>
    public struct FieldSourceData
    {
        public FieldSourceKind Kind;
        public float3 Position;
        public float3 Direction;
        public float Strength;
        public float Radius;
        public float Falloff;
    }

    public abstract class FieldSourceBase : MonoBehaviour
    {
        [SerializeField] protected float strength = 1f;
        [SerializeField, Min(0.001f)] protected float radius = 5f;
        [SerializeField, Min(0f)] protected float falloff = 2f;
        [SerializeField] protected Vector3 direction = Vector3.up;

        public abstract FieldSourceKind SourceKind { get; }

        public virtual FieldSourceData ToData()
        {
            return new FieldSourceData
            {
                Kind = SourceKind,
                Position = transform.position,
                Direction = math.normalizesafe((float3)direction),
                Strength = strength,
                Radius = radius,
                Falloff = falloff,
            };
        }
    }

    public sealed class RadialFieldSource : FieldSourceBase
    {
        public override FieldSourceKind SourceKind => FieldSourceKind.Radial;
    }

    public sealed class UniformFieldSource : FieldSourceBase
    {
        public override FieldSourceKind SourceKind => FieldSourceKind.Uniform;
    }

    public sealed class VortexFieldSource : FieldSourceBase
    {
        public override FieldSourceKind SourceKind => FieldSourceKind.Vortex;
    }
}
