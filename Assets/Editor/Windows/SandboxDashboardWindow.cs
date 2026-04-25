#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PhysicsSandbox.Fields;

public class SandboxDashboardWindow : EditorWindow
{
    [MenuItem("Physics Sandbox/Dashboard")]
    public static void Open()
    {
        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
        var w = GetWindow<SandboxDashboardWindow>(new Type[] { inspectorType });
        w.minSize = new Vector2(350, 520);
        w.titleContent = new GUIContent("Dashboard", EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image);
        w.Show();
    }

    // ── Data Models ───────────────────────────────────────────────────────────

    private sealed class ToolAction  { public string Label; public Action OnClick; }
    private sealed class Subsection  { public string Title; public List<ToolAction> Buttons = new(); public Action<VisualElement> CustomRender; }
    private sealed class Section     { public string Title; public List<Subsection> Subsections = new(); }
    private sealed class Area        { public string Title; public List<Section> Sections = new(); }

    // ── State ─────────────────────────────────────────────────────────────────

    private ListView      _areaList;
    private VisualElement _contentRoot;
    private Label         _contentHeader;
    private Button        _tabModules;
    private Button        _tabModels;
    private List<Area>    _moduleAreas;
    private List<Area>    _modelAreas;
    private bool          _isModelTabActive;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void OnHierarchyChange()
    {
        if (_moduleAreas == null || _areaList == null) return;
        int idx = _areaList.selectedIndex;
        _moduleAreas = BuildAreas();
        _modelAreas  = BuildModels();
        _areaList.itemsSource = _isModelTabActive ? _modelAreas : _moduleAreas;
        _areaList.RefreshItems();
        if (idx >= 0 && idx < _areaList.itemsSource.Count)
        {
            _areaList.SetSelection(idx);
            RenderArea((Area)_areaList.itemsSource[idx]);
        }
    }

    public void CreateGUI()
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UXML/SandboxDashboardWindow.uxml");
        if (uxml == null) { rootVisualElement.Add(new Label("Missing UXML: Assets/Editor/UXML/SandboxDashboardWindow.uxml")); return; }

        var tree = uxml.CloneTree();
        tree.style.flexGrow = 1;
        rootVisualElement.Add(tree);

        _areaList      = rootVisualElement.Q<ListView>("areaList");
        _contentRoot   = rootVisualElement.Q<VisualElement>("contentRoot");
        _contentHeader = rootVisualElement.Q<Label>("contentHeader");
        _tabModules    = rootVisualElement.Q<Button>("tabModules");
        _tabModels     = rootVisualElement.Q<Button>("tabModels");

        if (_areaList == null || _contentRoot == null || _contentHeader == null || _tabModules == null || _tabModels == null)
        {
            Debug.LogError("Dashboard: UI elements missing — check UXML names.");
            return;
        }

        _moduleAreas = BuildAreas();
        _modelAreas  = BuildModels();

        _tabModules.clicked += () => SwitchTab(false);
        _tabModels.clicked  += () => SwitchTab(true);

        SetupAreaList();
        SwitchTab(false);
    }

    private void SwitchTab(bool showModels)
    {
        _isModelTabActive = showModels;
        _tabModules.style.backgroundColor = showModels ? new Color(0.3f, 0.3f, 0.3f) : new StyleColor(StyleKeyword.Initial);
        _tabModels.style.backgroundColor  = showModels ? new StyleColor(StyleKeyword.Initial) : new Color(0.3f, 0.3f, 0.3f);
        _areaList.itemsSource = showModels ? _modelAreas : _moduleAreas;
        _areaList.Rebuild();
        if (_areaList.itemsSource.Count > 0)
        {
            _areaList.SetSelection(0);
            RenderArea((Area)_areaList.itemsSource[0]);
        }
        else
        {
            _contentRoot.Clear();
            _contentHeader.text = "Empty";
        }
    }

    private void SetupAreaList()
    {
        _areaList.makeItem = () =>
        {
            var lbl = new Label();
            lbl.style.paddingTop    = 8;
            lbl.style.paddingBottom = 8;
            lbl.style.paddingLeft   = 15;
            lbl.style.fontSize      = 13;
            return lbl;
        };

        _areaList.bindItem = (el, i) =>
        {
            var list = _isModelTabActive ? _modelAreas : _moduleAreas;
            if (i >= 0 && i < list.Count) ((Label)el).text = list[i].Title;
        };

        _areaList.selectionType    = SelectionType.Single;
        _areaList.selectionChanged += sel =>
        {
            foreach (var item in sel)
                if (item is Area a) RenderArea(a);
        };
    }

    // ── AREA DEFINITIONS ─────────────────────────────────────────────────────

    private List<Area> BuildAreas()
    {
        // ── 1. ENVIRONMENT ────────────────────────────────────────────────────
        var environment = new Area
        {
            Title = "Environment",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "World Settings",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Environment Manager",
                            CustomRender = c => BuildAutoUIFor<EnvironmentManager>(
                                c, "Create Manager", CreateEnvironmentManager, "🌍")
                        }
                    }
                },
                new Section
                {
                    Title = "Surfaces",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Physics Surfaces",
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<EnvironmentSurface>(c, null, null, "🧊");
                                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, flexWrap = Wrap.Wrap } };
                                row.Add(new Button(CreatePhysicsFloor) { text = "+ Floor" }.ApplyClass("tool-button"));
                                row.Add(new Button(CreateTestCube)     { text = "+ Test Cube" }.ApplyClass("tool-button"));
                                c.Add(row);
                            }
                        },
                        new Subsection
                        {
                            Title = "Inclined Planes",
                            CustomRender = c => BuildAutoUIFor<InclinedPlane>(
                                c, "+ Create Ramp", CreateInclinedPlane, "📐")
                        }
                    }
                },
                new Section
                {
                    Title = "Time Control",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c => BuildAutoUIFor<Clock>(
                                c, "Create Clock", CreateTimeManager, "⏱️", (card, clock) =>
                                {
                                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, flexWrap = Wrap.Wrap } };
                                    row.Add(new Button(clock.Play)                  { text = "▶ Play"  }.ApplyClass("tool-button"));
                                    row.Add(new Button(clock.Pause)                 { text = "⏸ Pause" }.ApplyClass("tool-button"));
                                    row.Add(new Button(() => clock.SetSpeed(0.25f)) { text = "0.25x"  }.ApplyClass("tool-button"));
                                    row.Add(new Button(() => clock.SetSpeed(0.5f))  { text = "0.5x"   }.ApplyClass("tool-button"));
                                    row.Add(new Button(() => clock.SetSpeed(1f))    { text = "1x"     }.ApplyClass("tool-button"));
                                    row.Add(new Button(() => clock.SetSpeed(2f))    { text = "2x"     }.ApplyClass("tool-button"));
                                    card.Add(row);
                                })
                        }
                    }
                }
            }
        };

        // ── 2. OBJECTS & MASS ─────────────────────────────────────────────────
        var objects = new Area
        {
            Title = "Objects",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Spawn",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Primitives",
                            CustomRender = c =>
                            {
                                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
                                row.Add(new Button(() => SpawnPrimitive(PrimitiveType.Cube))     { text = "Cube"     }.ApplyClass("tool-button"));
                                row.Add(new Button(() => SpawnPrimitive(PrimitiveType.Sphere))   { text = "Sphere"   }.ApplyClass("tool-button"));
                                row.Add(new Button(() => SpawnPrimitive(PrimitiveType.Capsule))  { text = "Capsule"  }.ApplyClass("tool-button"));
                                row.Add(new Button(() => SpawnPrimitive(PrimitiveType.Cylinder)) { text = "Cylinder" }.ApplyClass("tool-button"));
                                row.Add(new Button(() => SpawnPrimitive(PrimitiveType.Plane))    { text = "Plane"    }.ApplyClass("tool-button"));
                                row.Add(new Button(CreateTestMass)                               { text = "Full Mass Object" }.ApplyClass("tool-button"));
                                c.Add(row);
                            }
                        }
                    }
                },
                new Section
                {
                    Title = "Physical Bodies",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c => BuildAutoUIFor<Mass>(
                                c, "Create Mass", CreateTestMass, "📦", (card, mass) =>
                                {
                                    var btn = new Button(() => mass.UpdatePhysicalProperties()) { text = "↻ Apply Physics" };
                                    btn.style.marginTop         = 10;
                                    btn.style.backgroundColor   = new Color(0.2f, 0.4f, 0.6f);
                                    btn.AddToClassList("tool-button");
                                    card.Add(btn);
                                })
                        }
                    }
                },
                new Section
                {
                    Title = "Air Drag",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<RigidBodyDragApplier>(c, null, null, "💨", (card, drag) =>
                                {
                                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8, flexWrap = Wrap.Wrap } };
                                    row.Add(new Button(() => drag.BakeData(false)) { text = "Bake Fast"    }.ApplyClass("tool-button"));
                                    row.Add(new Button(() => drag.BakeData(true))  { text = "Bake Complex" }.ApplyClass("tool-button"));
                                    row.Add(new Button(drag.ClearBake)             { text = "Clear"        }.ApplyClass("tool-button"));
                                    card.Add(row);

                                    if (!string.IsNullOrEmpty(drag.BinaryFilePath))
                                    {
                                        var lbl = new Label($"Baked: {Path.GetFileName(drag.BinaryFilePath)}");
                                        lbl.style.color    = new Color(0.4f, 0.9f, 0.4f);
                                        lbl.style.fontSize = 10;
                                        lbl.style.marginTop = 4;
                                        card.Add(lbl);
                                    }
                                });

                                var addBtn = new Button(AddDragToSelected) { text = "+ Add Drag to Selected" };
                                addBtn.AddToClassList("tool-button");
                                addBtn.style.marginTop = 10;
                                c.Add(addBtn);
                            }
                        }
                    }
                }
            }
        };

        // ── 3. FREE BODY DIAGRAMS ─────────────────────────────────────────────
        var fbd = new Area
        {
            Title = "Free Body Diagrams",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "FBD Controls",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<FreeBodyDiagram>(c, null, null, "📊");

                                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10, flexWrap = Wrap.Wrap } };
                                row.Add(new Button(AddFBDToSelected) { text = "+ Add FBD to Selected" }.ApplyClass("tool-button"));
                                row.Add(new Button(ToggleAllFBDs)    { text = "Toggle All"            }.ApplyClass("tool-button"));
                                c.Add(row);
                            }
                        }
                    }
                }
            }
        };

        // ── 4. FORCES & MECHANICS ─────────────────────────────────────────────
        var forces = new Area
        {
            Title = "Forces",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Springs",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<SimpleSpring>(c, null, null, "🌀");

                                var hint = new Label("Select two GameObjects in the Hierarchy, then:");
                                hint.style.color     = new Color(0.6f, 0.6f, 0.6f);
                                hint.style.fontSize  = 11;
                                hint.style.marginTop = 10;
                                c.Add(hint);

                                var btn = new Button(CreateSpringFromSelection) { text = "+ Create Spring from Selection" };
                                btn.AddToClassList("tool-button");
                                btn.style.marginTop = 4;
                                c.Add(btn);
                            }
                        }
                    }
                },
                new Section
                {
                    Title = "Paths",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<LineRendererPath>(c, null, null, "〰️");

                                var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8, flexWrap = Wrap.Wrap } };
                                row.Add(new Button(CreateLinearPath) { text = "+ Linear Path" }.ApplyClass("tool-button"));
                                row.Add(new Button(CreateCurvedPath) { text = "+ Curved Path" }.ApplyClass("tool-button"));
                                c.Add(row);
                            }
                        }
                    }
                }
            }
        };

        // ── 5. FIELDS & ELECTROMAGNETISM ──────────────────────────────────────
        var fieldsEM = new Area
        {
            Title = "Fields & EM",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Charge Trails",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c => BuildAutoUIFor<ChargeTrail>(
                                c, "Create Charge Trail", CreateOrSetupChargeTrail, "⚡")
                        }
                    }
                },
                new Section
                {
                    Title = "Vector Field",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { CustomRender = RenderFieldControls }
                    }
                }
            }
        };

        // ── 6. ANALYSIS ───────────────────────────────────────────────────────
        var analysis = new Area
        {
            Title = "Analysis",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Energy Graphs",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            CustomRender = c =>
                            {
                                BuildAutoUIFor<EnergyBarGrapher>(c, null, null, "📈");

                                var btn = new Button(AddEnergyGraphToSelected) { text = "+ Add Energy Graph to Selected" };
                                btn.AddToClassList("tool-button");
                                btn.style.marginTop = 10;
                                c.Add(btn);
                            }
                        }
                    }
                }
            }
        };

        // ── 7. PRESENTATIONS ──────────────────────────────────────────────────
        var presentations = new Area
        {
            Title = "Presentations",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Lesson Manager",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { CustomRender = RenderPresentationControls }
                    }
                }
            }
        };

        // ── 8. SCENE I/O ──────────────────────────────────────────────────────
        var sceneIO = new Area
        {
            Title = "Scene I/O",
            Sections = new List<Section>
            {
                new Section
                {
                    Title = "Import & Export",
                    Subsections = new List<Subsection>
                    {
                        new Subsection { CustomRender = RenderSceneIOControls }
                    }
                }
            }
        };

        return new List<Area> { environment, objects, fbd, forces, fieldsEM, analysis, presentations, sceneIO };
    }

    // ── CUSTOM RENDERERS ──────────────────────────────────────────────────────

    private void RenderFieldControls(VisualElement c)
    {
        BuildAutoUIFor<FieldSystem3D>(c, null, null, "🌐");

        var title = new Label("Add Field Source");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.marginTop    = 14;
        title.style.marginBottom = 4;
        c.Add(title);

        var hint = new Label("Spawns a source at the origin. Drag it into a FieldSystem3D after creation.");
        hint.style.color      = new Color(0.6f, 0.6f, 0.6f);
        hint.style.fontSize   = 11;
        hint.style.whiteSpace = WhiteSpace.Normal;
        hint.style.marginBottom = 8;
        c.Add(hint);

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
        row.Add(new Button(() => CreateFieldSource<RadialFieldSource>("Radial Field Source"))   { text = "+ Radial"  }.ApplyClass("tool-button"));
        row.Add(new Button(() => CreateFieldSource<UniformFieldSource>("Uniform Field Source")) { text = "+ Uniform" }.ApplyClass("tool-button"));
        row.Add(new Button(() => CreateFieldSource<VortexFieldSource>("Vortex Field Source"))   { text = "+ Vortex"  }.ApplyClass("tool-button"));
        c.Add(row);

        var systemTitle = new Label("Field System");
        systemTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        systemTitle.style.marginTop    = 14;
        systemTitle.style.marginBottom = 4;
        c.Add(systemTitle);

        var createBtn = new Button(CreateFieldSystem) { text = "+ Create Field System" };
        createBtn.AddToClassList("tool-button");
        c.Add(createBtn);
    }

    private void RenderPresentationControls(VisualElement c)
    {
        var packLabel = new Label("Load a Lesson Pack");
        packLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        packLabel.style.marginBottom = 6;
        c.Add(packLabel);

        var packField = new ObjectField { objectType = typeof(LessonPack) };
        packField.label = "Lesson Pack";
        c.Add(packField);

        var loadBtn = new Button(() =>
        {
            var pack = packField.value as LessonPack;
            if (pack == null) { Debug.LogWarning("Dashboard: No lesson pack selected."); return; }
            var mgr = FindFirstObjectByType<PresentationManager>();
            if (mgr == null) { Debug.LogWarning("Dashboard: No PresentationManager in scene — create one first."); return; }
            mgr.currentLesson = pack;
            Debug.Log($"Dashboard: Loaded lesson pack '{pack.lessonTitle}'");
        }) { text = "Load Lesson Pack" };
        loadBtn.AddToClassList("tool-button");
        loadBtn.style.marginBottom = 14;
        c.Add(loadBtn);

        Separator(c);

        BuildAutoUIFor<PresentationManager>(c, "Create Presentation Manager", CreatePresentationManager, "🖼️", (card, pm) =>
        {
            var navRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
            navRow.Add(new Button(pm.PreviousSlide) { text = "◀ Prev" }.ApplyClass("tool-button"));
            navRow.Add(new Button(pm.NextSlide)     { text = "Next ▶" }.ApplyClass("tool-button"));
            card.Add(navRow);
        });

        Separator(c);

        var importerBtn = new Button(() => EditorApplication.ExecuteMenuItem("Physics Sandbox/Lesson Importer"))
            { text = "Open Lesson Importer" };
        importerBtn.AddToClassList("tool-button");
        importerBtn.style.marginTop = 6;
        c.Add(importerBtn);
    }

    private void RenderSceneIOControls(VisualElement c)
    {
        var desc = new Label("Serialize the full scene to JSON and restore it later.");
        desc.style.color      = new Color(0.7f, 0.7f, 0.7f);
        desc.style.whiteSpace = WhiteSpace.Normal;
        desc.style.marginBottom = 12;
        c.Add(desc);

        var openBtn = new Button(() => EditorApplication.ExecuteMenuItem("Physics Sandbox/Import & Export Data"))
            { text = "Open Import/Export Window" };
        openBtn.AddToClassList("tool-button");
        openBtn.style.marginBottom = 6;
        c.Add(openBtn);

        Separator(c);

        var quickTitle = new Label("Quick Actions");
        quickTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        quickTitle.style.marginTop    = 10;
        quickTitle.style.marginBottom = 6;
        c.Add(quickTitle);

        var exportBtn = new Button(QuickExportScene) { text = "Export Scene Now..." };
        exportBtn.AddToClassList("tool-button");
        exportBtn.style.backgroundColor = new Color(0.15f, 0.45f, 0.15f);
        exportBtn.style.marginBottom = 5;
        c.Add(exportBtn);

        var importBtn = new Button(QuickImportScene) { text = "Import Scene Now..." };
        importBtn.AddToClassList("tool-button");
        importBtn.style.backgroundColor = new Color(0.15f, 0.3f, 0.5f);
        c.Add(importBtn);
    }

    // ── GENERIC AUTO-UI BUILDER ───────────────────────────────────────────────

    private void BuildAutoUIFor<T>(
        VisualElement container,
        string createBtnText,
        Action createAction,
        string iconStr = "⚙️",
        Action<VisualElement, T> appendCustomControls = null) where T : MonoBehaviour
    {
        var instances = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        if (instances.Length == 0)
        {
            if (createAction != null)
            {
                var btn = new Button(createAction) { text = createBtnText };
                btn.AddToClassList("tool-button");
                container.Add(btn);
            }
            else
            {
                var placeholder = new Label($"No {typeof(T).Name} found in scene.");
                placeholder.AddToClassList("placeholder");
                container.Add(placeholder);
            }
            return;
        }

        foreach (var instance in instances)
        {
            var card = new VisualElement();
            card.AddToClassList("section-card");
            card.style.backgroundColor = new Color(0, 0, 0, 0.15f);
            card.style.marginBottom    = 15;

            var contentContainer = new VisualElement();
            bool isExpanded = true;

            // Header
            var headerRow  = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 10 } };
            var headerLeft = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var arrow      = new Label("▼") { style = { fontSize = 10, marginRight = 8, color = new Color(0.7f, 0.7f, 0.7f) } };
            var titleLabel = new Label($"{iconStr} {instance.gameObject.name}") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } };

            headerLeft.Add(arrow);
            headerLeft.Add(titleLabel);
            headerLeft.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                isExpanded = !isExpanded;
                arrow.text = isExpanded ? "▼" : "▶";
                contentContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                evt.StopPropagation();
            });

            var selectBtn = new Button(() => { Selection.activeGameObject = instance.gameObject; EditorGUIUtility.PingObject(instance.gameObject); }) { text = "Select" };
            selectBtn.AddToClassList("tool-button");
            selectBtn.style.marginBottom = 0;

            headerRow.Add(headerLeft);
            headerRow.Add(selectBtn);
            card.Add(headerRow);

            // Reflection-driven property fields
            var so   = new SerializedObject(instance);
            var prop = so.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script") continue;

                if (prop.type == "MassDefinition") { enterChildren = true; continue; }

                if (prop.name == "materialID")
                {
                    var choices = GetMaterialChoices();
                    string cur  = prop.stringValue;
                    if (!choices.Contains(cur))
                        choices.Add(string.IsNullOrEmpty(cur) && choices.Count > 0 ? choices[0] : cur);

                    var dd = new DropdownField(prop.displayName, choices, cur);
                    dd.BindProperty(prop);
                    dd.RegisterValueChangedCallback(_ =>
                    {
                        if (instance is Mass m) dd.schedule.Execute(() => m.UpdatePhysicalProperties()).StartingIn(10);
                    });
                    contentContainer.Add(dd);
                    continue;
                }

                if (prop.name == "visualModel")
                {
                    var objField = new ObjectField("Visual Model") { objectType = typeof(GameObject), value = prop.objectReferenceValue };
                    objField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue == null || instance is not Mass massInst) return;
                        var dropped = (GameObject)evt.newValue;
                        var newVis  = PrefabUtility.IsPartOfPrefabAsset(dropped)
                            ? (GameObject)PrefabUtility.InstantiatePrefab(dropped)
                            : dropped;

                        foreach (var filter in newVis.GetComponentsInChildren<MeshFilter>())
                        {
                            var mc = filter.gameObject.GetComponent<MeshCollider>() ?? filter.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = filter.sharedMesh;
                            mc.convex     = true;
                        }

                        newVis.transform.SetParent(massInst.transform);
                        newVis.transform.localPosition = Vector3.zero;
                        newVis.transform.localRotation = Quaternion.identity;
                        massInst.gameObject.name        = newVis.name.Replace("(Clone)", "");
                        massInst.visualModel            = newVis;
                        EditorUtility.SetDirty(massInst);
                        massInst.UpdatePhysicalProperties();
                    });
                    contentContainer.Add(objField);
                    continue;
                }

                var pf = new PropertyField(prop);
                pf.BindProperty(prop);
                contentContainer.Add(pf);
            }

            appendCustomControls?.Invoke(contentContainer, instance);

            card.Bind(so);
            card.Add(contentContainer);
            container.Add(card);
        }

        if (createAction != null)
        {
            var addBtn = new Button(createAction) { text = $"+ Add {createBtnText}" };
            addBtn.AddToClassList("tool-button");
            container.Add(addBtn);
        }
    }

    // ── AREA RENDERER ─────────────────────────────────────────────────────────

    private void RenderArea(Area area)
    {
        _contentRoot.Clear();
        _contentHeader.text = area.Title;

        foreach (var section in area.Sections)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = new Color(0, 0, 0, 0.5f);
            card.style.paddingTop = card.style.paddingBottom = card.style.paddingLeft = card.style.paddingRight = 15;
            card.style.marginBottom = 20;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;

            var sectionTitle = new Label(section.Title);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.fontSize    = 15;
            sectionTitle.style.marginBottom = 15;
            card.Add(sectionTitle);

            foreach (var sub in section.Subsections)
            {
                var block = new VisualElement { style = { marginBottom = 15 } };

                if (!string.IsNullOrEmpty(sub.Title))
                {
                    var subTitle = new Label(sub.Title);
                    subTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    subTitle.style.fontSize    = 12;
                    subTitle.style.color       = new Color(0.7f, 0.7f, 0.7f);
                    subTitle.style.marginBottom = 8;
                    block.Add(subTitle);
                }

                if (sub.CustomRender != null)
                {
                    sub.CustomRender.Invoke(block);
                }
                else if (sub.Buttons?.Count > 0)
                {
                    var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
                    foreach (var b in sub.Buttons)
                    {
                        var btn = new Button(() => b.OnClick?.Invoke()) { text = b.Label };
                        btn.style.paddingTop = btn.style.paddingBottom = btn.style.paddingLeft = btn.style.paddingRight = 6;
                        btn.style.marginRight = btn.style.marginBottom = 5;
                        btnRow.Add(btn);
                    }
                    block.Add(btnRow);
                }
                else
                {
                    var ph = new Label("—");
                    ph.style.color = new Color(0.5f, 0.5f, 0.5f);
                    ph.style.unityFontStyleAndWeight = FontStyle.Italic;
                    block.Add(ph);
                }

                card.Add(block);
            }

            _contentRoot.Add(card);
        }
    }

    // ── HELPER ACTIONS ────────────────────────────────────────────────────────

    private void AddFBDToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("Dashboard: Select an object first."); return; }
        if (go.GetComponent<FreeBodyDiagram>() != null) { Debug.Log($"Dashboard: {go.name} already has an FBD."); return; }
        Undo.AddComponent<FreeBodyDiagram>(go);
    }

    private void ToggleAllFBDs()
    {
        var fbds = FindObjectsByType<FreeBodyDiagram>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (fbds.Length == 0) { Debug.LogWarning("Dashboard: No FBDs in scene."); return; }
        bool anyOn = System.Array.Exists(fbds, f => f.enabled);
        foreach (var f in fbds) f.enabled = !anyOn;
    }

    private void AddDragToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("Dashboard: Select an object first."); return; }
        if (go.GetComponent<RigidBodyDragApplier>() != null) { Debug.Log($"Dashboard: {go.name} already has a drag applier."); return; }
        Undo.AddComponent<RigidBodyDragApplier>(go);
    }

    private void CreateSpringFromSelection()
    {
        var selected = Selection.gameObjects;
        if (selected.Length < 2) { Debug.LogWarning("Dashboard: Select exactly two GameObjects in the Hierarchy."); return; }

        var go = new GameObject("Spring");
        go.transform.position = (selected[0].transform.position + selected[1].transform.position) * 0.5f;

        var spring  = go.AddComponent<SimpleSpring>();
        spring.anchor = selected[0].GetComponent<PhysicsBody>() ?? selected[0].AddComponent<PhysicsBody>();
        spring.bob    = selected[1].GetComponent<PhysicsBody>() ?? selected[1].AddComponent<PhysicsBody>();

        Undo.RegisterCreatedObjectUndo(go, "Create Spring");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateLinearPath()
    {
        var go = new GameObject("Linear Path");
        go.AddComponent<LineRendererPath>();
        Undo.RegisterCreatedObjectUndo(go, "Create Linear Path");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateCurvedPath()
    {
        var go   = new GameObject("Curved Path");
        var path = go.AddComponent<LineRendererPath>();
        var so   = new SerializedObject(path);
        var mode = so.FindProperty("mode");
        if (mode != null) { mode.enumValueIndex = 1; so.ApplyModifiedProperties(); }
        Undo.RegisterCreatedObjectUndo(go, "Create Curved Path");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateFieldSource<T>(string name) where T : FieldSourceBase
    {
        var go = new GameObject(name);
        go.AddComponent<T>();
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateFieldSystem()
    {
        var go = new GameObject("Field System 3D");
        go.AddComponent<FieldSystem3D>();
        Undo.RegisterCreatedObjectUndo(go, "Create Field System 3D");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void AddEnergyGraphToSelected()
    {
        var go = Selection.activeGameObject;
        if (go == null) { Debug.LogWarning("Dashboard: Select an object first."); return; }
        if (go.GetComponent<EnergyBarGrapher>() != null) { Debug.Log($"Dashboard: {go.name} already has an energy graph."); return; }
        Undo.AddComponent<EnergyBarGrapher>(go);
    }

    private void CreatePresentationManager()
    {
        var go = new GameObject("Presentation Manager");
        go.AddComponent<PresentationManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create Presentation Manager");
        Selection.activeGameObject = go;
    }

    private void QuickExportScene()
    {
        string path = EditorUtility.SaveFilePanel("Export Scene", Application.dataPath, "scene_export", "json");
        if (string.IsNullOrEmpty(path)) return;
        EditorApplication.ExecuteMenuItem("Physics Sandbox/Import & Export Data");
        Debug.Log($"Dashboard: Use the Import/Export window to export to: {path}");
    }

    private void QuickImportScene()
    {
        string path = EditorUtility.OpenFilePanel("Import Scene", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        EditorApplication.ExecuteMenuItem("Physics Sandbox/Import & Export Data");
        Debug.Log($"Dashboard: Use the Import/Export window to import from: {path}");
    }

    private void CreateInclinedPlane()
    {
        var go = new GameObject("Inclined Plane");
        go.AddComponent<InclinedPlane>();
        Undo.RegisterCreatedObjectUndo(go, "Create Inclined Plane");
    }

    private void CreateOrSetupChargeTrail()
    {
        var go = GameObject.Find("Charge Trail") ?? new GameObject("Charge Trail");
        if (go.GetComponent<LineRenderer>() == null) go.AddComponent<LineRenderer>();
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void CreateEnvironmentManager()
    {
        var go = new GameObject("Environment");
        go.AddComponent<EnvironmentManager>();
        Undo.RegisterCreatedObjectUndo(go, "Create Environment Manager");
    }

    private void CreateTimeManager()
    {
        string[] guids = AssetDatabase.FindAssets("TimeSpeedPrefab t:Prefab");
        if (guids.Length == 0) { Debug.LogError("Dashboard: 'TimeSpeedPrefab' not found in project."); return; }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        var obj    = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "Create Time Manager");
        Selection.activeGameObject = obj;
    }

    private void CreatePhysicsFloor()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Floor";
        go.transform.localScale = new Vector3(20, 0.1f, 20);
        var surface = go.AddComponent<EnvironmentSurface>();
        surface.materialID = "Ice";
        Undo.RegisterCreatedObjectUndo(go, "Create Physics Floor");
    }

    private void CreateTestCube()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Test Mass";
        go.transform.position = new Vector3(0, 2, 0);
        go.AddComponent<Rigidbody>();
        go.AddComponent<EnvironmentSurface>().materialID = "Rubber";
        Undo.RegisterCreatedObjectUndo(go, "Create Test Cube");
    }

    private void CreateTestMass()
    {
        var go  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Test Mass";
        go.transform.position = new Vector3(0, 2, 0);
        var fbd  = go.AddComponent<FreeBodyDiagram>();
        var mass = go.GetComponent<Mass>();
        if (mass != null) mass.fbd = fbd;
        Undo.RegisterCreatedObjectUndo(go, "Create Test Mass");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void SpawnPrimitive(PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.position = Vector3.zero;
        go.AddComponent<Rigidbody>();
        go.AddComponent<EnvironmentSurface>().materialID = "Rubber";
        Undo.RegisterCreatedObjectUndo(go, $"Spawn {type}");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private List<string> GetMaterialChoices()
    {
        var choices = new List<string> { "Generic" };
        string path = Path.Combine(Application.streamingAssetsPath, "physics_config.json");
        if (File.Exists(path))
        {
            try
            {
                var config = JsonUtility.FromJson<PhysicsConfig>(File.ReadAllText(path));
                if (config?.materials?.Count > 0)
                {
                    choices.Clear();
                    foreach (var mat in config.materials) choices.Add(mat.id);
                }
            }
            catch { }
        }
        return choices;
    }

    // ── MODELS TAB ────────────────────────────────────────────────────────────

    private List<Area> BuildModels()
    {
        var areas = new List<Area>
        {
            new Area
            {
                Title    = "Import Models",
                Sections = new List<Section> { new Section { Subsections = new List<Subsection> { new Subsection { CustomRender = RenderModelUploader } } } }
            }
        };

        string baseFolder = "Assets/Models";
        if (!AssetDatabase.IsValidFolder(baseFolder)) { AssetDatabase.CreateFolder("Assets", "Models"); return areas; }

        foreach (string folderPath in AssetDatabase.GetSubFolders(baseFolder))
        {
            string folderName = Path.GetFileName(folderPath);
            areas.Add(new Area
            {
                Title = folderName,
                Sections = new List<Section>
                {
                    new Section
                    {
                        Title = $"{folderName} Library",
                        Subsections = new List<Subsection> { new Subsection { CustomRender = c => RenderModelFolderGrid(c, folderPath) } }
                    }
                }
            });
        }

        return areas;
    }

    private void RenderModelFolderGrid(VisualElement container, string targetFolder)
    {
        string[] guids = AssetDatabase.FindAssets("", new[] { targetFolder });
        var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
        int found = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetDirectoryName(path).Replace("\\", "/") != targetFolder) continue;
            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".prefab" && ext != ".obj" && ext != ".fbx" && ext != ".gltf" && ext != ".glb") continue;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            found++;

            var card = new VisualElement();
            card.style.width  = 110;
            card.style.marginRight = card.style.marginBottom = 10;
            card.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopColor = card.style.borderBottomColor = card.style.borderLeftColor = card.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 8;
            card.style.alignItems = Align.Center;
            card.style.paddingTop = card.style.paddingBottom = card.style.paddingLeft = card.style.paddingRight = 5;

            var img = new Image { style = { width = 90, height = 90, marginBottom = 5 } };
            img.schedule.Execute(() =>
            {
                var tex = AssetPreview.GetAssetPreview(prefab);
                img.image = tex != null ? tex
                    : !AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()) ? AssetPreview.GetMiniThumbnail(prefab)
                    : img.image;
            }).Every(100);

            var nameLbl = new Label(prefab.name);
            nameLbl.style.fontSize = 10;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.marginBottom = 5;
            nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            nameLbl.style.overflow = Overflow.Hidden;
            nameLbl.style.textOverflow = TextOverflow.Ellipsis;
            nameLbl.style.whiteSpace = WhiteSpace.NoWrap;
            nameLbl.style.width = 100;

            var spawnBtn = new Button(() => SpawnCustomModel(prefab)) { text = "Spawn" };
            spawnBtn.style.width = 90;
            spawnBtn.style.paddingTop = spawnBtn.style.paddingBottom = spawnBtn.style.paddingLeft = spawnBtn.style.paddingRight = 5;
            spawnBtn.AddToClassList("tool-button");

            card.Add(img); card.Add(nameLbl); card.Add(spawnBtn);
            grid.Add(card);
        }

        if (found == 0)
            container.Add(new Label("No models in this folder yet.") { style = { unityFontStyleAndWeight = FontStyle.Italic, color = new Color(0.6f, 0.6f, 0.6f) } });
        else
            container.Add(grid);
    }

    private void SpawnCustomModel(GameObject prefab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go == null) return;
        go.transform.position = Vector3.zero;
        if (go.GetComponent<Collider>() == null) go.AddComponent<MeshCollider>().convex = true;
        var fbd  = go.GetComponent<FreeBodyDiagram>() ?? go.AddComponent<FreeBodyDiagram>();
        var mass = go.GetComponent<Mass>();
        if (mass != null) mass.fbd = fbd;
        if (go.GetComponent<EnvironmentSurface>() == null) go.AddComponent<EnvironmentSurface>();
        Undo.RegisterCreatedObjectUndo(go, $"Spawn {prefab.name}");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private void RenderModelUploader(VisualElement container)
    {
        var browseBtn = new Button(BrowseForModel) { text = "Browse for Model (.obj, .fbx, .gltf, .glb)" };
        browseBtn.AddToClassList("tool-button");

        var dropZone = new VisualElement();
        dropZone.style.height          = 70;
        dropZone.style.backgroundColor = new Color(0, 0, 0, 0.2f);
        dropZone.style.borderTopWidth  = dropZone.style.borderBottomWidth = dropZone.style.borderLeftWidth = dropZone.style.borderRightWidth = 2;
        dropZone.style.borderTopColor  = dropZone.style.borderBottomColor = dropZone.style.borderLeftColor = dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        dropZone.style.borderTopLeftRadius = dropZone.style.borderTopRightRadius = dropZone.style.borderBottomLeftRadius = dropZone.style.borderBottomRightRadius = 8;
        dropZone.style.justifyContent  = Justify.Center;
        dropZone.style.alignItems      = Align.Center;
        dropZone.style.marginTop       = 10;
        dropZone.Add(new Label("Drag & Drop 3D Models Here") { style = { color = new Color(0.6f, 0.6f, 0.6f), unityFontStyleAndWeight = FontStyle.Bold } });

        dropZone.RegisterCallback<DragUpdatedEvent>(evt  => { DragAndDrop.visualMode = DragAndDropVisualMode.Copy; evt.StopPropagation(); });
        dropZone.RegisterCallback<DragPerformEvent>(evt  => { DragAndDrop.AcceptDrag(); foreach (var p in DragAndDrop.paths) ImportExternalModel(p); evt.StopPropagation(); });

        container.Add(browseBtn);
        container.Add(dropZone);
    }

    private void BrowseForModel()
    {
        string path = EditorUtility.OpenFilePanel("Import 3D Model", "", "obj,fbx,prefab,gltf,glb");
        if (!string.IsNullOrEmpty(path)) ImportExternalModel(path);
    }

    private void ImportExternalModel(string sourcePath)
    {
        string targetFolder = "Assets/Models/Custom";
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            Directory.CreateDirectory(Application.dataPath + "/Models/Custom");
            AssetDatabase.Refresh();
        }
        string fileName   = Path.GetFileName(sourcePath);
        string targetPath = targetFolder + "/" + fileName;
        FileUtil.CopyFileOrDirectory(sourcePath, targetPath);
        AssetDatabase.ImportAsset(targetPath);
        AssetDatabase.Refresh();
        OnHierarchyChange();
    }

    // ── UTILITIES ─────────────────────────────────────────────────────────────

    private static void Separator(VisualElement container)
    {
        var sep = new VisualElement();
        sep.style.height          = 1;
        sep.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        sep.style.marginTop       = sep.style.marginBottom = 8;
        container.Add(sep);
    }
}

public static class UIExtensions
{
    public static VisualElement ApplyClass(this VisualElement v, string className)
    {
        v.AddToClassList(className);
        return v;
    }
}
#endif
