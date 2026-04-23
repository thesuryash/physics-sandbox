# 3D Volume Vector Field Architecture

Your idea is strong: a movable **N x N x N point volume** (for example `3x3x3` for debugging and `10x10x10` for richer fields) is the right model when the field itself exists in 3D space.

## Evaluation of the idea

- ✅ **Conceptually correct**: if the phenomenon is truly volumetric, a 2D lattice is underspecified.
- ✅ **Practical for optimization**: a structured 3D grid gives stable indexing and predictable memory access.
- ✅ **Movable region is useful**: shifting the sampled volume lets you inspect a local region of interest without simulating the whole world.
- ⚠️ **Cost scales cubically**: doubling grid resolution on each axis multiplies sample count by `8x`.

A `10x10x10` grid is 1,000 cells, which is very manageable on CPU with Burst for many real-time cases.

## Runtime components

- `FieldGrid3D`
  - 3D lattice dimensions (`SizeX`, `SizeY`, `SizeZ`), per-axis cell size, local origin.
- `FieldSystem3D`
  - Precomputes local sample points once.
  - Converts them to world points every update via `localToWorld` transform matrix.
  - Accumulates vectors in a Burst `IJobParallelFor`.
- `FieldSourceBase`
  - Authoring components mapped to compact `FieldSourceData`.
  - Source kinds: radial, uniform, vortex (axis-based).
- `FieldArrowPool` + `FieldArrowRenderer`
  - Pooled arrow instances mapped one-to-one with cells.

## Performance notes

1. Start with `10x10x10` and profile.
2. Use a smaller debug grid (`3x3x3`) while tuning source behavior.
3. If sources become very numerous, bucket them spatially (grid/octree) before accumulation.
4. Consider dirty-volume updates when the volume and sources are mostly static.
5. Add optional GPU compute backend if you push beyond CPU comfort range.

## Recommended next iteration

- Keep this CPU Burst baseline.
- Add optional **volume-follow target** mode (camera/player).
- Add **LOD volume resolution** (near high-res, far low-res).
- Add an API to query interpolated vector at arbitrary world positions.
