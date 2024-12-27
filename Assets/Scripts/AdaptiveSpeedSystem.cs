using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class AdaptiveSpeedSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Query all vehicles and their associated transforms
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            //Ensure the vehicle has a valid LaneEntity
            if (vehicle.ValueRW.Lane == Entity.Null)
                continue;

       
            if (math.distance(vehicle.ValueRO.WaypointPosition, transform.ValueRO.Position) < 4)
            {
                vehicle.ValueRW.Speed = 0;
            }

            float3 currentPosition = transform.ValueRW.Position;
            var vehicleMoveDirection = math.normalize(vehicle.ValueRO.WaypointPosition - currentPosition);

            // Initialize adaptive speed
            var adjustedSpeed = vehicle.ValueRW.Speed;

            // Query other vehicles in the same lane
            foreach (var (otherVehicle, otherTransform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
            {
                if (otherVehicle.ValueRO.Lane != vehicle.ValueRO.Lane)
                    continue;

                float3 otherPosition = otherTransform.ValueRW.Position;

                // Check if the other vehicle is ahead 
                float3 distanceToOtherVehicle = otherPosition - currentPosition;
                var directiontoOtherVehicle = math.normalize(distanceToOtherVehicle);


                float distanceAlongLane = math.dot(distanceToOtherVehicle, vehicleMoveDirection);
                if (distanceAlongLane > 0
                    && distanceAlongLane < 10)
                {
                    // Slow down to maintain safe following distance
                    adjustedSpeed = otherVehicle.ValueRO.Speed;
                }
            }

            // Update the vehicle's current speed
            vehicle.ValueRW.Speed = adjustedSpeed;

            // Move the vehicle forward at the adjusted speed
            transform.ValueRW.Position += transform.ValueRO.Forward() * adjustedSpeed * deltaTime;

            // Update the Rotation component directly
            if (math.lengthsq(vehicleMoveDirection) > 0.005f && adjustedSpeed > 0) // Avoid division by zero
            {
                var targetRotation = quaternion.LookRotationSafe(vehicleMoveDirection, math.up());
                //transform.ValueRW.Rotation = targetRotation;
                transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation, deltaTime * 5); // Smooth rotation
            }

        }

    }
}