using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct TranslateVehicleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRO<Vehicle>, RefRW<LocalTransform>>())
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var speed = vehicle.ValueRO.Speed;
            var direction = math.normalize(vehicle.ValueRO.WaypointPosition - transform.ValueRO.Position);

            transform.ValueRW.Position += transform.ValueRO.Forward() * speed * deltaTime;

            if (speed <= 0)
                continue;

            var targetRotation = quaternion.LookRotationSafe(direction, math.up());
            transform.ValueRW.Rotation = math.slerp(transform.ValueRW.Rotation, targetRotation, deltaTime * 2.5f); 
        }
    }
}
