using Unity.Entities;

[System.Serializable]
public struct NearestObstacle : IComponentData
{
    public ObstacleType Type;
    public float Distance;
}

public enum ObstacleType
{
    None,
    SlowVehicle,
    MergingVehicle,
    RedLight,
    DeadEnd,
}