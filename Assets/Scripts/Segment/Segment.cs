using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct Segment : IComponentData
{
    public float3 Start; 
    public float3 StartTangent; 
    public float3 EndTangent; 
    public float3 End;

    public Entity AssociatedTrafficLight;
}

public struct SegmentConnection : IBufferElementData
{
    public Entity ConnectedSegment;
}