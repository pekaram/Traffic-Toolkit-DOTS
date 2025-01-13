using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

public class TrafficLightViewAuthoring : MonoBehaviour
{
    public TrafficLightAuthoring TrafficLight;
}

public class TrafficLightViewBaker : Baker<TrafficLightViewAuthoring>
{
    public override void Bake(TrafficLightViewAuthoring authoring)
    {
        var trafficLight = GetEntity(authoring.TrafficLight, TransformUsageFlags.None);

        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new TrafficLightView
        {
            TrafficLight = trafficLight,
        });

        AddComponent(entity, new URPMaterialPropertyBaseColor { Value = new float4(1, 1, 1, 1) });
    }
}