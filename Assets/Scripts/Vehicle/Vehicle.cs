using Unity.Entities;

public struct Vehicle : IComponentData
{
    public float CurrentSpeed;
    public float DriverSpeedBias;

    public Entity CurrentSegment;
    public float T;
}

public struct MergeTag : IComponentData, IEnableableComponent
{
}
