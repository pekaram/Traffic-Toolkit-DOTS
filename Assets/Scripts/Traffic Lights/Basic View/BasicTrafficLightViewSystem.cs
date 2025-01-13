using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public partial struct BasicTrafficLightViewSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {   
        foreach (var (colorProperty, trafficLightView) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<TrafficLightView>>())
        {
            var trafficLight = SystemAPI.GetComponent<TrafficLight>(trafficLightView.ValueRO.TrafficLight);
            colorProperty.ValueRW.Value = trafficLight.CurrentState == 1 ? new float4(0, 1, 0, 1) : new float4(1, 0, 0, 1); // RGBA
        }
    }
}