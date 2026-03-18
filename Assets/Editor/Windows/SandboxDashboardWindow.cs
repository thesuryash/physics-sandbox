#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SandboxDashboardWindow : EditorWindow
{
    [MenuItem("Physics Sandbox/Dashboard")]
    public static void Open()
    {
        // This magic line tells Unity to dock it right next to the Inspector!
        Type inspectorType = Type.GetType("UnityEditor.InspectorWindow,UnityEditor.dll");
        var w = GetWindow<SandboxDashboardWindow>(new Type[] { inspectorType });

        // Shrunk the width from 900 to 350 so it fits nicely on the side
        w.minSize = new Vector2(350, 520);

        // Add a cool icon to the Window tab
        w.titleContent = new GUIContent("Dashboard", EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image);
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
        public Action<VisualElement> CustomRender;
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
    private Button _tabModules;
    private Button _tabModels;

    // Data
    private List<Area> _moduleAreas;
    private List<Area> _modelAreas;
    private bool _isModelTabActive = false;

    // Data
    private List<Area> _areas;

    private void OnHierarchyChange()
    {
        if (_moduleAreas == null || _areaList == null) return;

        int currentIndex = _areaList.selectedIndex;

        // Rebuild both lists
        _moduleAreas = BuildAreas();
        _modelAreas = BuildModels();

        // Assign the correct list based on the active tab
        _areaList.itemsSource = _isModelTabActive ? _modelAreas : _moduleAreas;
        _areaList.RefreshItems();

        if (currentIndex >= 0 && currentIndex < _areaList.itemsSource.Count)
        {
            _areaList.SetSelection(currentIndex);
            RenderArea((Area)_areaList.itemsSource[currentIndex]);
        }
    }

    public void CreateGUI()
    {
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/UXML/SandboxDashboardWindow.uxml");

        if (uxml == null)
        {
            rootVisualElement.Add(new Label("❌ Missing UXML at: Assets/Editor/UXML/SandboxDashboardWindow.uxml"));
            return;
        }

        var uiTree = uxml.CloneTree();
        uiTree.style.flexGrow = 1;
        rootVisualElement.Add(uiTree);

        // Query the UI elements
        _areaList = rootVisualElement.Q<ListView>("areaList");
        _contentRoot = rootVisualElement.Q<VisualElement>("contentRoot");
        _contentHeader = rootVisualElement.Q<Label>("contentHeader");
        _tabModules = rootVisualElement.Q<Button>("tabModules");
        _tabModels = rootVisualElement.Q<Button>("tabModels");

        // SAFETY CHECK: Stop the script if anything is missing so it doesn't crash!
        if (_areaList == null || _contentRoot == null || _contentHeader == null || _tabModules == null || _tabModels == null)
        {
            Debug.LogError("❌ UI Elements missing! Check your UXML names.");
            return;
        }

        // Build data for both tabs
        _moduleAreas = BuildAreas();
        _modelAreas = BuildModels();

        // Bind the click events
        _tabModules.clicked += () => SwitchTab(false);
        _tabModels.clicked += () => SwitchTab(true);

        SetupAreaList();

        // Default to Modules tab on start (This automatically selects the first item!)
        SwitchTab(false);
    }

    //private void SetupAreaList()
    //{
    //    _areaList.makeItem = () =>
    //    {
    //        var row = new VisualElement();
    //        row.AddToClassList("nav-row");
    //        var label = new Label();
    //        label.AddToClassList("nav-row-label");
    //        row.Add(label);
    //        return row;
    //    };

    //    _areaList.bindItem = (element, index) =>
    //    {
    //        var label = element.Q<Label>();
    //        // Dynamically check which list to pull names from
    //        var currentList = _isModelTabActive ? _modelAreas : _moduleAreas;

    //        if (index >= 0 && index < currentList.Count)
    //            label.text = currentList[index].Title;
    //    };

    //    _areaList.selectionType = SelectionType.Single;
    //    _areaList.selectionChanged += selection =>
    //    {
    //        foreach (var item in selection)
    //        {
    //            if (item is Area a) RenderArea(a);
    //        }
    //    };
    //}

    private void SwitchTab(bool showModels)
    {
        _isModelTabActive = showModels;

        // Visual feedback (Darkens the unselected tab)
        _tabModules.style.backgroundColor = showModels ? new Color(0.3f, 0.3f, 0.3f) : new StyleColor(StyleKeyword.Initial);
        _tabModels.style.backgroundColor = showModels ? new StyleColor(StyleKeyword.Initial) : new Color(0.3f, 0.3f, 0.3f);

        // Swap the list data
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
            var label = new Label();
            label.style.paddingTop = 8;
            label.style.paddingBottom = 8;
            label.style.paddingLeft = 15;
            label.style.fontSize = 13;
            return label;
        };

        _areaList.bindItem = (element, index) =>
        {
            var label = (Label)element;
            // Dynamically check which list to pull names from
            var currentList = _isModelTabActive ? _modelAreas : _moduleAreas;

            if (index >= 0 && index < currentList.Count)
                label.text = currentList[index].Title;
        };

        _areaList.selectionType = SelectionType.Single;
        _areaList.selectionChanged += selection =>
        {
            foreach (var item in selection)
            {
                if (item is Area a) RenderArea(a);
            }
        };
    }


    private List<Area> BuildModels()
    {
        var modelAreas = new List<Area>();

        // 1. Keep the Uploader at the top
        modelAreas.Add(new Area
        {
            Title = "Import Models",
            Sections = new List<Section> { new Section { Subsections = new List<Subsection> { new Subsection { CustomRender = RenderModelUploader } } } }
        });

        string baseFolder = "Assets/Models";

        if (!AssetDatabase.IsValidFolder(baseFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Models");
            return modelAreas;
        }

        // 2. Magic: Turn every subfolder into a sidebar tab!
        string[] subfolders = AssetDatabase.GetSubFolders(baseFolder);

        foreach (string folderPath in subfolders)
        {
            string folderName = System.IO.Path.GetFileName(folderPath); // E.g., "Primitives" or "Custom"

            modelAreas.Add(new Area
            {
                Title = folderName,
                Sections = new List<Section>
                {
                    new Section
                    {
                        Title = $"{folderName} Library",
                        Subsections = new List<Subsection>
                        {
                            new Subsection
                            {
                                // Pass the specific folder path into our renderer!
                                CustomRender = (container) => RenderModelFolderGrid(container, folderPath)
                            }
                        }
                    }
                }
            });
        }

        return modelAreas;
    }

    private void RenderModelFolderGrid(VisualElement container, string targetFolder)
    {
        // FIX: Removed "t:GameObject" so Unity stops hiding your GLTF and OBJ files!
        string[] guids = AssetDatabase.FindAssets("", new[] { targetFolder });

        var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
        int validModelsFound = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // Only show models DIRECTLY in this folder (ignore sub-sub folders)
            if (System.IO.Path.GetDirectoryName(path).Replace("\\", "/") != targetFolder) continue;

            // FIX: Explicitly check for your 3D model extensions
            string ext = System.IO.Path.GetExtension(path).ToLower();
            if (ext != ".prefab" && ext != ".obj" && ext != ".fbx" && ext != ".gltf" && ext != ".glb") continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                validModelsFound++;

                var modelCard = new VisualElement();
                modelCard.style.width = 110;
                modelCard.style.marginRight = 10;
                modelCard.style.marginBottom = 15;
                modelCard.style.backgroundColor = new Color(0, 0, 0, 0.2f);
                modelCard.style.borderTopWidth = modelCard.style.borderBottomWidth = modelCard.style.borderLeftWidth = modelCard.style.borderRightWidth = 1;
                modelCard.style.borderTopColor = modelCard.style.borderBottomColor = modelCard.style.borderLeftColor = modelCard.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);

                modelCard.style.borderTopLeftRadius = 8;
                modelCard.style.borderTopRightRadius = 8;
                modelCard.style.borderBottomLeftRadius = 8;
                modelCard.style.borderBottomRightRadius = 8;

                modelCard.style.alignItems = Align.Center;

                modelCard.style.paddingTop = 5;
                modelCard.style.paddingBottom = 5;
                modelCard.style.paddingLeft = 5;
                modelCard.style.paddingRight = 5;

                var img = new Image();
                img.style.width = 90;
                img.style.height = 90;
                img.style.marginBottom = 5;

                img.schedule.Execute(() =>
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(prefab);
                    if (tex != null) img.image = tex;
                    else if (!AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID())) img.image = AssetPreview.GetMiniThumbnail(prefab);
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
                spawnBtn.style.paddingTop = 5;
                spawnBtn.style.paddingBottom = 5;
                spawnBtn.style.paddingLeft = 5;
                spawnBtn.style.paddingRight = 5;
                spawnBtn.AddToClassList("tool-button");

                modelCard.Add(img);
                modelCard.Add(nameLbl);
                modelCard.Add(spawnBtn);

                grid.Add(modelCard);
            }
        }

        // If we looped through the folder and found nothing valid, show the empty message
        if (validModelsFound == 0)
        {
            container.Add(new Label("No models in this folder yet.") { style = { unityFontStyleAndWeight = FontStyle.Italic, color = new Color(0.6f, 0.6f, 0.6f) } });
        }
        else
        {
            container.Add(grid);
        }
    }

    private void SpawnPrimitive(PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        go.transform.position = Vector3.zero;

        // Add default physics components
        go.AddComponent<Rigidbody>();
        var surface = go.AddComponent<EnvironmentSurface>();
        surface.materialID = "Rubber";

        Undo.RegisterCreatedObjectUndo(go, $"Spawn {type}");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    // ---------------------------
    // THE MAGIC GENERIC UI BUILDER
    // ---------------------------

    /// <summary>
    /// Automatically finds all instances of T in the scene, builds a UI card, and exposes all serialized variables.
    /// </summary>
    // ---------------------------
    // THE MAGIC GENERIC UI BUILDER
    // ---------------------------

    /// <summary>
    /// Automatically finds all instances of T in the scene, builds a collapsible UI card, and exposes all serialized variables.
    /// </summary>
    private void BuildAutoUIFor<T>(VisualElement container, string createBtnText, Action createAction, string iconStr = "⚙️", Action<VisualElement, T> appendCustomControls = null) where T : MonoBehaviour
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
                var placeholder = new Label($"No {typeof(T).Name} detected in scene.");
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
            card.style.marginBottom = 15;

            // 1. Create a container for the variables so we can hide/show it
            var contentContainer = new VisualElement();
            bool isExpanded = true; // Default state: open

            // 2. Build the Header Row
            var headerRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 10 } };

            // 3. Create the Clickable Left Side (Arrow + Name)
            var headerLeft = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            var toggleArrow = new Label("▼") { style = { fontSize = 10, marginRight = 8, color = new Color(0.7f, 0.7f, 0.7f) } };
            var titleLabel = new Label($"{iconStr} {instance.gameObject.name}") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } };

            headerLeft.Add(toggleArrow);
            headerLeft.Add(titleLabel);

            // 4. Add the Click Logic for Expanding/Collapsing
            headerLeft.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0) // Left click only
                {
                    isExpanded = !isExpanded;
                    toggleArrow.text = isExpanded ? "▼" : "▶";

                    // Hide or show the inner content container instantly
                    contentContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                    evt.StopPropagation(); // Prevent clicking from affecting other things
                }
            });

            var selectBtn = new Button(() => { Selection.activeGameObject = instance.gameObject; EditorGUIUtility.PingObject(instance.gameObject); }) { text = "Select" };
            selectBtn.AddToClassList("tool-button");
            selectBtn.style.marginBottom = 0; // Tweak alignment for the header

            headerRow.Add(headerLeft);
            headerRow.Add(selectBtn);
            card.Add(headerRow);

            // 5. Reflection Magic: Bind variables into the CONTENT CONTAINER, not the root card
            // 5. Reflection Magic: Bind variables into the CONTENT CONTAINER
            SerializedObject serializedObject = new SerializedObject(instance);
            SerializedProperty prop = serializedObject.GetIterator();

            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                // By default, don't enter children (this prevents Vector3s from breaking into 3 separate float boxes)
                enterChildren = false;

                if (prop.name == "m_Script") continue;

                // --- NEW: THE FOLDER UNLOCKER ---
                // If it sees your custom class, skip drawing the "folder" and tell the loop to dive inside!
                if (prop.type == "MassDefinition")
                {
                    enterChildren = true;
                    continue;
                }

                // Now that we are inside the folder, it will successfully find this!
                if (prop.name == "materialID")
                {
                    var choices = GetMaterialChoices();
                    string currentValue = prop.stringValue;

                    // CRASH FIX: Unity throws an error if the current string isn't in the dropdown list.
                    // If "Generic" (or any old value) isn't in your JSON, we safely add it to the list!
                    if (!choices.Contains(currentValue))
                    {
                        if (string.IsNullOrEmpty(currentValue) && choices.Count > 0)
                            currentValue = choices[0]; // Fallback to the first JSON item
                        else
                            choices.Add(currentValue); // Add "Generic" so it doesn't crash
                    }

                    var dropdown = new DropdownField(prop.displayName, choices, currentValue);
                    dropdown.BindProperty(prop);

                    dropdown.RegisterValueChangedCallback(evt => {
                        if (instance is Mass massInstance)
                        {
                            dropdown.schedule.Execute(() => massInstance.UpdatePhysicalProperties()).StartingIn(10);
                        }
                    });

                    contentContainer.Add(dropdown);
                    continue;
                }

                // --- NEW: THE VISUAL MODEL AUTO-PARENTER & RENAMER (CRASH FIX) ---
                if (prop.name == "visualModel")
                {
                    var objField = new ObjectField("Visual Model") { objectType = typeof(GameObject), value = prop.objectReferenceValue };

                    objField.RegisterValueChangedCallback(evt => {
                        if (evt.newValue != null && instance is Mass massInstance)
                        {
                            GameObject droppedMesh = (GameObject)evt.newValue;

                            // 1. If it's a file from the project window, spawn it into the scene
                            GameObject newVisual;
                            if (PrefabUtility.IsPartOfPrefabAsset(droppedMesh))
                                newVisual = (GameObject)PrefabUtility.InstantiatePrefab(droppedMesh);
                            else
                                newVisual = droppedMesh; // It's already in the scene


                            // 2. Add or fix perfectly shrink-wrapped MeshColliders
                            foreach (var filter in newVisual.GetComponentsInChildren<MeshFilter>())
                            {
                                var mc = filter.gameObject.GetComponent<MeshCollider>();
                                if (mc == null) mc = filter.gameObject.AddComponent<MeshCollider>();

                                // THE FIX: Tell the collider to use the actual 3D shape!
                                mc.sharedMesh = filter.sharedMesh;
                                mc.convex = true;
                            }

                            // 2. Automatically parent it UNDER the Mass object
                            newVisual.transform.SetParent(massInstance.transform);
                            newVisual.transform.localPosition = Vector3.zero;
                            newVisual.transform.localRotation = Quaternion.identity;

                            // 3. AUTO-RENAME THE ROOT OBJECT!
                            string cleanName = newVisual.name.Replace("(Clone)", "");
                            massInstance.gameObject.name = cleanName;

                            // 4. CRASH FIX: Assign directly to the script instead of the stale UI property
                            massInstance.visualModel = newVisual;
                            EditorUtility.SetDirty(massInstance); // Tell Unity to save this change

                            // 5. Force the Mass to recalculate its volume
                            massInstance.UpdatePhysicalProperties();
                        }
                    });



                    contentContainer.Add(objField);
                    continue;
                }
                // -------------------------------------------

                var propertyField = new PropertyField(prop);
                propertyField.BindProperty(prop);
                contentContainer.Add(propertyField);
            }

            // Append extra custom controls (like Play/Pause buttons) to the content container
            appendCustomControls?.Invoke(contentContainer, instance);

            // 6. Bind the card and add the collapsible container to it
            card.Bind(serializedObject);
            card.Add(contentContainer);
            container.Add(card);
        }

        if (createAction != null)
        {
            var addAnotherBtn = new Button(createAction) { text = "+ Add " + createBtnText };
            addAnotherBtn.AddToClassList("tool-button");
            container.Add(addAnotherBtn);
        }
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
                    Title = "Environment Manager",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Settings",
                            CustomRender = (container) => BuildAutoUIFor<EnvironmentManager>(container, "Create Manager", CreateEnvironmentManager, "🌍")
                        }
                    }
                },
                new Section
                {
                    Title = "Physics Surfaces",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Scene Surfaces",
                            // Pass null for CreateAction because we have specific buttons below it
                            CustomRender = (container) =>
                            {
                                BuildAutoUIFor<EnvironmentSurface>(container, null, null, "🧊");

                                var btnRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
                                btnRow.Add(new Button(CreatePhysicsFloor) { text = "+ Create Floor" }.ApplyClass("tool-button"));
                                btnRow.Add(new Button(CreateTestCube) { text = "+ Create Test Mass" }.ApplyClass("tool-button"));
                                container.Add(btnRow);
                            }
                        }
                    }
                },
                new Section
                {
                    Title = "Time Management",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Game Clock",
                            CustomRender = (container) =>
                            {
                                // We use our generic UI builder, but inject custom Play/Pause controls!
                                BuildAutoUIFor<Clock>(container, "Create Time Manager", CreateTimeManager, "⏱️", (card, clockInstance) =>
                                {
                                    var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 10 } };
                                    buttonRow.Add(new Button(clockInstance.Play) { text = "▶ Play" }.ApplyClass("tool-button"));
                                    buttonRow.Add(new Button(clockInstance.Pause) { text = "⏸ Pause" }.ApplyClass("tool-button"));
                                    card.Add(buttonRow);
                                });
                            }
                        }
                    }
                },
                new Section
                {
                    Title = "Physical Masses",
                    Subsections = new List<Subsection>
                    {
                        new Subsection
                        {
                            Title = "Scene Objects",
                            CustomRender = (container) =>
                            {
                                // We use our magic UI builder and inject a custom Apply button!
                                BuildAutoUIFor<Mass>(container, "Create Mass", CreateTestMass, "📦", (card, massInstance) =>
                                {
                                    var syncBtn = new Button(() => {
                                        massInstance.UpdatePhysicalProperties();
                                        Debug.Log($"[Mass] Synced properties for {massInstance.gameObject.name}");
                                    }) { text = "↻ Apply Physics Settings" };

                                    syncBtn.style.marginTop = 10;
                                    syncBtn.style.paddingTop = 5;
                                    syncBtn.style.paddingBottom = 5;
                                    syncBtn.style.backgroundColor = new Color(0.2f, 0.4f, 0.6f); // Give it a nice blue tint
                                    syncBtn.AddToClassList("tool-button");

                                    card.Add(syncBtn);
                                });
                            }
                        }
                    }
                },
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
                            Title = "Scene Trails",
                            // This uses our magic UI builder to generate a card for your ChargeTrail!
                            CustomRender = (container) => BuildAutoUIFor<ChargeTrail>(container, "Create Charge Trail", CreateOrSetupChargeTrail, "⚡")
                        }
                    }
                }
            }
        };

        // 3. Kinematics Area
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
                            Title = "Scene Ramps",
                            CustomRender = (container) => BuildAutoUIFor<InclinedPlane>(container, "Create Inclined Plane", CreateInclinedPlane, "📐")
                        }
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
            sectionCard.style.backgroundColor = new Color(0, 0, 0, 0.1f);
            sectionCard.style.borderTopWidth = 1;
            sectionCard.style.borderTopColor = new Color(0, 0, 0, 0.5f);

            // Safe Padding
            sectionCard.style.paddingTop = 15;
            sectionCard.style.paddingBottom = 15;
            sectionCard.style.paddingLeft = 15;
            sectionCard.style.paddingRight = 15;
            sectionCard.style.marginBottom = 20;

            // Safe Border Radius
            sectionCard.style.borderTopLeftRadius = 8;
            sectionCard.style.borderTopRightRadius = 8;
            sectionCard.style.borderBottomLeftRadius = 8;
            sectionCard.style.borderBottomRightRadius = 8;

            var sectionTitle = new Label(section.Title);
            sectionTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sectionTitle.style.fontSize = 15;
            sectionTitle.style.marginBottom = 15;
            sectionCard.Add(sectionTitle);

            foreach (var subsection in section.Subsections)
            {
                var subsectionBlock = new VisualElement();
                subsectionBlock.style.marginBottom = 15;

                if (!string.IsNullOrEmpty(subsection.Title))
                {
                    var subTitle = new Label(subsection.Title);
                    subTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                    subTitle.style.fontSize = 12;
                    subTitle.style.color = new Color(0.7f, 0.7f, 0.7f);
                    subTitle.style.marginBottom = 8;
                    subsectionBlock.Add(subTitle);
                }

                if (subsection.CustomRender != null)
                {
                    subsection.CustomRender.Invoke(subsectionBlock);
                }
                else if (subsection.Buttons != null && subsection.Buttons.Count > 0)
                {
                    var buttonRow = new VisualElement();
                    buttonRow.style.flexDirection = FlexDirection.Row;
                    buttonRow.style.flexWrap = Wrap.Wrap;

                    foreach (var b in subsection.Buttons)
                    {
                        var btn = new Button(() => b.OnClick?.Invoke()) { text = b.Label };
                        btn.style.paddingTop = 6;
                        btn.style.paddingBottom = 6;
                        btn.style.paddingLeft = 6;
                        btn.style.paddingRight = 6;
                        btn.style.marginRight = 5;
                        btn.style.marginBottom = 5;
                        buttonRow.Add(btn);
                    }
                    subsectionBlock.Add(buttonRow);
                }
                else
                {
                    var placeholder = new Label("—");
                    placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
                    placeholder.style.unityFontStyleAndWeight = FontStyle.Italic;
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

    private void CreateInclinedPlane()
    {
        var go = new GameObject("Inclined Plane");
        go.AddComponent<InclinedPlane>();
        Undo.RegisterCreatedObjectUndo(go, "Create Inclined Plane");
    }


    private void CreateOrSetupChargeTrail()
    {
        var go = GameObject.Find("Charge Trail");
        if (go == null) go = new GameObject("Charge Trail");

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
        // 1. Search for the prefab by name
        string[] guids = AssetDatabase.FindAssets("TimeSpeedPrefab t:Prefab");

        if (guids.Length == 0)
        {
            Debug.LogError("Dashboard: Could not find 'TimeSpeedPrefab' in the project!");
            return; // Exit out if the prefab is missing
        }

        // 2. Load the prefab from the project files
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject timeSpeedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        // 3. Instantiate it safely to keep the blue prefab link
        GameObject timeObj = (GameObject)PrefabUtility.InstantiatePrefab(timeSpeedPrefab);

        // 4. Register for Undo (Ctrl+Z) so you can easily undo the spawn
        Undo.RegisterCreatedObjectUndo(timeObj, "Create Time Manager");
        Selection.activeGameObject = timeObj;

        Debug.Log("Dashboard: TimeSpeedPrefab successfully spawned!");
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
        var surface = go.AddComponent<EnvironmentSurface>();
        surface.materialID = "Rubber";
        Undo.RegisterCreatedObjectUndo(go, "Create Test Cube");
    }

    private void CreateTestMass()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Test Mass";
        go.transform.position = new Vector3(0, 2, 0);

        // Add the FBD. Because of your [RequireComponent] tags, 
        // Unity automatically adds the Rigidbody and Mass for us!
        var fbdComp = go.AddComponent<FreeBodyDiagram>();
        var massComp = go.GetComponent<Mass>();

        // Link them together immediately so the UI populates
        massComp.fbd = fbdComp;

        Undo.RegisterCreatedObjectUndo(go, "Create Test Mass");
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
    }

    private List<string> GetMaterialChoices()
    {
        var choices = new List<string> { "Generic" }; // Fallback

        // Peek into the StreamingAssets folder without needing the EnvironmentManager to be awake
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, "physics_config.json");

        if (System.IO.File.Exists(path))
        {
            try
            {
                string json = System.IO.File.ReadAllText(path);
                var config = JsonUtility.FromJson<PhysicsConfig>(json);
                if (config != null && config.materials != null && config.materials.Count > 0)
                {
                    choices.Clear();
                    foreach (var mat in config.materials) choices.Add(mat.id);
                }
            }
            catch { /* If JSON is temporarily malformed, just use the fallback */ }
        }
        return choices;
    }

    // --- CUSTOM MODEL UI ---

    private void RenderModelUploader(VisualElement container)
    {
        // 1. The Browse Button
        var browseBtn = new Button(BrowseForModel) { text = "📁 Browse for Model (.obj, .fbx)" };
        browseBtn.AddToClassList("tool-button");

        // 2. The Drag & Drop Zone
        var dropZone = new VisualElement();
        dropZone.style.height = 70;
        dropZone.style.backgroundColor = new Color(0, 0, 0, 0.2f);
        dropZone.style.borderTopWidth = dropZone.style.borderBottomWidth = dropZone.style.borderLeftWidth = dropZone.style.borderRightWidth = 2;
        dropZone.style.borderTopColor = dropZone.style.borderBottomColor = dropZone.style.borderLeftColor = dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        dropZone.style.borderTopLeftRadius = dropZone.style.borderTopRightRadius = dropZone.style.borderBottomLeftRadius = dropZone.style.borderBottomRightRadius = 8;
        dropZone.style.justifyContent = Justify.Center;
        dropZone.style.alignItems = Align.Center;
        dropZone.style.marginTop = 10;

        var dropLabel = new Label("Drag & Drop 3D Models Here");
        dropLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        dropLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        dropZone.Add(dropLabel);

        // 3. Unity Drag & Drop API Events
        dropZone.RegisterCallback<DragUpdatedEvent>(evt => {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy; // Shows the "Copy" cursor icon
            evt.StopPropagation();
        });

        dropZone.RegisterCallback<DragPerformEvent>(evt => {
            DragAndDrop.AcceptDrag();
            foreach (var path in DragAndDrop.paths)
            {
                ImportExternalModel(path);
            }
            evt.StopPropagation();
        });

        container.Add(browseBtn);
        container.Add(dropZone);
    }

    private void BrowseForModel()
    {
        // Added gltf and glb to the allowed extensions!
        string path = EditorUtility.OpenFilePanel("Import 3D Model", "", "obj,fbx,prefab,gltf,glb");

        if (!string.IsNullOrEmpty(path))
        {
            ImportExternalModel(path);
        }
    }

    private void ImportExternalModel(string sourcePath)
    {
        string targetFolder = "Assets/Models/Custom";

        // Create the folder automatically if it doesn't exist
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            System.IO.Directory.CreateDirectory(Application.dataPath + "/Models/Custom");
            AssetDatabase.Refresh(); // Tell Unity we made a folder
        }

        string fileName = System.IO.Path.GetFileName(sourcePath);
        string targetPath = targetFolder + "/" + fileName;

        // Copy the file into the Unity project
        FileUtil.CopyFileOrDirectory(sourcePath, targetPath);

        // Force Unity to import the .obj/.fbx immediately
        AssetDatabase.ImportAsset(targetPath);
        AssetDatabase.Refresh();

        // Refresh our dashboard so the new model shows up in the Library!
        OnHierarchyChange();
    }

    private void RenderCustomModelLibrary(VisualElement container)
    {
        string targetFolder = "Assets/Models/Custom";

        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            container.Add(new Label("Upload a model to start your library.") { style = { unityFontStyleAndWeight = FontStyle.Italic, color = new Color(0.6f, 0.6f, 0.6f) } });
            return;
        }

        // Search the folder for any 3D models or prefabs
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { targetFolder });

        if (guids.Length == 0)
        {
            container.Add(new Label("Upload a model to start your library.") { style = { unityFontStyleAndWeight = FontStyle.Italic, color = new Color(0.6f, 0.6f, 0.6f) } });
            return;
        }

        // Create a grid layout container
        var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // 1. Build a Card for the Model
                var modelCard = new VisualElement();
                modelCard.style.width = 110;
                modelCard.style.marginRight = 10;
                modelCard.style.marginBottom = 15;
                modelCard.style.backgroundColor = new Color(0, 0, 0, 0.2f);
                modelCard.style.borderTopWidth = modelCard.style.borderBottomWidth = modelCard.style.borderLeftWidth = modelCard.style.borderRightWidth = 1;
                modelCard.style.borderTopColor = modelCard.style.borderBottomColor = modelCard.style.borderLeftColor = modelCard.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);

                // FIXED BORDER RADIUS HERE
                modelCard.style.borderTopLeftRadius = 8;
                modelCard.style.borderTopRightRadius = 8;
                modelCard.style.borderBottomLeftRadius = 8;
                modelCard.style.borderBottomRightRadius = 8;

                modelCard.style.alignItems = Align.Center;

                modelCard.style.paddingTop = 5;
                modelCard.style.paddingBottom = 5;
                modelCard.style.paddingLeft = 5;
                modelCard.style.paddingRight = 5;

                // 2. The 3D Preview Image Box
                var img = new Image();
                img.style.width = 90;
                img.style.height = 90;
                img.style.marginBottom = 5;

                img.schedule.Execute(() =>
                {
                    Texture2D tex = AssetPreview.GetAssetPreview(prefab);
                    if (tex != null)
                    {
                        img.image = tex;
                    }
                    else if (!AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
                    {
                        img.image = AssetPreview.GetMiniThumbnail(prefab);
                    }
                }).Every(100);

                // 3. The Model Name Label
                var nameLbl = new Label(prefab.name);
                nameLbl.style.fontSize = 10;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameLbl.style.marginBottom = 5;
                nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;

                nameLbl.style.overflow = Overflow.Hidden;
                nameLbl.style.textOverflow = TextOverflow.Ellipsis;
                nameLbl.style.whiteSpace = WhiteSpace.NoWrap;
                nameLbl.style.width = 100;

                // 4. The Spawn Button
                var spawnBtn = new Button(() => SpawnCustomModel(prefab)) { text = "Spawn" };
                spawnBtn.style.width = 90;

                spawnBtn.style.paddingTop = 5;
                spawnBtn.style.paddingBottom = 5;
                spawnBtn.style.paddingLeft = 5;
                spawnBtn.style.paddingRight = 5;

                spawnBtn.AddToClassList("tool-button");

                // Assemble the card
                modelCard.Add(img);
                modelCard.Add(nameLbl);
                modelCard.Add(spawnBtn);

                grid.Add(modelCard);
            }
        }

        container.Add(grid);
    }

    private void SpawnCustomModel(GameObject prefab)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go != null)
        {
            go.transform.position = Vector3.zero;

            // Automatically add physics to their uploaded model
            if (go.GetComponent<Collider>() == null) go.AddComponent<MeshCollider>().convex = true;

            // Auto-add the FBD (and Mass/Rigidbody)
            var fbdComp = go.GetComponent<FreeBodyDiagram>();
            if (fbdComp == null) fbdComp = go.AddComponent<FreeBodyDiagram>();

            var massComp = go.GetComponent<Mass>();
            if (massComp != null) massComp.fbd = fbdComp; // Link it!

            if (go.GetComponent<EnvironmentSurface>() == null) go.AddComponent<EnvironmentSurface>();

            Undo.RegisterCreatedObjectUndo(go, $"Spawn {prefab.name}");
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }
    }
}



// Extension to keep the inline styling clean
public static class UIExtensions
{
    public static VisualElement ApplyClass(this VisualElement v, string className)
    {
        v.AddToClassList(className);
        return v;
    }
}
#endif