using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct SpeedControlSystem : ISystem
{
    public const float CriticalGap = 10;

    private const float AcceleratingPower = 10;
    public const float BrakingPowerPerSecond = 10; 

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSystem = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSystem.CollisionWorld;
        var deltaTime = SystemAPI.Time.DeltaTime;

        var controlVehicleSpeed = new ControlVehicleSpeed() { CollisionWorld = collisionWorld, DeltaTime = deltaTime };
        controlVehicleSpeed.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct ControlVehicleSpeed : IJobEntity
    {
        [ReadOnly] public CollisionWorld CollisionWorld;

        [ReadOnly] public float DeltaTime;

        public void Execute(ref Vehicle vehicle, in LocalTransform transform, in PhysicsCollider physicsCollider)
        {
            var isPathBlocked = IsPathBlocked(ref vehicle, transform, physicsCollider);
            if (vehicle.CurrentSpeed > vehicle.SpeedToReach || isPathBlocked)
            {
                Brake(ref vehicle, DeltaTime * BrakingPowerPerSecond);
            }
            else
            {
                Accelerate(ref vehicle, AcceleratingPower * DeltaTime);
            }
        }

        private bool IsPathBlocked(ref Vehicle vehicle, in LocalTransform transform, in PhysicsCollider physicsCollider)
        {
            var colliderBlob = physicsCollider.Value;
            var aabb = colliderBlob.Value.CalculateAabb();
            var distanceToColliderTip = aabb.Extents.z;
            var startOffset = distanceToColliderTip + 0.1f;
            var start = transform.Position + transform.Forward() * startOffset;

            var brakingDistance = (vehicle.CurrentSpeed * vehicle.CurrentSpeed) / (2f * BrakingPowerPerSecond);
            var detectionDistance = brakingDistance + CriticalGap;
            var end = transform.Position + transform.Forward() * detectionDistance;

            var colliderCast = new ColliderCastInput(colliderBlob, start, end);
            return CollisionWorld.CastCollider(colliderCast, out _);
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

    private static void LogCollisionError(Entity entity1, Entity entity2, EntityManager entityManager)
    {
        var id1 = entityManager.GetComponentData<FixedEntityId>(entity1).Id;
        var id2 = entityManager.GetComponentData<FixedEntityId>(entity2).Id;

        var formattedMessage = string.Format("Collision detected between: Entity1 {0} Entity2 {1}", id1, id2);
        UnityEngine.Debug.LogError(formattedMessage);
    }
}