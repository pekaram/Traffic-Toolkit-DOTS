using Unity.Entities;
using Unity.Mathematics;

public struct Lane : IComponentData
{
    public float Width;
    public float3 StartPoint;
    public float3 EndPoint;
}
