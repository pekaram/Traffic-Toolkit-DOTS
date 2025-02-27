using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using System;

public class TrafficLightAuthoring : MonoBehaviour
{
    public List<LaneAuthoring> Lanes = new();

    public event Action OnValidated;

    private void OnValidate()
    {
        OnValidated?.Invoke();
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