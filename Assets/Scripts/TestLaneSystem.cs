using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct TestLaneSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (lane, transform) in SystemAPI.Query<RefRW<Lane>, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position = transform.ValueRW.Position + new float3(0, 0.001f, 0);
        }
    }
}