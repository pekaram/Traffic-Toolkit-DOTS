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
            // Ensure the vehicle has a valid LaneEntity
            if (vehicle.ValueRW.Lane == Entity.Null)
                continue;

            // Retrieve the Lane component
            Lane lane = SystemAPI.GetComponent<Lane>(vehicle.ValueRW.Lane);

            float3 currentPosition = transform.ValueRW.Position;

            // Initialize adaptive speed
            float adjustedSpeed = vehicle.ValueRW.Speed;

            // Query other vehicles in the same lane
            foreach (var (otherVehicle, otherTransform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
            {
                // Skip the current vehicle itself
                if (otherVehicle.Equals(vehicle))
                    continue;

                // Ensure the other vehicle is on the same lane
                // Frontal vehicle can be null first off 
                if (otherVehicle.ValueRW.Lane != vehicle.ValueRW.Lane)
                {
                    continue;
                }

                float3 otherPosition = otherTransform.ValueRW.Position;

                // Check if the other vehicle is ahead on the same lane
                float3 laneDirection = math.normalize(lane.EndPoint - lane.StartPoint);
                float3 toOtherVehicle = otherPosition - currentPosition;
                float distanceAlongLane = math.dot(toOtherVehicle, laneDirection);
                if (distanceAlongLane > 0 && distanceAlongLane < 20f) // Adjust threshold distance as needed
                {
                  // Slow down to maintain safe following distance
                    float stoppingDistance = 15f; // Minimum distance to the vehicle ahead
                    adjustedSpeed = otherVehicle.ValueRO.Speed;
                }
            }

            // Update the vehicle's current speed
            vehicle.ValueRW.Speed = adjustedSpeed;

            // Move the vehicle forward at the adjusted speed
            float3 direction = math.normalize(lane.EndPoint - currentPosition);
            transform.ValueRW.Position += direction * adjustedSpeed * deltaTime;
        }
    }
}
