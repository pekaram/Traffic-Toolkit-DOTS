using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct WaypointSystem : ISystem
{
    private BufferLookup<Waypoint> _bufferLookup;

    public void OnCreate(ref SystemState state)
    {
        _bufferLookup = state.GetBufferLookup<Waypoint>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _bufferLookup.Update(ref state);


        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>())
        {
            if (vehicle.ValueRW.Lane == Entity.Null)
                continue;

            _bufferLookup.TryGetBuffer(vehicle.ValueRO.Lane, out var waypoints);

            if (waypoints.Length == 0 || vehicle.ValueRO.WaypointIndex >= waypoints.Length)
                continue;

            vehicle.ValueRW.WaypointPosition = waypoints[vehicle.ValueRO.WaypointIndex].Position;

            if (math.distance(waypoints[vehicle.ValueRO.WaypointIndex].Position, transform.ValueRO.Position) > 10)
                continue;

            // Reached the current waypoint, move to the next one
            vehicle.ValueRW.WaypointIndex++;
        }
    }
}