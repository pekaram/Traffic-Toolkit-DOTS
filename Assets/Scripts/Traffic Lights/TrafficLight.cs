using Unity.Entities;

public struct TrafficLight : IComponentData
{
    public int CurrentState;
    public Entity AssociatedLane;
}
