using Unity.Entities;
using Unity.Mathematics;

public struct Vehicle : IComponentData
{
    public float Speed;
    public Entity Lane;
    public int WaypointIndex;
    public float3 WaypointPosition;
}
