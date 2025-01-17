using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;

public partial struct AdaptiveSpeedSystem : ISystem
{
    private const float IdealSpeed = 20;

    private const float BrakingPower = 5;

    private const float AcceleratingPower = 1;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            if (vehicle.ValueRW.CurrentLane == Entity.Null)
                continue;

            var arrivedToTargetPoint = math.distance(vehicle.ValueRO.WaypointPosition, transform.ValueRO.Position) < 4;
            if (arrivedToTargetPoint)
            {
                vehicle.ValueRW.Speed = 0;
                continue;
            }

            var isFrontalSensorOn = IsFrontalSensorOn(ref state, transform, vehicle);
            var currentSpeed = vehicle.ValueRO.Speed;
            if (isFrontalSensorOn)
            {
                vehicle.ValueRW.Speed = currentSpeed <= 0 ? 0 : currentSpeed - BrakingPower;
                continue;
            }

            if (vehicle.ValueRO.Speed >= IdealSpeed)
                continue;

            // Update the vehicle's current speed
            vehicle.ValueRW.Speed += AcceleratingPower;
        }
    }

    private bool IsFrontalSensorOn(ref SystemState state, RefRW<LocalTransform> transform, RefRW<Vehicle> vehicle)
    {
        var currentPosition = transform.ValueRW.Position;
        var vehicleMoveDirection = math.normalize(vehicle.ValueRO.WaypointPosition - currentPosition);

        foreach (var (otherVehicle, otherTransform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            if (vehicle.ValueRO.CurrentLane != otherVehicle.ValueRO.CurrentLane)
                continue;

            var otherPosition = otherTransform.ValueRW.Position;

            var distanceToOtherVehicle = otherPosition - currentPosition;

            var distanceAlongLane = math.dot(distanceToOtherVehicle, vehicleMoveDirection);
            if (distanceAlongLane > 0 && distanceAlongLane < 12)
            {
                return true;
            }
        }

        return false;
    }
}