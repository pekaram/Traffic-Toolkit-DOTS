using UnityEngine;
using Unity.Entities;

class VehicleAuthoring : MonoBehaviour
{
    public float MaxSpeed;
    public SegmentAuthoring Segment;
    public float T;
}

class VehicleBaker : Baker<VehicleAuthoring>
{
    public override void Bake(VehicleAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Vehicle
        {
            MaxSpeed = authoring.MaxSpeed,
            CurrentSegment = GetEntity(authoring.Segment, TransformUsageFlags.None),
            T = authoring.T
        });
    }
}