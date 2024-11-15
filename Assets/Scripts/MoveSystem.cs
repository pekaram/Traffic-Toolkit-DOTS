//using Unity.Burst;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;

//public partial struct MoveSystem : ISystem
//{
//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        foreach (RefRW<LocalTransform> spawner in SystemAPI.Query<RefRW<LocalTransform>>())
//        {
//            //spawner.ValueRW.Position = spawner.ValueRW.Position + new float3(0.01f, 0.01f, 0.01f);
//        }
//    }
//}