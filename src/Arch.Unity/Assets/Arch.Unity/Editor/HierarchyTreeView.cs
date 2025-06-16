using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Arch.Core;

namespace Arch.Unity.Editor
{
    public sealed class HierarchyTreeView : TreeView, IDisposable
    {
        public enum ItemType
        {
            World,
            Entity
        }

        public sealed class Item : TreeViewItem
        {
            Item() { }

            static readonly Stack<Item> pool = new();
            public static Item GetOrCreate()
            {
                if (!pool.TryPop(out var item)) item = new();
                return item;
            }

            public static void Return(Item item)
            {
                item.parent = null;
                item.children?.Clear();
                pool.Push(item);
            }

            public ItemType itemType;

            public Entity entityReference;
        }

        // Component filtering state
        bool _filteringEnabled;
        List<Type> _selectedComponentTypes = new List<Type>();

        public HierarchyTreeView(TreeViewState state) : base(state)
        {

        }

        public void SetComponentFilter(bool enabled, List<Type> selectedComponentTypes)
        {
            _filteringEnabled = enabled;
            _selectedComponentTypes.Clear();
            if (selectedComponentTypes != null && selectedComponentTypes.Count > 0)
            {
                _selectedComponentTypes.AddRange(selectedComponentTypes);
            }
            Reload();
        }

        public void SetWorld(World world)
        {
            var changed = TargetWorld != world;

            TargetWorld = world;
            Reload();

            if (changed)
            {
                SetExpanded(-2, true);
                SetExpanded(-1, true);
            }
        }

        World TargetWorld { get; set; }

        EntitySelectionProxy currentSelection;
        Item root;
        readonly List<Item> items = new();

        protected override TreeViewItem BuildRoot()
        {
            foreach (var item in items) Item.Return(item);
            items.Clear();

            root = Item.GetOrCreate();
            root.id = -2;
            root.depth = -1;
            root.displayName = "Root";
            items.Add(root);

            var hierarchyRoot = Item.GetOrCreate();
            hierarchyRoot.id = -1;
            hierarchyRoot.depth = 0;
            hierarchyRoot.displayName = $"World {TargetWorld.Id}";
            hierarchyRoot.itemType = ItemType.World;
            items.Add(hierarchyRoot);
            root.AddChild(hierarchyRoot);

            foreach (var chunk in TargetWorld.Query(new QueryDescription()))
            {
                for (int i = 0; i < chunk.Entities.Length; i++)
                {
                    if (TargetWorld.IsAlive(chunk.Entities[i]))
                    {
                        var entity = chunk.Entities[i];

                        // Apply filtering based on component types
                        if (_filteringEnabled && _selectedComponentTypes.Count > 0)
                        {
                            if (!EntityMatchesFilter(entity))
                            {
                                continue;
                            }
                        }

                        hierarchyRoot.AddChild(CreateItem(entity));
                    }
                }
            }
            return root;
        }

        bool EntityMatchesFilter(Entity entity)
        {
            if (!_filteringEnabled || _selectedComponentTypes == null || _selectedComponentTypes.Count == 0)
            {
                return true;
            }

            // Get all components for this entity
            var entityComponents = TargetWorld.GetAllComponents(entity);
            if (entityComponents == null || entityComponents.Length == 0)
            {
                return false;
            }

            // Get component types from the entity
            var entityComponentTypes = new HashSet<Type>();
            foreach (var component in entityComponents)
            {
                if (component != null)
                {
                    entityComponentTypes.Add(component.GetType());
                }
            }

            // Check if entity has all selected component types
            foreach (var selectedType in _selectedComponentTypes)
            {
                if (!entityComponentTypes.Contains(selectedType))
                {
                    return false;
                }
            }

            return true;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
        var item = (Item)args.item;
        var disabled = TargetWorld.IsAlive(item.entityReference);

        using (new EditorGUI.DisabledScope(disabled))
        {
            var iconImage = item.itemType == ItemType.World ? Styles.ModelImporterIcon.image : Styles.GameObjectIcon.image;
            var iconRect = args.rowRect;
            iconRect.x += GetContentIndent(args.item);
            iconRect.width = 16f;
            GUI.DrawTexture(iconRect, iconImage);

            extraSpaceBeforeIconAndLabel = iconRect.width + 2f;
            base.RowGUI(args);
        }
    }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);

            if (selectedIds.Count == 0) return;
            var item = (Item)FindItem(selectedIds[0], root);

            if (item.itemType == ItemType.World)
            {
                Selection.activeObject = null;
                return;
            }

            if (currentSelection == null) currentSelection = ScriptableObject.CreateInstance<EntitySelectionProxy>();

            currentSelection.world = TargetWorld;
            currentSelection.entityReference = item.entityReference;

            Selection.activeObject = currentSelection;
        }

        TreeViewItem CreateItem(Entity entity)
        {
            var reference = entity;
            var hasName = TargetWorld.TryGet(entity, out EntityName entityName);
            var item = Item.GetOrCreate();
            item.id = entity.Id;
            item.depth = 1;
            item.displayName = hasName ? entityName.ToString() : $"Entity({entity.Id}:{reference.Version})";
            item.itemType = ItemType.Entity;
            item.entityReference = reference;
            return item;
        }

        public void Dispose()
        {
            if (currentSelection != null) UnityEngine.Object.Destroy(currentSelection);
        }
    }
}
