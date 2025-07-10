using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
 
public partial struct MergingVehicleDetectionSystem : ISystem
{
    private const float CriticalGap = CollisionDetectionSystem.CriticalGap;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, nearestObstacle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<NearestDectectedObstacle>, RefRO<LocalTransform>>())
        {
            ResetDetectedObstacle(ref nearestObstacle.ValueRW);

            if (vehicle.ValueRW.CurrentSegment == Entity.Null)
                continue;

            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);
            var vehiclePosition = transform.ValueRO.Position;

            var distanceToMergingVehicle = 0f;
            foreach (var (mergingVehicle, mergingTransform) 
                in SystemAPI.Query<RefRW<Vehicle>, RefRO<LocalTransform>>().WithAll<MergeTag>())
            {
                var segmentToJoin = SystemAPI.GetComponent<Connector>(mergingVehicle.ValueRO.CurrentSegment).SegmentB;
                if (!segmentToJoin.Equals(vehicle.ValueRO.CurrentSegment))
                    continue;

                var isPathBlocked = IsPathBlockedByMerge(in vehicle.ValueRO, in vehiclePosition, in mergingTransform.ValueRO.Position, in segment);
                var distanceToPreviousMergingVehicle = distanceToMergingVehicle;
                distanceToMergingVehicle = math.distance(mergingTransform.ValueRO.Position, vehiclePosition);
                if (isPathBlocked && distanceToMergingVehicle < distanceToPreviousMergingVehicle)
                {
                    distanceToMergingVehicle = math.distance(mergingTransform.ValueRO.Position, vehiclePosition);
                    TrySetNearestObstacle(ref nearestObstacle.ValueRW, distanceToMergingVehicle, ObstacleType.MergeAhead);
                }
            }
        }
    }

    public bool IsPathBlockedByMerge(in Vehicle vehicle, in float3 vehiclePosition, in float3 mergePosition, in Segment segment)
    {
        var brakeStopDistance = (vehicle.CurrentSpeed * vehicle.CurrentSpeed) / (2f * SpeedSystem.BrakingPowerPerSecond);
        var criticalStopDistance = brakeStopDistance + CriticalGap;
        var distanceToMergeAhead = math.distance(mergePosition, vehiclePosition);
        var distanceDirection = mergePosition - vehiclePosition;
        var vehicleHeadingDirection = BezierUtilities.EvaluateCubicBezier(segment, 1) - vehiclePosition;
        var distanceDot = math.dot(math.normalize(distanceDirection), math.normalize(vehicleHeadingDirection));

        return distanceToMergeAhead < criticalStopDistance && distanceDot > 0;
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
