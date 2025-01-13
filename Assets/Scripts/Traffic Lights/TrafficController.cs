using Unity.Collections;
using Unity.Entities;

public struct TrafficController : IComponentData
{
    public float CycleTime; // Time for one full cycle (e.g., 60 seconds)
    public float ElapsedTime; // Time elapsed in the current cycle
}

public struct ControlledTrafficLight : IBufferElementData
{
    public Entity Entity;

    // Option A
    public int CurrentState;

    // Can this be a list?
    public Entity AssociatedLane;
}