using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SelectedVehicleGizmos
{
    private static Entity SelectedVehicleEntity;

    private static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

    static SelectedVehicleGizmos()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        EntitySelection.SelectionChanged += HandleSelectionChange;
    }

    private static void HandleSelectionChange(Entity entity)
    {
        if (entity == Entity.Null || !EntityManager.HasComponent<Vehicle>(entity))
        {
            SelectedVehicleEntity = Entity.Null;
            return;
        }

        SelectedVehicleEntity = entity;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (SelectedVehicleEntity == Entity.Null)
            return;

        var vehicle = EntityManager.GetComponentData<Vehicle>(SelectedVehicleEntity);

        Handles.color = Color.green; 
        var vehiclePosition = EntityManager.GetComponentData<LocalTransform>(SelectedVehicleEntity).Position;
        var waypointPosition = vehicle.WaypointPosition;

        Handles.DrawDottedLine(vehiclePosition, waypointPosition, 5f);
        Handles.SphereHandleCap(0, waypointPosition, Quaternion.identity, 0.5f, EventType.Repaint);
    }
}
