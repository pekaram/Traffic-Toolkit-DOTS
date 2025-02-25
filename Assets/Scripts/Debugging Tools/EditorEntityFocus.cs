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
        EntitySelection.SelectionChanged += HandleSelectionChange;
    }

    public static void Focus()
    {
        IsFocused = true;
    }

    private static void HandleSelectionChange(Entity entity)
    {
        IsFocused = false;
        SelectedEntity = entity;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (SelectedEntity == Entity.Null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

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
}
