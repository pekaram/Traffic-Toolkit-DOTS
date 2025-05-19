using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Mathematics;

public class SegmentAuthoring : MonoBehaviour
{
    public Vector3 Start;

    [HideInInspector]
    public Vector3 StartTangent;

    [HideInInspector]
    public Vector3 EndTangent;

    public Vector3 End;

    public TrafficLightAuthoring AssociatedTrafficLight;

    public List<SegmentAuthoringConnection> ConnectedSegments = new List<SegmentAuthoringConnection>();

    class Baker : Baker<SegmentAuthoring>
    { 
        public override void Bake(SegmentAuthoring authoring)
        {
            var trafficLightEntity = Entity.Null;
            if (authoring.AssociatedTrafficLight != null)
            {
                trafficLightEntity = GetEntity(authoring.AssociatedTrafficLight, TransformUsageFlags.None);
            }

            var entity = GetEntity(TransformUsageFlags.WorldSpace);
            AddComponent(entity, new Segment
            {
                Start = TransformPoint(authoring, authoring.Start),
                StartTangent = TransformPoint(authoring, authoring.StartTangent),
                EndTangent = TransformPoint(authoring, authoring.EndTangent),
                End = TransformPoint(authoring, authoring.End),
                AssociatedTrafficLight = trafficLightEntity
            });

            var connections = AddBuffer<SegmentConnection>(entity);
            foreach (var connection in authoring.ConnectedSegments)
            {
                if (connection.EndPoint == null)
                    continue;

                var connectionEntity = CreateAdditionalEntity(TransformUsageFlags.WorldSpace);
                AddComponent(connectionEntity, new Segment
                {
                    Start = TransformPoint(authoring, authoring.End),
                    StartTangent = TransformPoint(authoring, connection.StartTangent),
                    EndTangent = TransformPoint(connection.EndPoint, connection.EndTangent),
                    End = TransformPoint(connection.EndPoint, connection.EndPoint.Start)
                });
                var connectedSegment = AddBuffer<SegmentConnection>(connectionEntity);
                var connectedSgementEntity = GetEntity(connection.EndPoint, TransformUsageFlags.None);
                connectedSegment.Add(new SegmentConnection { ConnectedSegment = connectedSgementEntity });

                connections.Add(new SegmentConnection { ConnectedSegment = connectionEntity });
            }
        }

        private float3 TransformPoint(SegmentAuthoring segment, Vector3 localPosition)
        {
            return segment.transform.TransformPoint(localPosition);
        }
    }
}

[System.Serializable]
public class SegmentAuthoringConnection
{
    [HideInInspector]
    public Vector3 StartTangent;
    [HideInInspector]
    public Vector3 EndTangent;

    public SegmentAuthoring EndPoint;
}
