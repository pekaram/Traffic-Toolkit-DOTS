using Unity.Entities;
using Unity.Mathematics;

public struct Vehicle : IComponentData
{
    public float DesiredSpeed;
    public float CurrentSpeed;

    public Entity CurrentSegment;
    public float T;
}
