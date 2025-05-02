using UnityEngine;
using Unity.Entities;

class VehicleAuthoring : MonoBehaviour
{
    public float Speed;
    public GameObject Lane;
    public float T;
}

class VehicleBaker : Baker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new VehicleV2
        {
            Speed = authoring.Speed,
            CurrentSegment = GetEntity(authoring.Lane, TransformUsageFlags.None),
            T = authoring.T
        });
    }
}