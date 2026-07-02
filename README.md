# Physics Sandbox

**An interactive, no-code Unity environment for teaching physics in the classroom.**

Physics Sandbox is a Unity-based educational simulation tool that lets instructors build, configure, and demonstrate physics scenarios without writing any code. It covers classical mechanics, aerodynamics, electromagnetism, and energy analysis through real-time, interactive 3D scenes — all controlled from a single in-editor dashboard.

---

## What It Does

Students and instructors can:

- Spawn physical objects and configure their mass, density, and material properties
- Visualize forces on any body in real time with Free Body Diagrams
- Simulate aerodynamic drag with a directional baked lookup model
- Build and run spring, inclined plane, and projectile scenarios
- Explore electric charge behavior with trail visualization
- Plot kinetic, potential, and total energy live on bar charts
- Visualize 3D vector fields (radial, uniform, vortex) with Burst-computed arrow grids
- Load structured lesson packs and navigate slides without leaving the editor
- Export and restore full scenes to JSON for sharing and grading

---

## Feature Overview

### Physics Systems

| System | Description |
|---|---|
| **Mass & Bodies** | Volume-based mass calculation, density and material assignment per object |
| **Air Drag** | Linear and quadratic drag models with per-object directional baked lookup tables |
| **Free Body Diagrams** | Real-time force arrows (gravity, normal, friction, drag, net) on any Rigidbody |
| **Springs** | Hooke's law spring between any two bodies with live visual rendering |
| **Inclined Planes** | Procedurally generated ramps with configurable angle, length, and material |
| **Vector Fields** | 3D field grid (radial, uniform, vortex sources) computed with Unity Burst |
| **Electromagnetism** | Charge trail visualization for moving charged particles |
| **Energy Graphs** | Live KE / PE / TE bar charts locked to initial total energy for scale stability |
| **Paths** | Linear waypoint and parametric curved path rendering |
| **Environment** | JSON-configured material library (friction, bounciness) applied globally to surfaces |

### Educator Tools

| Tool | Description |
|---|---|
| **No-Code Dashboard** | In-editor control panel covering every system — no scripting required |
| **Presentation Mode** | Load and navigate structured lesson packs (slides + notes) inside the editor |
| **Scene Import / Export** | Serialize and restore full scenes to `.json` — share setups between computers |
| **Model Library** | Drag-and-drop 3D model importer (.obj, .fbx, .gltf, .glb) with visual grid browser |
| **Lesson Importer** | Batch-import a folder of slide images into a structured LessonPack asset |

---

## The Dashboard

Open via **Physics Sandbox → Dashboard** in the Unity menu bar.

Every system is accessible from a single collapsible panel — no Inspector hunting, no scripting:

| Area | What You Can Do |
|---|---|
| **Environment** | Set gravity, configure material surfaces, create floors and ramps, control simulation speed |
| **Objects** | Spawn primitives, configure mass and density, bake drag data per object |
| **Free Body Diagrams** | Add FBDs to any selected object, toggle all FBDs on/off at once |
| **Forces** | Create springs between two selected objects, draw linear or curved paths |
| **Fields & EM** | Spawn field sources (radial, uniform, vortex), manage charge trails |
| **Analysis** | Attach energy graphs to any body in the scene |
| **Presentations** | Load a lesson pack, navigate slides forward and back |
| **Scene I/O** | Export or import the full scene to a shareable JSON file |

---

## Architecture

```
Runtime/
├── Clock/              Time control — pause, play, speed multiplier
├── Drag/               Aerodynamic drag with baked directional lookup tables
├── Electromagnetism/   Charge trail visualization
├── Environment/        JSON-driven physics material config for surfaces
├── Field/              3D vector field (Burst parallel jobs, radial/uniform/vortex)
├── FBD/                Free body diagram force rendering
├── Graph/              Real-time energy bar charts (XCharts)
├── ImportExport/       Full scene serialization and 3-pass reconstruction
├── Mass/               Volume-based mass, density, and material sync
├── Mechanics/Springs/  Hooke's law spring physics
├── Path/               Linear waypoint and parametric curved paths
├── PresentationSlides/ Lesson pack and slide ScriptableObjects
└── Visuals/            Arrow pool, field arrow renderer, 2D procedural arrows

Editor/
├── Windows/            Dashboard, Import/Export, Lesson Importer, component editors
├── UXML/               UI Toolkit layout files
└── USS/                Stylesheet files

Samples~/DemoScenes/    Example lesson scenes and supporting assets (import from Package Manager)
```

### Scene Serialization — How It Works

Export serializes every GameObject in the active scene into a flat list of `EntityNode` records (transform, mesh, materials, component data, parent ID) and writes them to a `.json` file. Import reconstructs the scene in three passes:

1. **Spawn** — create GameObjects with base components
2. **Mesh rebuild** — reconstruct geometry from stored vertex/triangle/UV arrays
3. **Reference resolution** — wire up cross-object references via GUID registry

---

## Technical Requirements

| Requirement | Detail |
|---|---|
| Unity version | 2022.3 LTS or newer |
| Render pipeline | Built-in (URP compatible with minor material adjustments) |
| Key packages | Unity.Burst, Unity.Jobs, Unity.Mathematics, TextMeshPro |
| Third-party | [XCharts](https://github.com/XCharts-Team/XCharts) (energy graphs), Newtonsoft.Json (scene I/O) |

---

## Installation

Install via the Unity Package Manager using the git URL:

1. In Unity, open **Window → Package Manager**.
2. Click the **+** button → **Add package from git URL…**
3. Enter:
   ```
   https://github.com/thesuryash/physics-sandbox.git
   ```
4. Click **Add**. Unity resolves the package and its dependencies
   (Burst, Mathematics, Collections, TextMeshPro, Newtonsoft.Json) automatically.

> Requires **Unity 2022.3 LTS** or newer.

### Optional: energy graphs (XCharts)

The live KE / PE / TE energy graphs require **XCharts**, which is not on the Unity
registry and therefore cannot be a package dependency. To enable energy graphs,
install it manually:

- **Package Manager → + → Add package from git URL** →
  `https://github.com/XCharts-Team/XCharts.git`

Once `com.monitor1394.xcharts` is present, the `XCHARTS_PRESENT` define activates and
the energy-graph features compile in automatically. Without it, the rest of the
package works normally and the energy-graph UI shows a hint to install XCharts.

### Importing the demo scenes

Example lesson scenes ship as a package **sample**:

1. Open **Window → Package Manager** and select **Physics Sandbox** in the list.
2. Expand the **Samples** section and click **Import** next to **Demo Scenes**.
3. Unity copies the scenes and supporting assets into
   `Assets/Samples/Physics Sandbox/<version>/Demo Scenes/`.
4. Open any scene from there, then open the dashboard: **Physics Sandbox → Dashboard**.

The physics material config loads automatically from `Resources` (a `physics_config`
`TextAsset` shipped in the package). No additional setup is required.

---

## Presentations & Lessons

Lessons are structured as **LessonPack** ScriptableObjects containing ordered **SlideData** assets (title, description, texture). To create a lesson:

1. Prepare a folder of slide images (`.png` or `.jpg`)
2. Open **Physics Sandbox → Lesson Importer**
3. Drop the folder into the importer — it generates all assets automatically
4. Load the LessonPack from the Dashboard → Presentations panel and navigate slides during your session

---

## License

[MIT](LICENSE.md) — free to use, adapt, and share in educational settings.

---

## Author

Built by **Suryash Malviya** as part of an ongoing educational physics simulation research project.
