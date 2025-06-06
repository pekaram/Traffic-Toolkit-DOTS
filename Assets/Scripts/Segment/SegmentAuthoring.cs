using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;

public class SegmentAuthoring : MonoBehaviour
{
    public Vector3 Start;

    [HideInInspector]
    public Vector3 StartTangent;

    [HideInInspector]
    public Vector3 EndTangent;

    public Vector3 End;

    [HideInInspector]
    public Vector3 WorldStart;

    [HideInInspector]
    public Vector3 WorldEnd;

    [HideInInspector]
    public Vector3 WorldStartTangent;

    [HideInInspector]
    public Vector3 WorldEndTangent;

    public float MaxSpeed;

    public TrafficLightAuthoring AssociatedTrafficLight;

    public List<SegmentAuthoringConnection> ConnectedSegments = new();

    class Baker : Baker<SegmentAuthoring>
    { 
        public override void Bake(SegmentAuthoring authoring)
        {
            var trafficLightEntity = Entity.Null;
            if (authoring.AssociatedTrafficLight != null)
            {
                trafficLightEntity = GetEntity(authoring.AssociatedTrafficLight, TransformUsageFlags.None);
            }

            var segmentEntity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponent(segmentEntity, new Segment
            {
                Start = authoring.WorldStart,
                StartTangent = authoring.WorldStartTangent,
                EndTangent = authoring.WorldEndTangent,
                End = authoring.WorldEnd,
                AssociatedTrafficLight = trafficLightEntity,
                MaxSpeed = authoring.MaxSpeed,
            });

            var connectionPoints = AddBuffer<ConnectionPoint>(segmentEntity);
            foreach (var connection in authoring.ConnectedSegments.OrderBy(connection => connection.fromT))
            {
                if (connection.EndPoint == null)
                    continue;

                var connectionEntity = CreateAdditionalEntity(TransformUsageFlags.WorldSpace);
                AddComponent(connectionEntity, new Segment
                {
                    Start = connection.WorldSegment.Start,
                    StartTangent = connection.WorldSegment.StartTangent,
                    EndTangent = connection.WorldSegment.EndTangent,
                    End = connection.WorldSegment.End
                });

                var startPoint = new ConnectionPoint { ConnectedSegmentEntity = connectionEntity, TransitionT = connection.fromT, ConnectedSegmentT = 0, Type = connection.Type };
                connectionPoints.Add(startPoint);

                var endpointConnection = AddBuffer<ConnectionPoint>(connectionEntity);
                var connectedSegmentEntity = GetEntity(connection.EndPoint, TransformUsageFlags.None);
                endpointConnection.Add(new ConnectionPoint { ConnectedSegmentEntity = connectedSegmentEntity, ConnectedSegmentT = connection.toT, TransitionT = 1, Type = 0 });         
            }
        }
    }
}

[System.Serializable]
public class SegmentAuthoringConnection
{
    public float fromT;

    public float toT;

    [HideInInspector]
    public Vector3 StartTangent;
 
    [HideInInspector]
    public Vector3 EndTangent;

    [HideInInspector]
    public Segment WorldSegment;

    public SegmentAuthoring EndPoint;

    public int Type; // 0 = Intersection, 1 = LeftAdjacent, 2 = RightAdacent
}