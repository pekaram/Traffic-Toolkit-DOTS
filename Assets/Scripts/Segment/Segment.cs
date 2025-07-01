using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct Segment : IComponentData
{
    // Bezier curve control points
    public float3 Start; 
    public float3 StartTangent; 
    public float3 EndTangent; 
    public float3 End;

    // Traffic Data
    public Entity AssociatedTrafficLight;
    public float SpeedLimit;
    public bool IsDeadEnd;
}

public enum ConnectionType
{
    Intersection = 0,
    LeftAdjacent = 1,
    RightAdjacent = 2,
    Join = 3,
}

public struct ConnectorElementData : IBufferElementData
{
    public Entity ConnectorSegmentEntity;
}

public struct Connector : IComponentData
{
    public Entity SegmentA;
    public Entity SegmentB;
    public float TransitionT;
    public float MergeT;

    public ConnectionType Type; 
}