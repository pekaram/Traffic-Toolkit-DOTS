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

        void Execute(ref Vehicle vehicle, ref LocalTransform transform)
        {
            if (vehicle.CurrentSegment == Entity.Null || vehicle.T >= 1f || vehicle.Speed == 0)
                return;

            if (!SegmentLookup.TryGetComponent(vehicle.CurrentSegment, out var segment))
                return;

            transform.Position = EvaluateCubicBezier(segment, vehicle.T);

            vehicle.T = TranslateT(segment, vehicle.T, vehicle.Speed * DeltaTime);
            var newPos = EvaluateCubicBezier(segment, vehicle.T);
            var direction = math.normalize(newPos - transform.Position);
            var targetRotation = quaternion.LookRotationSafe(direction, math.up());

            transform.Rotation = targetRotation;
            transform.Position = newPos;
        }

        private static float TranslateT(Segment segment, float t, float targetDistance)
        {
            const int steps = 1000;
            var newPosition = EvaluateCubicBezier(segment, t);
            var oldPosition = newPosition;
            for (var step = t * steps; step <= steps + 1; step += 1)
            {
                t = step / steps;
                if (t > 1)
                    return 1;
            
                newPosition = EvaluateCubicBezier(segment, t);
                var steppedDistance = math.distance(newPosition, oldPosition);
                if (steppedDistance < targetDistance)
                    continue;

                var previousT = (step - 1) / steps;
                var previousPosition = EvaluateCubicBezier(segment, previousT);
                var previousDistance = math.distance(oldPosition, previousPosition);

                var ratio = (targetDistance - previousDistance) / (steppedDistance - previousDistance);
                var interpretedT = math.lerp(previousT, t, ratio);

                return interpretedT;
            }

            UnityEngine.Debug.LogError("Failed to Translate T");
            return t;
        }

        private static float3 EvaluateCubicBezier(Segment segment, float t)
        {
            var p0 = segment.Start;
            var p1 = segment.StartTangent;
            var p2 = segment.EndTangent;
            var p3 = segment.End;

            float u = 1 - t;
            return
                u * u * u * p0 +
                3 * u * u * t * p1 +
                3 * u * t * t * p2 +
                t * t * t * p3;
        }
    }
}
