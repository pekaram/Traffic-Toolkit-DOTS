using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

public class TrafficControllerAuthoring : MonoBehaviour
{
    public List<TrafficLightAuthoring> ControlledTrafficLights;
    public float CycleTime;
}

public class TrafficControllerBaker : Baker<TrafficControllerAuthoring>
{
    public override void Bake(TrafficControllerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TrafficController
        {
            CycleTime = authoring.CycleTime,
            ElapsedTime = 0
        });

   
        var controlledTrafficLights = AddBuffer<ControlledTrafficLight>(entity);
        foreach (var controlledTrafficLight in authoring.ControlledTrafficLights)
        {
            var trafficLightEntitiy = GetEntity(controlledTrafficLight, TransformUsageFlags.Renderable);
            controlledTrafficLights.Add(new ControlledTrafficLight { Entity = trafficLightEntitiy });
        }
    }
}
