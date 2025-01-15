using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System.Linq;

public class LaneAuthoring : MonoBehaviour
{
    public List<Vector3> Waypoints;
    public List<LaneAuthoring> ConnectedLanes;
}

public class LaneBaker : Baker<LaneAuthoring>
{
    public override void Bake(LaneAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        var TrafficLight = Object.FindObjectsByType<TrafficLightAuthoring>(FindObjectsSortMode.None).Where(p => p.Lane == authoring).FirstOrDefault();
        var trafficLightEntity = Entity.Null;
        if (TrafficLight != null)
        {
            trafficLightEntity = GetEntity(TrafficLight, TransformUsageFlags.None);
        }

        AddComponent(entity, new Lane
        {
            Width = 0,
            AssociatedTrafficLight = trafficLightEntity
        });

        var waypointsBuffer = AddBuffer<Waypoint>(entity);
        foreach(var waypoint in authoring.Waypoints)
        {
            waypointsBuffer.Add(new Waypoint { Position = waypoint });
        }

        var lanesConnections = AddBuffer<LaneConnection>(entity);
        foreach (var lane in authoring.ConnectedLanes)
        {
            var connectedLaneEntity = GetEntity(lane, TransformUsageFlags.None);
            lanesConnections.Add(new LaneConnection { ConnectedLane = connectedLaneEntity });
        }
    }
}