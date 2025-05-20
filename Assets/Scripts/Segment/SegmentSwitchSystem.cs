using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentSwitchSystem : ISystem
{
    private BufferLookup<SegmentConnection> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _connectionLookup = state.GetBufferLookup<SegmentConnection>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, entity) in SystemAPI.Query<RefRW<Vehicle>>().WithEntityAccess())
        {
            if (vehicle.ValueRO.T < 1)
                continue;

            TrySwitchToNextLane(ref state, vehicle, entity.Index * 100000);
        }
    }

    private bool CanSwitchSegment(RefRW<Vehicle> vehicle, ref SystemState state)
    {
        var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);

        if (segment.AssociatedTrafficLight == Entity.Null)
            return true;

        var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
        return trafficLight.Signal == TrafficLightSignal.Green;
    }

    private bool TrySwitchToNextLane(ref SystemState state, RefRW<Vehicle> vehicle, int randomSeed)
    {
        if (vehicle.ValueRO.CurrentSegment == Entity.Null)
            return false;

        if (!CanSwitchSegment(vehicle, ref state))
            return false;

        var nextSegment = TrySetNextLane(vehicle, randomSeed);

        vehicle.ValueRW.CurrentSegment = nextSegment;
        vehicle.ValueRW.T = 0;

        return true;
    }

    private Entity TrySetNextLane(RefRW<Vehicle> vehicle, int randomSeed)
    {
        _connectionLookup.TryGetBuffer(vehicle.ValueRO.CurrentSegment, out var connections);
        if (connections.Length == 0)
            return Entity.Null;

        var random = new Random((uint)randomSeed);
        var randomIndex = random.NextInt(connections.Length);

        return connections[randomIndex].ConnectedSegment;
    }
}
