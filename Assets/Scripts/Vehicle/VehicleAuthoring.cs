using UnityEngine;
using Unity.Entities;

class VehicleAuthoring : MonoBehaviour
{
    [Range(0.5f,1.5f)]
    public float DriverSpeedBias = 1;
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
            SpeedToReach = authoring.Segment.SpeedLimit * authoring.DriverSpeedBias,
            CurrentSegment = GetEntity(authoring.Segment, TransformUsageFlags.None),
            T = authoring.T,
            DriverSpeedBias = authoring.DriverSpeedBias,
        });
    }
}