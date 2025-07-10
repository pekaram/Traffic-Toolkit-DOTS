using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SpeedSystem : ISystem
{
    public const float BrakingPowerPerSecond = 10;
    private const float AcceleratingPower = 10;
    private const float MinimumCreepSpeed = 0.1f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (vehicle, nearestObstacle) in SystemAPI.Query<RefRW<Vehicle>, RefRO<NearestDectectedObstacle>>())
        {
            var isPathBlocked = nearestObstacle.ValueRO.Type != ObstacleType.None;
            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);
            var speedToReach = segment.SpeedLimit * vehicle.ValueRO.DriverSpeedBias;

            if (vehicle.ValueRO.CurrentSpeed > speedToReach || isPathBlocked)
            {
                Brake(ref vehicle.ValueRW, deltaTime * BrakingPowerPerSecond, MinimumCreepSpeed);
            }
            else
            {
                Accelerate(ref vehicle.ValueRW, AcceleratingPower * deltaTime, speedToReach);
            }
        }
    }

    private void Brake(ref Vehicle vehicle, float brakePower, float minSpeed)
    {
        vehicle.CurrentSpeed = vehicle.CurrentSpeed - brakePower;
        if (vehicle.CurrentSpeed < minSpeed)
        {
            vehicle.CurrentSpeed = 0;
        }
    }

    private void Accelerate(ref Vehicle vehicle, float acceleratePower, float maxSpeed)
    {
        vehicle.CurrentSpeed = math.min(acceleratePower + vehicle.CurrentSpeed, maxSpeed);
    }
}
