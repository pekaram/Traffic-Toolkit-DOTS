using Unity.Entities;

public struct Vehicle : IComponentData
{
    public float MaxSpeed;
    public float Speed;
    public Entity CurrentSegment;
    public float T;
}
