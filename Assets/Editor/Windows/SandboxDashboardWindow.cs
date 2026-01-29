#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SandboxDashboardWindow : EditorWindow
{
    // Only allowed menu item per your constraint:
    [MenuItem("Physics Sandbox/Dashboard")]
    public static void Open()
    {
        var w = GetWindow<SandboxDashboardWindow>("Dashboard");
        w.minSize = new Vector2(900, 520);
        w.Show();
    }

    // ---------------------------
    // Simple data model (readable)
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
            rootVisualElement.Add(new Label(
                "Missing UXML: Assets/Editor/UXML/SandboxDashboardWindow.uxml"
            ));
            return;
        }

        rootVisualElement.Add(uxml.CloneTree());

        // (Optional) Load USS
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(
            "Assets/Editor/USS/SandboxDashboardWindow.uss"
        );
        if (uss != null)
            rootVisualElement.styleSheets.Add(uss);

        // Query UI elements by name
        _areaList = rootVisualElement.Q<ListView>("areaList");
        _contentRoot = rootVisualElement.Q<VisualElement>("contentRoot");
        _contentHeader = rootVisualElement.Q<Label>("contentHeader");

        // Build data (placeholders allowed)
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
            label.text = _areas[index].Title;
        };

        _areaList.selectionType = SelectionType.Single;
        _areaList.onSelectionChange += selection =>
        {
            foreach (var item in selection)
            {
                if (item is Area a)
                    RenderArea(a);
            }
        };
    }

    private List<Area> BuildAreas()
    {
        // Keep this simple and explicit. Easy to add later.

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
                                new ToolAction { Label = "Open Charge Path Editor", OnClick = () => ChargePathEditorWindow.Open() },
                                new ToolAction { Label = "Open Charge Flow Preview", OnClick = () => ChargeFlowPreviewWindow.Open() },
                                new ToolAction { Label = "Create/Setup Charge Trail", OnClick = CreateOrSetupChargeTrail },

                            }
                        },
                        new Subsection { Title = "Experiments (placeholder)" }
                    }
                }
            }
        };

        var kinematics = new Area
        {
            Title = "Kinematics",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Inclined Plane (placeholder)",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { Title = "Tools (placeholder)" },
                        new Subsection { Title = "Experiments (placeholder)" }
                    }
                }
            }
        };

        var fluids = new Area
        {
            Title = "Fluids",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Basics (placeholder)",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { Title = "Tools (placeholder)" }
                    }
                }
            }
        };

        return new List<Area> { electronics, kinematics, fluids };
    }

    private void CreateOrSetupChargeTrail()
    {
        // Create or find TrailSystem
        var go = GameObject.Find("TrailSystem");
        if (go == null) go = new GameObject("TrailSystem");

        // Ensure LineRenderer exists
        var lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();

        // Ensure ChargeTrail exists (rename if you kept LR.cs)
        var trail = go.GetComponent<ChargeTrail>();
        if (trail == null) trail = go.AddComponent<ChargeTrail>();

        // Select it
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        Debug.Log("TrailSystem created/updated. Assign charge + material in Inspector.");
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
                        var btn = new Button(() => b.OnClick?.Invoke())
                        {
                            text = b.Label
                        };
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
}
#endif
