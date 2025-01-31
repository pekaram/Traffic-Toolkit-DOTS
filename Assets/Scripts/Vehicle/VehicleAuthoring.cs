using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class VehicleAuthoring : MonoBehaviour
{
    public float Speed;
    public GameObject Lane;
}

class VehicleBaker : Baker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Vehicle
        {
            Speed = authoring.Speed,
            CurrentLane = GetEntity(authoring.Lane, TransformUsageFlags.None)
        });
    }
}