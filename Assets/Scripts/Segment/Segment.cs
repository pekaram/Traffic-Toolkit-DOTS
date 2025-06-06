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
    public float MaxSpeed;
}

public struct ConnectionPoint : IBufferElementData
{
    public float TransitionT;

    public Entity ConnectedSegmentEntity;
    public float ConnectedSegmentT;

    public int Type; // 0 = Intersection, 1 = LeftAdjacent, 2 = RightAdacent
}