#nullable disable
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Arch.Core;

namespace Arch.Unity.Editor
{
    public class ArchHierarchyWindow : EditorWindow
    {
        [MenuItem("Window/Arch/Arch Hierarchy")]
        public static ArchHierarchyWindow Open()
        {
            var window = GetWindow<ArchHierarchyWindow>(false, "Arch Hierarchy", true);
            window.titleContent.image = EditorGUIUtility.IconContent("UnityEditor.HierarchyWindow").image;
            window.Show();
            return window;
        }

        int selectedWorldId;
        HierarchyTreeView treeView;
        TreeViewState<int> treeViewState;

        // Component filtering state
        bool _filteringEnabled = false; // Explicitly disabled by default
        List<Type> _availableComponentTypes = new List<Type>();
        List<Type> _selectedComponentTypes = new List<Type>();
        Vector2 _componentFilterScrollPos;
        bool _needComponentRefresh = false;

        void OnEnable()
        {
            treeViewState = new TreeViewState<int>();
            treeView = new HierarchyTreeView(treeViewState);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        void OnDisable()
        {
            treeView.Dispose();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            treeView.SetSelection(Array.Empty<int>());
            _needComponentRefresh = true;
            Repaint();
        }

        void OnEditorUpdate()
        {
            // Check if we need to refresh components
            if (_needComponentRefresh && _filteringEnabled)
            {
                var worlds = World.Worlds.Where(x => x != null).ToDictionary(x => x.Id, x => x);
                if (worlds.ContainsKey(selectedWorldId))
                {
                    RefreshComponentTypes(worlds[selectedWorldId]);
                    _needComponentRefresh = false;
                    Repaint();
                }
            }
        }

        void OnGUI()
        {
            var worlds = World.Worlds.Where(x => x != null).ToDictionary(x => x.Id, x => x);
            var worldSize = worlds.Count;
            var keys = worlds.Keys.ToArray();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (worldSize == 0)
                {
                    GUILayout.Button("No World", EditorStyles.toolbarPopup, GUILayout.Width(100f));
                }
                else
                {
                    var displayedOptions = worlds.Select(x => $"World {x.Value.Id}").ToArray();
                    var id = EditorGUILayout.IntPopup(selectedWorldId, displayedOptions, keys, EditorStyles.toolbarPopup, GUILayout.Width(100f));
                    if (id != selectedWorldId)
                    {
                        treeView.SetSelection(Array.Empty<int>());
                        selectedWorldId = id;
                    }
                }

                // Add filter toggle button
                GUILayout.Space(5);
                var prevFilterState = _filteringEnabled;
                _filteringEnabled = GUILayout.Toggle(_filteringEnabled, "Filter", EditorStyles.toolbarButton);
                if (_filteringEnabled != prevFilterState && _filteringEnabled && worlds.ContainsKey(selectedWorldId))
                {
                    // When enabling filter, refresh component types
                    RefreshComponentTypes(worlds[selectedWorldId]);
                }

                GUILayout.FlexibleSpace();
            }

            if (worlds.Count == 0) return;
            if (!worlds.ContainsKey(selectedWorldId))
            {
                selectedWorldId = keys.First();
            }

            var currentWorld = worlds[selectedWorldId];

            // Draw component filter panel if filtering is enabled
            if (_filteringEnabled)
            {
                DrawComponentFilterPanel();
            }

            // Pass filter settings to tree view
            treeView.SetWorld(currentWorld);
            treeView.SetComponentFilter(_filteringEnabled, _selectedComponentTypes);

            var treeViewRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeView.OnGUI(treeViewRect);
        }

        void DrawComponentFilterPanel()
        {
            // Draw a box with all available component types as toggles
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filter by Components", EditorStyles.boldLabel);

            if (_availableComponentTypes.Count == 0)
            {
                EditorGUILayout.LabelField("No components found in current world");
            }
            else
            {
                _componentFilterScrollPos = EditorGUILayout.BeginScrollView(_componentFilterScrollPos, GUILayout.MaxHeight(150));

                foreach (var componentType in _availableComponentTypes)
                {
                    // Clean up the component type name by removing [] suffix
                    var typeName = componentType.Name;
                    if (typeName.EndsWith("[]"))
                    {
                        typeName = typeName.Substring(0, typeName.Length - 2);
                    }
                    var displayName = ObjectNames.NicifyVariableName(typeName);

                    var isSelected = _selectedComponentTypes.Contains(componentType);
                    var newIsSelected = EditorGUILayout.ToggleLeft(displayName, isSelected);

                    if (newIsSelected != isSelected)
                    {
                        if (newIsSelected)
                        {
                            _selectedComponentTypes.Add(componentType);
                        }
                        else
                        {
                            _selectedComponentTypes.Remove(componentType);
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }

        void RefreshComponentTypes(World world)
        {
            _availableComponentTypes.Clear();
            var uniqueComponentTypes = new HashSet<Type>();

            // Get all entities in the world and check their components
            foreach (var chunk in world.Query(new QueryDescription()))
            {
                for (int i = 0; i < chunk.Entities.Length; i++)
                {
                    var entity = chunk.Entities[i];
                    if (!world.IsAlive(entity)) {
                        continue;
                    }

                    // Get components for this specific entity
                    var components = world.GetAllComponents(entity);
                    foreach (var component in components)
                    {
                        if (component != null && !uniqueComponentTypes.Contains(component.GetType()))
                        {
                            uniqueComponentTypes.Add(component.GetType());
                        }
                    }
                }
            }

            _availableComponentTypes.AddRange(uniqueComponentTypes.OrderBy(t => t.Name));

            // If we still didn't find any components, set a flag to try again later
            if (_availableComponentTypes.Count == 0)
            {
                _needComponentRefresh = true;
            }
        }
    }
}
