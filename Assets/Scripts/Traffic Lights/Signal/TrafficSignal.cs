using Unity.Entities;
using Unity.Mathematics;

public struct TrafficSignal : IComponentData
{
    public Entity TrafficLight;

    public TrafficLightSignal Signal;

    public float4 EmissionColor;
}
