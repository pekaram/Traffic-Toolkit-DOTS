using Unity.Entities;
using Unity.Mathematics;

public struct Vehicle : IComponentData
{
    public float MaxSpeed;
    public float Speed;

    public Entity CurrentSegment;
    public float T;
}
