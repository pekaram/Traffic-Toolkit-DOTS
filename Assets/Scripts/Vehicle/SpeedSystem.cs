using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct SpeedSystem : ISystem
{
    private const float AcceleratingPower = 10;
    public const float BrakingPowerPerSecond = 10;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (vehicle, nearestObstacle, vehicleTransform) in SystemAPI.Query<RefRW<Vehicle>, RefRO<NearestObstacle>, RefRO<LocalTransform>>())
        {
            var isPathBlocked = nearestObstacle.ValueRO.Type != ObstacleType.None;

            if (vehicle.ValueRO.CurrentSpeed > vehicle.ValueRO.SpeedToReach || isPathBlocked)
            {
                Brake(ref vehicle.ValueRW, deltaTime * BrakingPowerPerSecond);
            }
            else
            {
                Accelerate(ref vehicle.ValueRW, AcceleratingPower * deltaTime);
            }
        }
    }

    private void Brake(ref Vehicle vehicle, float brakePower)
    {
        vehicle.CurrentSpeed = math.max(0f, vehicle.CurrentSpeed - brakePower);
    }

    private void Accelerate(ref Vehicle vehicle, float acceleratePower)
    {
        vehicle.CurrentSpeed = math.min(vehicle.SpeedToReach, vehicle.CurrentSpeed + acceleratePower);
    }
}
