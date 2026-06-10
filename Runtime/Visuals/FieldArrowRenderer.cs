using System.Collections.Generic;
using PhysicsSandbox.Fields;
using Unity.Mathematics;
using UnityEngine;

namespace PhysicsSandbox.Visualization
{
    /// <summary>
    /// Links simulated vectors to pooled arrow Transforms.
    /// </summary>
    public sealed class FieldArrowRenderer : MonoBehaviour
    {
        [SerializeField] private FieldSystem3D fieldSystem;
        [SerializeField] private FieldArrowPool pool;
        [SerializeField] private float vectorToLength = 0.35f;
        [SerializeField] private float minVisibleMagnitude = 0.0001f;

        private readonly List<Transform> arrows = new();

        private void OnEnable()
        {
            RebuildArrows();
        }

        private void OnDisable()
        {
            ReleaseArrows();
        }

        private void LateUpdate()
        {
            if (fieldSystem == null)
            {
                return;
            }

            if (arrows.Count != fieldSystem.Grid.CellCount)
            {
                RebuildArrows();
            }

            var positions = fieldSystem.Positions;
            var vectors = fieldSystem.Vectors;

            for (int i = 0; i < arrows.Count; i++)
            {
                Transform arrow = arrows[i];
                float3 vector = vectors[i];
                float magnitude = math.length(vector);

                arrow.position = positions[i];
                arrow.gameObject.SetActive(magnitude > minVisibleMagnitude);

                if (magnitude <= minVisibleMagnitude)
                {
                    continue;
                }

                arrow.rotation = Quaternion.LookRotation(math.normalizesafe(vector), Vector3.up);
                arrow.localScale = new Vector3(1f, 1f, magnitude * vectorToLength);
            }
        }

        [ContextMenu("Rebuild Arrows")]
        public void RebuildArrows()
        {
            ReleaseArrows();

            if (fieldSystem == null || pool == null)
            {
                return;
            }

            int count = fieldSystem.Grid.CellCount;
            arrows.Capacity = count;

            for (int i = 0; i < count; i++)
            {
                Transform arrow = pool.Rent();
                arrows.Add(arrow);
            }
        }

        private void ReleaseArrows()
        {
            if (pool == null)
            {
                arrows.Clear();
                return;
            }

            foreach (Transform arrow in arrows)
            {
                pool.Return(arrow);
            }

            arrows.Clear();
        }
    }
}
