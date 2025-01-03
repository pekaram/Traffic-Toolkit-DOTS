using Unity.Entities;
using Unity.Mathematics;

public struct Lane : IComponentData
{
    public float Width;
    public Entity LaneEntity;
}

public struct Waypoint : IBufferElementData
{
    public float3 Position;
}

public struct LaneConnection : IBufferElementData
{
    public Entity ConnectedLane; 
}
