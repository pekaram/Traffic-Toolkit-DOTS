using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public partial struct CollisionDetectionSystem : ISystem
{
    public const float CriticalGap = 10;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSystem = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var collisionWorld = physicsWorldSystem.CollisionWorld;
        var deltaTime = SystemAPI.Time.DeltaTime;

        var controlVehicleSpeed = new RaycastJob() { CollisionWorld = collisionWorld, DeltaTime = deltaTime };
        controlVehicleSpeed.ScheduleParallel();
    }

    [BurstCompile]
    public partial struct RaycastJob : IJobEntity
    {
        [ReadOnly] public CollisionWorld CollisionWorld;

        [ReadOnly] public float DeltaTime;

        public void Execute(ref NearestDectectedObstacle detectedObstacle, in Vehicle vehicle, in LocalTransform transform, in PhysicsCollider physicsCollider)
        {
            var isPathBlocked = ColliderCastForward(in vehicle, in transform, in physicsCollider, out var hit);
            if (!isPathBlocked)
            {
                ResetDetectedObstacle(ref detectedObstacle);
                return;
            }

            var distanceToHitVehicle = math.distance(hit.Position, transform.Position);
            if (detectedObstacle.Type != ObstacleType.None && detectedObstacle.Distance < distanceToHitVehicle)
                return;

            detectedObstacle.Distance = distanceToHitVehicle;
            detectedObstacle.Type = ObstacleType.SlowVehicle;
        }

        private bool ColliderCastForward(
            in Vehicle vehicle, in LocalTransform transform, in PhysicsCollider physicsCollider, out ColliderCastHit hit)
        {
            var colliderBlob = physicsCollider.Value;
            var aabb = colliderBlob.Value.CalculateAabb();
            var distanceToColliderTip = aabb.Extents.z;
            var startOffset = distanceToColliderTip + 0.1f;
            var start = transform.Position + transform.Forward() * startOffset;

            var brakingDistance = (vehicle.CurrentSpeed * vehicle.CurrentSpeed) / (2f * SpeedSystem.BrakingPowerPerSecond);
            var detectionDistance = brakingDistance + CriticalGap;
            var end = transform.Position + transform.Forward() * detectionDistance;

            var colliderCast = new ColliderCastInput(colliderBlob, start, end);
            var isHit = CollisionWorld.CastCollider(colliderCast, out hit);   
            return isHit;
        }

        public void TrySetNearestObstacle(ref NearestDectectedObstacle nearestObstacle, float distance, ObstacleType obstacleType)
        {
            if (nearestObstacle.Type != ObstacleType.None && nearestObstacle.Distance < distance)
                return;

            nearestObstacle.Type = obstacleType;
            nearestObstacle.Distance = distance;
        }

        private void ResetDetectedObstacle(ref NearestDectectedObstacle nearestObstacle)
        {
            var ownType = nearestObstacle.Type == ObstacleType.SlowVehicle;
            if (ownType)
            {
                nearestObstacle.Type = ObstacleType.None;
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