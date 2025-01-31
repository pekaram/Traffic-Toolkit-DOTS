using Unity.Entities;

public struct TrafficLight : IComponentData
{
    public TrafficLightSignal Signal;
}

public enum TrafficLightSignal
{
    Red = 0,
    Green = 1,
    Yellow = 2,
}
