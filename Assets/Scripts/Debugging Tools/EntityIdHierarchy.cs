using UnityEditor;
using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;
using System.Reflection;
using System;

public class EntityIdHierarchy : EditorWindow
{
    private MethodInfo EntitySelectionProxy_SelectEntity;

    private World World => World.DefaultGameObjectInjectionWorld;

    private string _searchQuery = "";

    private int _cachedEntityOrderVersion = -1;

    private readonly Dictionary<string, Entity> _idsToEntities = new();

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

        foreach (var idToEntity in _idsToEntities)
        {
            var entity = idToEntity.Value;
            var isSelected = _selectedEntity.HasValue && _selectedEntity.Value == entity;
            var buttonStyle = new GUIStyle(EditorStyles.miniButton);
            if (isSelected)
            {
                buttonStyle.normal.textColor = Color.green;
                buttonStyle.hover.textColor = Color.green;
            }

            if (GUILayout.Button($"Entity: {idToEntity.Key}", buttonStyle))
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
        var entityManager = World.EntityManager;
        _idsToEntities.Clear();

        _cachedEntityOrderVersion = entityManager.EntityOrderVersion;

        using (var entityArray = entityManager.GetAllEntities(Allocator.Temp))
        {
            foreach (var entity in entityArray)
            {
                if (!World.EntityManager.HasComponent<FixedEntityId>(entity))
                    continue;

                var id = World.EntityManager.GetComponentData<FixedEntityId>(entity).Id;
                _idsToEntities.Add(id.ToString(), entity);
            }
        }

    }
}
