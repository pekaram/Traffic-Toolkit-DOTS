using Bezier;
using Unity.Entities;
using UnityEngine;

class VehicleAuthoring : MonoBehaviour
{
    [Range(0.5f, 1.5f)]
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
            CurrentSegment = GetEntity(authoring.Segment, TransformUsageFlags.None),
            T = authoring.T,
            DriverSpeedBias = authoring.DriverSpeedBias,
        });

        AddComponent(entity, new MergeTag());
        SetComponentEnabled<MergeTag>(entity, false);

        AddComponent(entity, new NearestDectectedObstacle());
    }
}