using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public partial struct TrafficSignalSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (colorProperty, trafficLightView) in SystemAPI.Query<RefRW<URPMaterialPropertyEmissionColor>, RefRO<TrafficSignal>>())
        {
            var trafficLight = SystemAPI.GetComponent<TrafficLight>(trafficLightView.ValueRO.TrafficLight);
            colorProperty.ValueRW.Value = trafficLightView.ValueRO.Signal == trafficLight.Signal ? trafficLightView.ValueRO.EmissionColor : float4.zero;
        }
    } 
}