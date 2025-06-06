using UnityEditor;
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;
using System.Reflection;
using System;
#if UNITY_EDITOR

public class EntityIdHierarchy : EditorWindow
{
    private MethodInfo EntitySelectionProxy_SelectEntity;

    private World World => World.DefaultGameObjectInjectionWorld;

    private string _searchQuery = "";

    private int _cachedEntityOrderVersion = -1;

    private readonly Dictionary<string, Entity> _idsToEntities = new();

    private readonly List<(string, Entity)> _namesToEntities = new();

    private Vector2 _scrollPosition;

    private Entity? _selectedEntity = null;

    [MenuItem("Window/Entity ID Hierarchy")]
    public static void ShowWindow()
    {
        GetWindow<EntityIdHierarchy>("Entity ID Hierarchy");
    }

    private void OnEnable()
    {
        var type = Type.GetType("Unity.Entities.Editor.EntitySelectionProxy, Unity.Entities.Editor");
        var method = type.GetMethod("SelectEntity", BindingFlags.Public | BindingFlags.Static);
        EntitySelectionProxy_SelectEntity = method;

        Selection.selectionChanged += HandleDeselect;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= HandleDeselect;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Select Entity", EditorStyles.boldLabel);
        _searchQuery = EditorGUILayout.TextField("ID", _searchQuery);
        if (!string.IsNullOrEmpty(_searchQuery) && _idsToEntities.ContainsKey(_searchQuery))
        {
            SelectEntity(_idsToEntities[_searchQuery]);
        }

        DisplayList();
    }

    private void HandleDeselect()
    {     
        if (Selection.activeObject == null)
        {
            _selectedEntity = null;
            Repaint();
        }
    }

    private void DisplayList()
    {
        if (_cachedEntityOrderVersion != World.EntityManager.EntityOrderVersion)
        {
            Selection.activeObject = null;
            RefreshEntities();
        }

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        foreach (var nameToEntity in _namesToEntities)
        {
            var entity = nameToEntity.Item2;
            var isSelected = _selectedEntity.HasValue && _selectedEntity.Value == entity;
            var buttonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(20, 20, 0, 0)
            };

            if (isSelected)
            {
                buttonStyle.normal.textColor = Color.green;
                buttonStyle.hover.textColor = Color.green;
            }

            if (GUILayout.Button(nameToEntity.Item1, buttonStyle))
            {
                HandleEntityClicked(entity);
            }
        }

        EditorGUILayout.EndScrollView();
        HandleDeselect();
    }

    private void HandleEntityClicked(Entity entity)
    {
        if (entity == _selectedEntity)
        {
            EditorEntityFocus.Focus();
        }
        else
        {
            SelectEntity(entity);
        }

        Event.current.Use();
    }

    private void SelectEntity(Entity entity)
    {
        EntitySelectionProxy_SelectEntity.Invoke(null, new object[] { World, entity });
        _selectedEntity = entity;
    }

    private void RefreshEntities()
    {
        _idsToEntities.Clear();
        _namesToEntities.Clear();

        var entityManager = World.EntityManager;

        using (var entityArray = entityManager.GetAllEntities(Allocator.Temp))
        {
            foreach (var entity in entityArray)
            {
                if (!World.EntityManager.HasComponent<FixedEntityId>(entity))
                    continue;   

                var fixedEntityID = World.EntityManager.GetComponentData<FixedEntityId>(entity);
               
                _idsToEntities.Add(fixedEntityID.Id.ToString(), entity);
                _namesToEntities.Add((fixedEntityID.DebugName.ToString(), entity));
            }
        }

        _cachedEntityOrderVersion = entityManager.EntityOrderVersion;
    }
}
#endif