using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct MoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var lane = SystemAPI.GetComponent<Lane>(vehicle.ValueRW.Lane);
            var currentPosition = transform.ValueRW.Position;
            var direction = math.normalize(lane.EndPoint - lane.StartPoint);

            float distance = vehicle.ValueRW.Speed * deltaTime;
            float3 nextPosition = currentPosition + direction * distance;

            transform.ValueRW.Position = nextPosition;
            //spawner.ValueRW.Position = spawner.ValueRW.Position + new float3(0.01f, 0.01f, 0.01f);
        }
    }
}