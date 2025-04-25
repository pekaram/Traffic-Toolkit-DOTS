using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct TranslateVehicleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        _segmentLookup.Update(ref state);

        new TranslateVehicleJob
        {
            DeltaTime = deltaTime,
            SegmentLookup = _segmentLookup,
        }.ScheduleParallel();
    }

    private ComponentLookup<Segment> _segmentLookup;

    public void OnCreate(ref SystemState state)
    {
        _segmentLookup = state.GetComponentLookup<Segment>(true);
    }

    [BurstCompile]
    public partial struct TranslateVehicleJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Segment> SegmentLookup;

        [ReadOnly] public float DeltaTime;


        void Execute(ref VehicleV2 vehicle, ref LocalTransform transform)
        {
            if (vehicle.CurrentSegment == Entity.Null || vehicle.T >= 1f)
                return;

            if (!SegmentLookup.TryGetComponent(vehicle.CurrentSegment, out var segment))
                return;

            var speed = vehicle.Speed;
            var newPos = transform.Position;
         
            var steps = 10000;
            var currentStep = vehicle.T * steps;
            for (var t = currentStep; t <= steps + 1; t += 1)
            {
                vehicle.T = t / steps;
                newPos = EvaluateCubicBezier(
                    segment.Start,
                    segment.StartTangent,
                    segment.EndTangent,
                    segment.End,
                    vehicle.T);

                if (math.distance(newPos, transform.Position) >= 10 * DeltaTime)
                    break;

                if (vehicle.T >= 1)
                    break;
            }

            var direction = math.normalize(newPos - transform.Position);
            var targetRotation = quaternion.LookRotationSafe(direction, math.up());

            transform.Rotation = targetRotation;
            transform.Position = newPos;
        }

        private static float3 EvaluateCubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float u = 1 - t;
            return
                u * u * u * p0 +
                3 * u * u * t * p1 +
                3 * u * t * t * p2 +
                t * t * t * p3;
        }
    }
}
