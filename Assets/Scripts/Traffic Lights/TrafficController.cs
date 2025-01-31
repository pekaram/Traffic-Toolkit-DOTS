using Unity.Entities;

public struct TrafficController : IComponentData
{
    public float CycleTime; 
    public float ElapsedTime; 
    public float YellowSignalPercentage;
}

public struct ControlledTrafficLight : IBufferElementData
{
    public Entity Entity;

    public int CurrentState;

    public Entity AssociatedLane;
}