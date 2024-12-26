using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public struct Lane : IComponentData
{
    public float Width;
    public Entity LaneEntity;
}

public struct Waypoint : IBufferElementData
{
    public float3 Position;
}