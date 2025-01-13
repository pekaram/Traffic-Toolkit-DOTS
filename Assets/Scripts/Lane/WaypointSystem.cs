using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct WaypointSystem : ISystem
{
    private BufferLookup<Waypoint> _waypointLookup;

    private BufferLookup<LaneConnection> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _waypointLookup = state.GetBufferLookup<Waypoint>(true);
        _connectionLookup = state.GetBufferLookup<LaneConnection>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _waypointLookup.Update(ref state);
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, transform, entity) in SystemAPI.Query<RefRW<Vehicle>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            if (vehicle.ValueRW.CurrentLane == Entity.Null)
                continue;

            _waypointLookup.TryGetBuffer(vehicle.ValueRO.CurrentLane, out var waypoints);

            if (waypoints.Length == 0)
                continue;

            if (vehicle.ValueRO.WaypointIndex >= waypoints.Length)
            {
                TrySwitchToNextLane(vehicle, ref state);
                continue;
            }

            vehicle.ValueRW.WaypointPosition = waypoints[vehicle.ValueRO.WaypointIndex].Position;

            if (math.distance(waypoints[vehicle.ValueRO.WaypointIndex].Position, transform.ValueRO.Position) > 10)
                continue;

            // Reached the current waypoint, move to the next one
            vehicle.ValueRW.WaypointIndex++;
        }
    }

    private bool TrySwitchToNextLane(RefRW<Vehicle> vehicle, ref SystemState state)
    {
        if (vehicle.ValueRO.NextLane == Entity.Null)
            return false;

        var lane = SystemAPI.GetComponent<Lane>(vehicle.ValueRO.NextLane);
        if (!lane.IsAvailable)
        {
           // UnityEngine.Debug.LogError("A vehicle Stopped at traffic light a lane index" + vehicle.ValueRO.NextLane.Index);
            return false;
        }
   
        vehicle.ValueRW.CurrentLane = vehicle.ValueRW.NextLane;
        vehicle.ValueRW.WaypointIndex = 0;
        vehicle.ValueRW.NextLane = Entity.Null;

        return true;
    }
}