using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class SegmentAuthoring : MonoBehaviour
{
    // Bezier p0,p1,p2,p3
    public Vector3 Start;
    public Vector3 StartTangent;
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
                Start = authoring.Start,
                StartTangent = authoring.StartTangent,
                EndTangent = authoring.EndTangent,
                End = authoring.End,
                AssociatedTrafficLight = trafficLightEntity
            });

            var connections = AddBuffer<SegmentConnection>(entity);
            foreach (var connection in authoring.ConnectedSegments)
            {
                var connectionEntity = CreateAdditionalEntity(TransformUsageFlags.WorldSpace);
                AddComponent(connectionEntity, new Segment
                {
                    Start = authoring.End,
                    StartTangent = connection.StartTangent,
                    EndTangent = connection.EndTangent,
                    End = connection.EndPoint.Start
                });
                var connectedSegment = AddBuffer<SegmentConnection>(connectionEntity);
                var connectedSgementEntity = GetEntity(connection.EndPoint, TransformUsageFlags.None);
                connectedSegment.Add(new SegmentConnection { ConnectedSegment = connectedSgementEntity });

                connections.Add(new SegmentConnection { ConnectedSegment = connectionEntity });
            }
        }
    }
}

[System.Serializable]
public class SegmentAuthoringConnection
{
    public Vector3 StartTangent;
    public Vector3 EndTangent;

    public SegmentAuthoring EndPoint;
}
