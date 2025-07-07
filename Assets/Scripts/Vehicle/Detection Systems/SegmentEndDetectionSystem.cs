using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentEndDetectionSystem : ISystem
{
    private const float TrafficLightStopGap = 0;


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, nearestObstacle) in SystemAPI.Query<RefRW<Vehicle>, RefRW<NearestDectectedObstacle>>())
        {
            if (vehicle.ValueRW.CurrentSegment == Entity.Null)
                continue;

            var brakingDistance = (vehicle.ValueRO.CurrentSpeed * vehicle.ValueRO.CurrentSpeed) / (2f * CollisionDetectionSystem.BrakingPowerPerSecond);
            var minimumBrakingDistance = brakingDistance + TrafficLightStopGap;
            minimumBrakingDistance = math.max(minimumBrakingDistance, TrafficLightStopGap);

            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);
            var vehiclePosition = BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T);

            var remainingDistance = math.distance(BezierUtilities.EvaluateCubicBezier(segment, 1), BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T));

            vehicle.ValueRW.SpeedToReach = segment.SpeedLimit * vehicle.ValueRO.DriverSpeedBias;

            var segmentDirection = BezierUtilities.EvaluateCubicBezier(segment, 1) - BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T);

            ResetDetectedObstacle(ref nearestObstacle.ValueRW);

            if (remainingDistance > minimumBrakingDistance)
            {
                continue;
            }

            if (segment.IsDeadEnd)
            {
                TrySetNearestObstacle(ref nearestObstacle.ValueRW, remainingDistance, ObstacleType.DeadEnd);
                continue;
            }

            if (segment.AssociatedTrafficLight == Entity.Null)
            {
                continue;
            }

            var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
            if (trafficLight.Signal != TrafficLightSignal.Green)
            {
                TrySetNearestObstacle(ref nearestObstacle.ValueRW, remainingDistance, ObstacleType.RedLight);
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
