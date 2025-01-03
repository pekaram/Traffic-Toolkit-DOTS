using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

class LaneAuthoring : MonoBehaviour
{
    public List<Vector3> Waypoints;
    public List<LaneAuthoring> ConnectedLanes;
}

class LaneBaker : Baker<LaneAuthoring>
{
    public override void Bake(LaneAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Lane
        {
            Width = 0,
            LaneEntity = entity
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