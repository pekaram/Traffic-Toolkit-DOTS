using Unity.Entities;
using Unity.Mathematics;

public struct Lane : IComponentData
{
    public float Width;
    public bool IsAvailable;
    public Entity AssociatedTrafficLight;
}

public struct Waypoint : IBufferElementData
{
    public float3 Position;
}

public struct LaneConnection : IBufferElementData
{
    public Entity ConnectedLane; 
}
