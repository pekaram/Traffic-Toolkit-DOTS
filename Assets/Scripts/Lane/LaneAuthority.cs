using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;

public class LaneAuthoring : MonoBehaviour
{
    public List<Vector3> Waypoints;
    public List<LaneAuthoring> ConnectedLanes;
    public TrafficLightAuthoring TrafficLight { get; set; }
}

public class LaneBaker : Baker<LaneAuthoring>
{
    public override void Bake(LaneAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        var trafficLightEntity = Entity.Null;
        if (authoring.TrafficLight != null)
        {
            trafficLightEntity = GetEntity(authoring.TrafficLight, TransformUsageFlags.None);
        }

        AddComponent(entity, new Lane
        {
            Width = 0,
            AssociatedTrafficLight = trafficLightEntity
        });

        var waypointsBuffer = AddBuffer<Waypoint>(entity);
        foreach(var waypoint in authoring.Waypoints)
        {
            waypointsBuffer.Add(new Waypoint { Position = authoring.transform.TransformPoint(waypoint) });
        }

        var lanesConnections = AddBuffer<LaneConnection>(entity);
        foreach (var lane in authoring.ConnectedLanes)
        {
            var connectedLaneEntity = GetEntity(lane, TransformUsageFlags.None);
            lanesConnections.Add(new LaneConnection { ConnectedLane = connectedLaneEntity });
        }
    }
}