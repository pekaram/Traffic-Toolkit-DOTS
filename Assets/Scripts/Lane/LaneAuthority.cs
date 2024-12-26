using UnityEngine;
using Unity.Entities;

class LaneAuthoring : MonoBehaviour
{
    public Vector3[] Waypoints;
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
    }
}