using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentEndDetectionSystem : ISystem
{
    private const float CriticalGap = 10;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, nearestObstacle) in SystemAPI.Query<RefRW<Vehicle>, RefRW<NearestDectectedObstacle>>())
        {
            if (vehicle.ValueRW.CurrentSegment == Entity.Null)
                continue;

            var brakeStopDistance = (vehicle.ValueRO.CurrentSpeed * vehicle.ValueRO.CurrentSpeed) / (2f * SpeedSystem.BrakingPowerPerSecond);
            var criticalStopDistance = brakeStopDistance + CriticalGap;

            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);
            var remainingDistanceToSegmentEnd = math.distance(BezierUtilities.EvaluateCubicBezier(segment, 1), BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T));

            ResetDetectedObstacle(ref nearestObstacle.ValueRW);

            if (remainingDistanceToSegmentEnd > criticalStopDistance)
                continue;

            if (segment.IsDeadEnd)
            {
                TrySetNearestObstacle(ref nearestObstacle.ValueRW, remainingDistanceToSegmentEnd, ObstacleType.DeadEnd);
                continue;
            }

            if (segment.AssociatedTrafficLight == Entity.Null)
                continue;

            var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
            if (trafficLight.Signal != TrafficLightSignal.Green)
            {
                TrySetNearestObstacle(ref nearestObstacle.ValueRW, remainingDistanceToSegmentEnd, ObstacleType.RedLight);
            }
        }
    }

    public void TrySetNearestObstacle(ref NearestDectectedObstacle nearestObstacle, float distance, ObstacleType obstacleType)
    {
        if (nearestObstacle.Type != ObstacleType.None && nearestObstacle.Distance < distance)
            return;

        nearestObstacle.Type = obstacleType;
        nearestObstacle.Distance = distance;
    }

    private void ResetDetectedObstacle(ref NearestDectectedObstacle nearestObstacle)
    {
        var ownType = nearestObstacle.Type == ObstacleType.DeadEnd || nearestObstacle.Type == ObstacleType.RedLight;
        if (ownType)
        {
            nearestObstacle.Type = ObstacleType.None;
        }
    }
}
