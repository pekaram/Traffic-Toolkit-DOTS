using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MergingVehicleDetectionSystem : ISystem
{
    private const float TrafficLightStopGap = 20;


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

            var isMergingVehicleAhead = false;
            var mergingVehiclePosition = float3.zero;

            foreach (var (mergingVehicle, mergingTransform) 
                in SystemAPI.Query<RefRW<Vehicle>, RefRO<LocalTransform>>().WithAll<MergeTag>())
            {
                var segmentToJoin = SystemAPI.GetComponent<Connector>(mergingVehicle.ValueRO.CurrentSegment).SegmentB;
                if (!segmentToJoin.Equals(vehicle.ValueRO.CurrentSegment))
                    continue;

                var remainingDistanceToMerging = math.distance(mergingTransform.ValueRO.Position, vehiclePosition);

                var distanceDirection = mergingTransform.ValueRO.Position - vehiclePosition;
                var headingDirection = BezierUtilities.EvaluateCubicBezier(segment, 1) - vehiclePosition;
                var distanceDot = math.dot(math.normalize(distanceDirection), math.normalize(headingDirection));

                if (remainingDistanceToMerging < minimumBrakingDistance && distanceDot > 0)
                {
                    isMergingVehicleAhead = true;
                    mergingVehiclePosition = mergingTransform.ValueRO.Position;

                    break;
                }
            }

            if (!isMergingVehicleAhead)
            {
                ResetDetectedObstacle(ref nearestObstacle.ValueRW);
                continue;
            }

            var distanceToMergingVehicle = math.distance(mergingVehiclePosition, vehiclePosition);
            TrySetNearestObstacle(ref nearestObstacle.ValueRW, distanceToMergingVehicle, ObstacleType.MergeAhead);
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
        var ownDetection = nearestObstacle.Type == ObstacleType.MergeAhead;
        if (ownDetection)
        {
            nearestObstacle.Type = ObstacleType.None;
        }
    }
}
