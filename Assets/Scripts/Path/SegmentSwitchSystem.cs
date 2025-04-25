using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
        foreach (var (vehicle, transform) in SystemAPI.Query<RefRW<VehicleV2>, RefRW<LocalTransform>>())
        {
            if (vehicle.ValueRO.T < 1)
                continue;

            TrySwitchToNextLane(vehicle, ref state);
        }
    }

    private bool CanSwitchSegment(RefRW<VehicleV2> vehicle, ref SystemState state)
    {
        var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);

        if (segment.AssociatedTrafficLight == Entity.Null)
            return true;

        var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
        return trafficLight.Signal == TrafficLightSignal.Green;
    }

    private bool TrySwitchToNextLane(RefRW<VehicleV2> vehicle, ref SystemState state)
    {
        if (vehicle.ValueRO.CurrentSegment == Entity.Null)
            return false;

        if (!CanSwitchSegment(vehicle, ref state))
            return false;

        var nextSegment = TrySetNextLane(vehicle, 1);

        vehicle.ValueRW.CurrentSegment = nextSegment;
        vehicle.ValueRW.T = 0;

        return true;
    }

    private Entity TrySetNextLane(RefRW<VehicleV2> vehicle, int index)
    {
        _connectionLookup.TryGetBuffer(vehicle.ValueRO.CurrentSegment, out var connections);
        if (connections.Length == 0)
            return Entity.Null;

        var random = new Random((uint)index * 100000);
        var randomIndex = random.NextInt(connections.Length);

        return connections[randomIndex].ConnectedSegment;
    }

}
