using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public partial struct BasicTrafficLightViewSystem : ISystem
{
    private static readonly Dictionary<TrafficLightSignal, float4> signalsToColors = new()
    {
        { TrafficLightSignal.Green,  new float4(0, 1, 0, 1) },
        { TrafficLightSignal.Yellow,  new float4(1, 1, 0, 1) },
        { TrafficLightSignal.Red,  new float4(1, 0, 0, 1) }
    };

    public void OnUpdate(ref SystemState state)
    {   
        foreach (var (colorProperty, trafficLightView) in SystemAPI.Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<TrafficLightView>>())
        {
            var trafficLight = SystemAPI.GetComponent<TrafficLight>(trafficLightView.ValueRO.TrafficLight);
            colorProperty.ValueRW.Value = GetRGBAColor(trafficLight.Signal);
        }
    }

    private float4 GetRGBAColor(TrafficLightSignal trafficLightSignal)
    {
        if (!signalsToColors.ContainsKey(trafficLightSignal))
        {
            throw new NotSupportedException($"Unsupported Traffic Signal: {trafficLightSignal}");
        }

        return signalsToColors[trafficLightSignal];
    }
}