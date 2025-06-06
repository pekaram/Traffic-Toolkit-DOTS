using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

public partial struct ActiveCollisionAvoidanceSystem : ISystem
{
    private const float MinimumSpeed = 0.1f;

    private const float AcceleratingPower = 100;
    private const float BrakingPower = 100;
    
    public const float CollisionDetectionDistance = 10;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSystem = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSystem.CollisionWorld;
        var deltaTime = SystemAPI.Time.DeltaTime;

        var controlVehcileSpeed = new ControlVehicleSpeed() { CollisionWorld = collisionWorld, DeltaTime = deltaTime };
        controlVehcileSpeed.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct ControlVehicleSpeed : IJobEntity
    {
        [ReadOnly] public CollisionWorld CollisionWorld;

        [ReadOnly] public float DeltaTime;

        public void Execute(ref Vehicle vehicle, in LocalTransform transform, in PhysicsCollider physicsCollider)
        {
            var colliderBlob = physicsCollider.Value;
            var aabb = colliderBlob.Value.CalculateAabb();
            var distanceToColliderTip = aabb.Extents.z;
            var startOffset = distanceToColliderTip + 0.1f;
            var start = transform.Position + transform.Forward() * startOffset;
            var end = transform.Position + transform.Forward() * CollisionDetectionDistance;

            var colliderCast = new ColliderCastInput(colliderBlob, start, end);
            CollisionWorld.CastCollider(colliderCast, out var hit);

            if (hit.Entity != Entity.Null)
            {
                Brake(ref vehicle, BrakingPower * DeltaTime);
            }
            else
            {
                Accelerate(ref vehicle, AcceleratingPower * DeltaTime);
            }
        }

        private void Brake(ref Vehicle vehicle, float brakePower)
        {
            if (vehicle.Speed < MinimumSpeed)
            {
                vehicle.Speed = 0;
            }
            else
            {
                vehicle.Speed -= brakePower;
            }
        }

        private void Accelerate(ref Vehicle vehicle, float acceleratePower)
        {
            if (vehicle.Speed >= vehicle.MaxSpeed)
            {
                vehicle.Speed = vehicle.MaxSpeed;
            }
            else
            {
                vehicle.Speed += acceleratePower;
            }
        }
    }

    private static void LogCollisionError(Entity entity1, Entity entity2, EntityManager entityManager)
    {
        var id1 = entityManager.GetComponentData<FixedEntityId>(entity1).Id;
        var id2 = entityManager.GetComponentData<FixedEntityId>(entity2).Id;

        var formattedMessage = string.Format("Collision detected between: Entity1 {0} Entity2 {1}", id1, id2);
        UnityEngine.Debug.LogError(formattedMessage);
    }
}