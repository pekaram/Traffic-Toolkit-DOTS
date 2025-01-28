using Unity.Entities;
using Unity.Mathematics;

public struct Vehicle : IComponentData
{
    public float Speed;

    public Entity CurrentLane;
    public float3 WaypointPosition;
    public int WaypointIndex;
    public int RemainingWaypoints;

    public Entity NextLane;
}
