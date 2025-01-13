using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct LaneSystem : ISystem
{
    private BufferLookup<LaneConnection> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
       _connectionLookup = state.GetBufferLookup<LaneConnection>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, entity) in SystemAPI.Query<RefRW<Vehicle>>().WithEntityAccess())
        {
            if (vehicle.ValueRW.NextLane != Entity.Null)
                continue;

            TrySetNextLane(vehicle, entity.Index);
        }
    }

    private bool TrySetNextLane(RefRW<Vehicle> vehicle, int index)
    {
        _connectionLookup.TryGetBuffer(vehicle.ValueRO.CurrentLane, out var connections);
        if (connections.Length == 0)
            return false;

        var random = new Random((uint)index * 100000);
        var randomIndex = random.NextInt(connections.Length);
        vehicle.ValueRW.NextLane = connections[randomIndex].ConnectedLane;
   
        return true;
    }
}