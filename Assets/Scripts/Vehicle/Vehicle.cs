using Unity.Entities;

public struct Vehicle : IComponentData
{
    public float SpeedToReach;
    public float CurrentSpeed;

    public Entity CurrentSegment;
    public float T;
}
