using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Aspects;
using Unity.Transforms;

public partial struct AdaptiveSpeedSystem : ISystem
{
    private const float IdealSpeed = 20;

    private const float BrakingPower = 100;

    private const float AcceleratingPower = 100;

    private const float CollisionDetectionDistance = 20;

    private const float MinimumSpeed = 0;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (vehicle, transform, entity) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (vehicle.ValueRW.CurrentLane == Entity.Null)
                continue;

            if (vehicle.ValueRO.RemainingWaypoints == 0)
            {
                Brake(vehicle, BrakingPower * deltaTime);
                continue;
            }

            var colliderAspect = SystemAPI.GetAspect<ColliderAspect>(entity);    
            if (physicsWorld.CastCollider(in colliderAspect, transform.ValueRO.Forward(), CollisionDetectionDistance, out var hit))
            {
                if (SystemAPI.HasComponent<Vehicle>(hit.Entity))
                {
                    Brake(vehicle, BrakingPower * deltaTime);
                    continue;
                }
            }

            Accelerate(vehicle, AcceleratingPower * deltaTime);
        }
    }

    private void Brake(RefRW<Vehicle> vehicle, float brakePower)
    {
        vehicle.ValueRW.Speed = vehicle.ValueRW.Speed <= MinimumSpeed ? MinimumSpeed : vehicle.ValueRW.Speed - brakePower;
    }

    private void Accelerate(RefRW<Vehicle> vehicle, float acceleratePower)
    {
        if (vehicle.ValueRO.Speed >= IdealSpeed)
            return;

        vehicle.ValueRW.Speed += acceleratePower;
    }
}