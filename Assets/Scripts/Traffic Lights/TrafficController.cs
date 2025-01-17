using Unity.Entities;

public struct TrafficController : IComponentData
{
    public float CycleTime; // Time for one full cycle (e.g., 60 seconds)
    public float ElapsedTime; // Time elapsed in the current cycle
}

public struct ControlledTrafficLight : IBufferElementData
{
    public Entity Entity;

    public int CurrentState;

    public Entity AssociatedLane;
}