using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEditor.PackageManager;
using UnityEngine;

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

    public float SpeedLimit;

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
                SpeedLimit = authoring.SpeedLimit,
                IsDeadEnd = !authoring.ConnectedSegments.Any(segment => segment.Type == ConnectionType.Intersection || segment.Type == ConnectionType.Join)
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
                    End = connection.WorldSegment.End,
                    SpeedLimit = connection.EndPoint.SpeedLimit,
                    IsDeadEnd = false
                });

                var startPoint = new ConnectionPoint { ConnectedSegmentEntity = connectionEntity, TransitionT = connection.fromT, ConnectedSegmentT = 0, Type = connection.Type };
                connectionPoints.Add(startPoint);

                var endpointConnection = AddBuffer<ConnectionPoint>(connectionEntity);
                var connectedSegmentEntity = GetEntity(connection.EndPoint, TransformUsageFlags.None);
                endpointConnection.Add(new ConnectionPoint { ConnectedSegmentEntity = connectedSegmentEntity, ConnectedSegmentT = connection.toT, TransitionT = 1, Type = ConnectionType.Intersection });         
            }
        }

        private void BakeConnectors(Entity segmentEntity, SegmentAuthoring authoring)
        {
            var connectors = AddBuffer<ConnectorElementData>(segmentEntity);
            foreach (var connection in authoring.ConnectedSegments.OrderBy(connection => connection.fromT))
            {
                if (connection.EndPoint == null)
                    continue;

                var connectorSegment = CreateAdditionalEntity(TransformUsageFlags.WorldSpace);
                AddComponent(connectorSegment, new Segment
                {
                    Start = connection.WorldSegment.Start,
                    StartTangent = connection.WorldSegment.StartTangent,
                    EndTangent = connection.WorldSegment.EndTangent,
                    End = connection.WorldSegment.End,
                    SpeedLimit = connection.EndPoint.SpeedLimit,
                    IsDeadEnd = false
                });
                AddComponent(connectorSegment, new Connector
                {
                    SegmentA = segmentEntity,
                    SegmentB = GetEntity(connection.EndPoint, TransformUsageFlags.None),
                    TransitionT = connection.fromT,
                    MergeT = connection.toT,
                    Type = connection.Type
                });


                connectors.Add(new ConnectorElementData { ConnectorSegmentEntity = connectorSegment });
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

    public ConnectionType Type;
}