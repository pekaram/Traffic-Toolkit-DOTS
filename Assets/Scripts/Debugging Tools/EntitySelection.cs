using UnityEngine;
using Unity.Entities;
using UnityEditor;
#if UNITY_EDITOR

[InitializeOnLoad]
public class EntitySelection
{
    public static Entity SelectedEntity;

    public static event System.Action<Entity> SelectionChanged;

    static EntitySelection()
    {
        SceneView.beforeSceneGui += OnSceneGUI;
        Selection.selectionChanged += HandleSelectionChange;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (SelectedEntity == Entity.Null)
            return;

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (entityManager.EntityCapacity <= SelectedEntity.Index || !entityManager.Exists(SelectedEntity))
        {
            SelectedEntity = Entity.Null;
            SelectionChanged?.Invoke(Entity.Null);
        }
    }

    private static void HandleSelectionChange()
    {
        SelectedEntity = GetSelectedEntity(Selection.activeObject);
        SelectionChanged?.Invoke(SelectedEntity);
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
#endif