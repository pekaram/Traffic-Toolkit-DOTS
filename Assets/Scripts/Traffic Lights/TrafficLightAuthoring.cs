using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;

public class TrafficLightAuthoring : MonoBehaviour
{
    public List<LaneAuthoring> Lanes = new();

    public void OnValidate()
    {
        foreach (var lane in Lanes)
        {
            lane.TrafficLight = this;
        }
    }
}

public class TrafficLightBaker : Baker<TrafficLightAuthoring>
{
    public override void Bake(TrafficLightAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new TrafficLight());
    }
}