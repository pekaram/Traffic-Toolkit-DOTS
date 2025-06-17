using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class TrafficControllerAuthoring : MonoBehaviour
{
    public List<TrafficLightAuthoring> ControlledTrafficLights;

    public float CycleTime;

    [Range(0f, 1f)]
    public float YellowSignalPercentage;
}

public class TrafficControllerBaker : Baker<TrafficControllerAuthoring>
{
    public override void Bake(TrafficControllerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new TrafficController
        {
            CycleTime = authoring.CycleTime,
            ElapsedTime = 0,
            YellowSignalPercentage = authoring.YellowSignalPercentage,
        });

        var controlledTrafficLights = AddBuffer<TrafficLightBufferElement>(entity);
        foreach (var controlledTrafficLight in authoring.ControlledTrafficLights)
        {
            var trafficLightEntitiy = GetEntity(controlledTrafficLight, TransformUsageFlags.None);
            controlledTrafficLights.Add(new TrafficLightBufferElement { TrafficLightEntity = trafficLightEntitiy });
        }
    }
}
