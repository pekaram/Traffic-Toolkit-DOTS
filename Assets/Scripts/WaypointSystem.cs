using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static UnityEngine.EventSystems.EventTrigger;

public partial struct WaypointSystem : ISystem
{
    private BufferLookup<Waypoint> _bufferLookup;

    private BufferLookup<LaneConnection> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _bufferLookup = state.GetBufferLookup<Waypoint>(true);
        _connectionLookup = state.GetBufferLookup<LaneConnection>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _bufferLookup.Update(ref state);
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, transform, entity) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (vehicle.ValueRW.CurrentLane == Entity.Null)
                continue;

            _bufferLookup.TryGetBuffer(vehicle.ValueRO.CurrentLane, out var waypoints);

            if (waypoints.Length == 0)
                continue;

            if (vehicle.ValueRO.WaypointIndex >= waypoints.Length)
            {
                TrySwitchToNextLane(vehicle);
                continue;
            }

            vehicle.ValueRW.WaypointPosition = waypoints[vehicle.ValueRO.WaypointIndex].Position;

            if (math.distance(waypoints[vehicle.ValueRO.WaypointIndex].Position, transform.ValueRO.Position) > 10)
                continue;

            // Reached the current waypoint, move to the next one
            vehicle.ValueRW.WaypointIndex++;
        }
    }

    private bool TrySwitchToNextLane(RefRW<Vehicle> vehicle)
    {
        if (vehicle.ValueRW.NextLane == Entity.Null)
            return false;

        vehicle.ValueRW.CurrentLane = vehicle.ValueRW.NextLane;
        vehicle.ValueRW.WaypointIndex = 0;
        vehicle.ValueRW.NextLane = Entity.Null;

        return true;
    }
}