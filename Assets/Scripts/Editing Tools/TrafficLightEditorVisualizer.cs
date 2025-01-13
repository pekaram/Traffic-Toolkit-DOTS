//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEditor;
//using UnityEngine;

//[CustomEditor(typeof(TrafficLightAuthoring))]
//public class TrafficLightEditorVisualizer : Editor
//{
//    private Entity cachedEntity = Entity.Null;  // Cached entity reference
//    private EntityManager entityManager;

//    private const float PositionMatchThreshold = 0.1f;  // Tolerance for position matching

//    private void OnEnable()
//    {
//        // Get the EntityManager once
//        if (Application.isPlaying)
//            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
//    }

//    private void OnSceneGUI()
//    {
//        var authoring = (TrafficLightAuthoring)target;
//        var authoringPosition = authoring.transform.position;

//        Handles.CubeHandleCap(0, authoringPosition + Vector3.up * 2, Quaternion.identity, 1, EventType.Repaint);

//        // Only fetch and cache the entity once
//        if (Application.isPlaying && cachedEntity == Entity.Null)
//        {
//            CacheTrafficLightEntity(authoringPosition);
//        }

//        // If entity is cached, get its state
//        if (cachedEntity == Entity.Null || !entityManager.Exists(cachedEntity))
//        {
//            cachedEntity = Entity.Null;
//            return;
//        }

//        var trafficLight = entityManager.GetComponentData<TrafficLight>(cachedEntity);
//        Handles.color = GetStateColor(trafficLight);

//    }

//    private Color GetStateColor(TrafficLight trafficLight)
//    {
//        return trafficLight.CurrentState switch
//        {
//            1 => Color.green,
//            0 => Color.red,
//            _ => Color.gray,
//        };
//    }

//    /// <summary>
//    /// Cache the matching traffic light entity by position
//    /// </summary>
//    private void CacheTrafficLightEntity(float3 authoringPosition)
//    {
//        var query = entityManager.CreateEntityQuery(typeof(TrafficLight), typeof(LocalTransform));
//        using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
//        {
//            foreach (var entity in entities)
//            {
//                var transform = entityManager.GetComponentData<LocalToWorld>(entity);
      
//                if (math.distance(transform.Position, authoringPosition) < PositionMatchThreshold)
//                {
//                    cachedEntity = entity;  // Cache the matched entity
//                    break;
//                }
//            }
//        }
//    }
//}