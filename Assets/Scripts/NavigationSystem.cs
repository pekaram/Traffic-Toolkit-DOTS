using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct NavigationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            if (vehicle.ValueRW.Speed == 0)
                continue;

            var deltaTime = SystemAPI.Time.DeltaTime;
            const float DistanceThreshold = 2;

            var lane = SystemAPI.GetComponent<Lane>(vehicle.ValueRW.Lane);
            var currentPosition = transform.ValueRW.Position;
            var direction = math.normalize(lane.EndPoint - lane.StartPoint);

            var distance = vehicle.ValueRW.Speed * deltaTime;
            var nextPosition = currentPosition + direction * distance;

            var isRoadEnd = math.distance(nextPosition, lane.EndPoint) < DistanceThreshold;
            if (isRoadEnd)
            {
                vehicle.ValueRW.Speed = 0;
            }
        }
    }
}