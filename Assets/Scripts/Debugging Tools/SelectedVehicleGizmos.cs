using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
[InitializeOnLoad]
public static class SelectedVehicleGizmos
{
    private static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

    private static Entity SelectedVehicleEntity;

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
        if (SelectedVehicleEntity == Entity.Null || !EntityManager.Exists(SelectedVehicleEntity))
            return;

        var vehicle = EntityManager.GetComponentData<Vehicle>(SelectedVehicleEntity);
        var transform = EntityManager.GetComponentData<LocalToWorld>(SelectedVehicleEntity);

        if (vehicle.CurrentSegment != Entity.Null)
        {
            var segment = EntityManager.GetComponentData<Segment>(vehicle.CurrentSegment);
            DrawSegment(segment);
        }

        // TODO: [MTS-41] Collision Debugging Gizmos should get fed data from collision avoidance system
        var collider = EntityManager.GetComponentData<PhysicsCollider>(SelectedVehicleEntity);
        var boundingBox = collider.Value.Value.CalculateAabb();
        DrawRaycastLine(boundingBox, transform.Position, transform.Forward, SpeedControlSystem.CriticalGap);
        DrawBoundingBox(boundingBox, transform.Position, transform.Rotation);
    }

    private static void DrawSegment(Segment segment)
    {
        Handles.color = Color.green;
        Handles.DrawBezier(segment.Start, segment.End, segment.StartTangent, segment.EndTangent, Color.green, null, 3f);
        Handles.SphereHandleCap(0, segment.End, Quaternion.identity, 0.5f, EventType.Repaint);
    }

    private static void DrawRaycastLine(Aabb boundingBox, float3 position, float3 rayDirection, float hitDistance)
    {
        var colliderTip = position + boundingBox.Max * rayDirection;
        var hitPoint = colliderTip + rayDirection * hitDistance;

        Handles.color = Color.red;
        Handles.DrawLine(colliderTip, hitPoint);

        Handles.color = Color.red;
        Handles.SphereHandleCap(0, hitPoint, Quaternion.identity, 0.5f, EventType.Repaint);
    }

    private static void DrawBoundingBox(Aabb boundingBox, float3 position, quaternion rotation)
    {
        Vector3 center = position + boundingBox.Center;
        Vector3 size = boundingBox.Extents;

        Matrix4x4 matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        using (new Handles.DrawingScope(Color.green, matrix))
        {
            Handles.DrawWireCube(Vector3.zero, size);
        }
    }
}
#endif