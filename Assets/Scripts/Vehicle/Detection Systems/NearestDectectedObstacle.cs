using Unity.Entities;

[System.Serializable]
public struct NearestDectectedObstacle : IComponentData
{
    public ObstacleType Type;
    public float Distance;
}

public enum ObstacleType
{
    None,
    SlowVehicle,
    MergeAhead,
    RedLight,
    DeadEnd,
}