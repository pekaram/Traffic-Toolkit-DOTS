using Unity.Entities;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorEntityFocus
{
    private static Entity SelectedEntity;

    private static bool IsFocused = false;

    static EditorEntityFocus()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        Selection.selectionChanged += HandleSelectionChange;
    }

    public static void Focus()
    {
        IsFocused = true;
    }

    private static void HandleSelectionChange()
    {
        IsFocused = false;
        SelectedEntity = GetSelectedEntity(Selection.activeObject);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (SelectedEntity == Entity.Null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (entityManager.EntityCapacity <= SelectedEntity.Index || !entityManager.Exists(SelectedEntity))
        {
            SelectedEntity = Entity.Null;
            return;
        }

        var e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F)
        {
            IsFocused = !IsFocused;
        }

        if (!entityManager.HasComponent<LocalToWorld>(SelectedEntity))
            return;

        var transform = entityManager.GetComponentData<LocalToWorld>(SelectedEntity);
        Handles.Label(transform.Position, $"Entity {SelectedEntity.Index}");

        if (!IsFocused)
            return;

        SceneView.lastActiveSceneView.pivot = transform.Position;
    }


    private static Entity GetSelectedEntity(Object selectedObject)
    {
        if (!selectedObject)
            return Entity.Null;

        // Unity uses EntitySelectionProxy internally for their Entity selections
        // Workaround using reflection to access data from Unity's EntitySelectionProxy.Entity 
        var entityProperty = selectedObject.GetType().GetProperty(nameof(Entity), typeof(Entity));
        if (entityProperty == null)
            return Entity.Null;

        var entity = (Entity)entityProperty.GetValue(selectedObject);
        return entity;
    }
}
