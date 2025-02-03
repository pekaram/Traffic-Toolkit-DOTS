using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public class TrafficSignalAuthoring : MonoBehaviour
{
    public TrafficLightAuthoring TrafficLight;

    public TrafficLightSignal Signal;

    public Color EmissionColor;
}

public class TrafficSignalBaker : Baker<TrafficSignalAuthoring>
{
    public override void Bake(TrafficSignalAuthoring authoring)
    {
        var trafficLight = GetEntity(authoring.TrafficLight, TransformUsageFlags.None);
        var emissionColor = new float4(authoring.EmissionColor.r, authoring.EmissionColor.g, authoring.EmissionColor.b, authoring.EmissionColor.a);

        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new TrafficSignal
        {
            TrafficLight = trafficLight,
            Signal = authoring.Signal,
            EmissionColor = emissionColor,
        });
        AddComponent(entity, new URPMaterialPropertyEmissionColor() { Value = emissionColor });
    }
}