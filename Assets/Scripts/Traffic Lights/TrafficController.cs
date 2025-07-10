using Unity.Entities;

public struct TrafficController : IComponentData
{
    public float CycleTime; 
    public float ElapsedTime; 
    public float YellowSignalPercentage;
}

public struct TrafficLightBufferElement : IBufferElementData
{
    public Entity TrafficLightEntity;
}