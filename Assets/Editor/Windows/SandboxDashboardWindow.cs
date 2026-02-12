#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SandboxDashboardWindow : EditorWindow
{
    [MenuItem("Physics Sandbox/Dashboard")]
    public static void Open()
    {
        var w = GetWindow<SandboxDashboardWindow>("Dashboard");
        w.minSize = new Vector2(900, 520);
        w.Show();
    }

    // ---------------------------
    // Data Models
    // ---------------------------

    private sealed class ToolAction
    {
        public string Label;
        public Action OnClick;
    }

    private sealed class Subsection
    {
        public string Title;
        public List<ToolAction> Buttons = new();
    }

    private sealed class Section
    {
        public string Title;
        public List<Subsection> Subsections = new();
    }

    private sealed class Area
    {
        public string Title;
        public List<Section> Sections = new();
    }

    // UI references
    private ListView _areaList;
    private VisualElement _contentRoot;
    private Label _contentHeader;

    // Data
    private List<Area> _areas;

    public void CreateGUI()
    {
        // Load UXML
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/Editor/UXML/SandboxDashboardWindow.uxml"
        );

        if (uxml == null)
        {
            rootVisualElement.Add(new Label("Missing UXML: Assets/Editor/UXML/SandboxDashboardWindow.uxml"));
            return;
        }

        rootVisualElement.Add(uxml.CloneTree());

        // Load USS
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/USS/SandboxDashboardWindow.uss"
        );
        if (uss != null) rootVisualElement.styleSheets.Add(uss);

        // Query UI elements
        _areaList = rootVisualElement.Q<ListView>("areaList");
        _contentRoot = rootVisualElement.Q<VisualElement>("contentRoot");
        _contentHeader = rootVisualElement.Q<Label>("contentHeader");

        // Build data
        _areas = BuildAreas();

        // Configure list
        SetupAreaList();

        // Select default area
        if (_areas.Count > 0)
        {
            _areaList.SetSelection(0);
            RenderArea(_areas[0]);
        }
    }

    private void SetupAreaList()
    {
        _areaList.itemsSource = _areas;

        _areaList.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("nav-row");
            var label = new Label();
            label.AddToClassList("nav-row-label");
            row.Add(label);
            return row;
        };

        _areaList.bindItem = (element, index) =>
        {
            var label = element.Q<Label>();
            if (index >= 0 && index < _areas.Count)
                label.text = _areas[index].Title;
        };

        _areaList.selectionType = SelectionType.Single;

        _areaList.selectionChanged += selection =>
        {
            foreach (var item in selection)
            {
                if (item is Area a)
                    RenderArea(a);
            }
        };
    }

    // ---------------------------
    // AREA DEFINITIONS
    // ---------------------------

    private List<Area> BuildAreas()
    {
        // 1. Environment Area
        var environment = new Area
        {
            Title = "Environment",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Scene Setup",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Core Components",
                            Buttons = new List<ToolAction>
                            {
                                new ToolAction { Label = "Create Manager", OnClick = CreateEnvironmentManager },
                                new ToolAction { Label = "Create Floor", OnClick = CreatePhysicsFloor },
                                new ToolAction { Label = "Create Test Cube", OnClick = CreateTestCube }
                            }
                        }
                    }
                },
                new Section
                {
                    Title = "Global Controls",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Gravity",
                            Buttons = new List<ToolAction>
                            {
                                new ToolAction { Label = "Reset Earth (-9.81)", OnClick = () => SetGravity(new Vector3(0, -9.81f, 0)) },
                                new ToolAction { Label = "Zero Gravity", OnClick = () => SetGravity(Vector3.zero) },
                                new ToolAction { Label = "Moon Gravity (-1.62)", OnClick = () => SetGravity(new Vector3(0, -1.62f, 0)) }
                            }
                        }
                    }
                }
            }
        };

        // 2. Electronics Area
        var electronics = new Area
        {
            Title = "Electronics",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Charge & Current",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Tools",
                            Buttons = new List<ToolAction>
                            {
                                // new ToolAction { Label = "Open Charge Path Editor", OnClick = () => ChargePathEditorWindow.Open() },
                                new ToolAction { Label = "Create/Setup Charge Trail", OnClick = CreateOrSetupChargeTrail },
                            }
                        },
                         new Subsection { Title = "Experiments (placeholder)" }
                    }
                }
            }
        };

        // 3. Kinematics Area (UPDATED)
        var kinematics = new Area
        {
            Title = "Kinematics",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Inclined Plane",
                    Subsections = new List<Subsection>
                    {
                         new Subsection
                         {
                            Title = "Tools",
                            Buttons = new List<ToolAction>
                            {
                                new ToolAction { Label = "Create Inclined Plane", OnClick = CreateInclinedPlane }
                            }
                         },
                         new Subsection { Title = "Experiments (placeholder)" }
                    }
                }
            }
        };

        // 4. Fluids Area
        var fluids = new Area
        {
            Title = "Fluids",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Basics",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { Title = "Tools (placeholder)" }
                    }
                }
            }
        };

        return new List<Area> { environment, electronics, kinematics, fluids };
    }

    private void RenderArea(Area area)
    {
        _contentRoot.Clear();
        _contentHeader.text = area.Title;

        foreach (var section in area.Sections)
        {
            var sectionCard = new VisualElement();
            sectionCard.AddToClassList("section-card");

            var sectionTitle = new Label(section.Title);
            sectionTitle.AddToClassList("section-title");
            sectionCard.Add(sectionTitle);

            foreach (var subsection in section.Subsections)
            {
                var subsectionBlock = new VisualElement();
                subsectionBlock.AddToClassList("subsection-block");

                var subsectionTitle = new Label(subsection.Title);
                subsectionTitle.AddToClassList("subsection-title");
                subsectionBlock.Add(subsectionTitle);

                if (subsection.Buttons != null && subsection.Buttons.Count > 0)
                {
                    var buttonRow = new VisualElement();
                    buttonRow.AddToClassList("button-row");

                    foreach (var b in subsection.Buttons)
                    {
                        var btn = new Button(() => b.OnClick?.Invoke()) { text = b.Label };
                        btn.AddToClassList("tool-button");
                        buttonRow.Add(btn);
                    }
                    subsectionBlock.Add(buttonRow);
                }
                else
                {
                    var placeholder = new Label("—");
                    placeholder.AddToClassList("placeholder");
                    subsectionBlock.Add(placeholder);
                }
                sectionCard.Add(subsectionBlock);
            }
            _contentRoot.Add(sectionCard);
        }
    }

    // ---------------------------
    // HELPER ACTIONS
    // ---------------------------

    // --- Kinematics (NEW) ---
    private void CreateInclinedPlane()
    {
        var go = new GameObject("Inclined Plane");
        // Center it in the scene
        go.transform.position = Vector3.zero;

        // Add the script you just wrote
        go.AddComponent<InclinedPlane>();

        Undo.RegisterCreatedObjectUndo(go, "Create Inclined Plane");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    // --- Electronics ---
    private void CreateOrSetupChargeTrail()
    {
        var go = GameObject.Find("TrailSystem");
        if (go == null) go = new GameObject("TrailSystem");

        var lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    // --- Environment ---

    private void CreateEnvironmentManager()
    {
        var go = GameObject.Find("Environment");
        if (go == null)
        {
            go = new GameObject("Environment");
            go.AddComponent<EnvironmentManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create Environment Manager");
        }
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreatePhysicsFloor()
    {
        var go = GameObject.Find("Floor");
        if (go == null)
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Floor";
            go.transform.localScale = new Vector3(20, 0.1f, 20);
            go.transform.position = Vector3.zero;

            var surface = go.AddComponent<EnvironmentSurface>();
            surface.materialID = "Ice";

            Undo.RegisterCreatedObjectUndo(go, "Create Physics Floor");
        }
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateTestCube()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Test Mass";
        go.transform.position = new Vector3(0, 2, 0);
        go.AddComponent<Rigidbody>();

        var surface = go.AddComponent<EnvironmentSurface>();
        surface.materialID = "Rubber";

        Undo.RegisterCreatedObjectUndo(go, "Create Test Cube");
        Selection.activeGameObject = go;
    }

    private void SetGravity(Vector3 newGravity)
    {
        if (EnvironmentManager.Instance != null)
        {
            EnvironmentManager.Instance.Gravity = newGravity;
            Debug.Log($"Gravity set to {newGravity}");
        }
        else
        {
            Physics.gravity = newGravity;
            Debug.Log($"Gravity (Direct) set to {newGravity}");
        }
    }
}
#endif