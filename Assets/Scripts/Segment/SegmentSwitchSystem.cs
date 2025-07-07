using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentSwitchSystem : ISystem
{
    private const float RequiredMergeGapDistance = 30;

    private const float SwitchToleranceDistance = 0.05f;

    private BufferLookup<ConnectorSegmentEntity> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _connectionLookup = state.GetBufferLookup<ConnectorSegmentEntity>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, vehicleEntity) in SystemAPI.Query<RefRW<Vehicle>>().WithEntityAccess())
        {
            if (vehicle.ValueRO.CurrentSegment == Entity.Null)
                continue;

            if (vehicle.ValueRO.T >= 1)
            {
                OnSegmentConcluded(ref state, vehicle, vehicleEntity);
                continue;
            }
        
            var isInConnector = SystemAPI.HasComponent<Connector>(vehicle.ValueRO.CurrentSegment);
            if (isInConnector)
                continue;

            TrySpeedBiasedMerge(ref state, vehicle, vehicleEntity);
        }
    }

    private void OnSegmentConcluded(ref SystemState state, RefRW<Vehicle> vehicle, Entity vehicleEntity)
    {
        SystemAPI.SetComponentEnabled<MergeTag>(vehicleEntity, false);

        EnterNewSegment(ref state, vehicle, vehicleEntity);

        if (!SystemAPI.HasComponent<Connector>(vehicle.ValueRO.CurrentSegment))
            return;

        var connector = SystemAPI.GetComponent<Connector>(vehicle.ValueRO.CurrentSegment);
        if (connector.Type != ConnectionType.ZipperMerge)
            return;

        SystemAPI.SetComponentEnabled<MergeTag>(vehicleEntity, true);
    }

    private bool TrySpeedBiasedMerge(ref SystemState state, RefRW<Vehicle> vehicle, Entity vehicleEntity)
    {
        var segmentEntity = vehicle.ValueRO.CurrentSegment;
        var t = vehicle.ValueRO.T;
        var connectionEntity = Entity.Null;

        if (vehicle.ValueRO.DriverSpeedBias > 1)
        {
            connectionEntity = GetConnectionByT(ref state, segmentEntity, ConnectionType.LeftAdjacent, t, SwitchToleranceDistance);
        }
        if (vehicle.ValueRO.DriverSpeedBias < 0.9f)
        {
            connectionEntity = GetConnectionByT(ref state, segmentEntity, ConnectionType.RightAdjacent, t, SwitchToleranceDistance);
        }

        if (connectionEntity == Entity.Null)
            return false;

        var canMerge = ValidateMergeGap(ref state, vehicle.ValueRO, connectionEntity);
        if (!canMerge)
            return false;

        vehicle.ValueRW.CurrentSegment = connectionEntity;
        vehicle.ValueRW.T = 0;
        SystemAPI.SetComponentEnabled<MergeTag>(vehicleEntity, true);
        return true;
    }

    private void EnterNewSegment(ref SystemState state, RefRW<Vehicle> vehicle, Entity vehicleEntity)
    {
        var wasInConnector = SystemAPI.HasComponent<Connector>(vehicle.ValueRO.CurrentSegment);
        if (wasInConnector)
        {
            var connector = SystemAPI.GetComponent<Connector>(vehicle.ValueRO.CurrentSegment);
            vehicle.ValueRW.CurrentSegment = connector.SegmentB;
            vehicle.ValueRW.T = connector.MergeT;
            return;
        }

        var randomSegment = GetConnectionToRandomNewSegment(ref state, vehicle.ValueRO.CurrentSegment, vehicleEntity.Index * 100000);
        vehicle.ValueRW.CurrentSegment = randomSegment;
        vehicle.ValueRW.T = 0;
    }

    private bool ValidateMergeGap(ref SystemState state, in Vehicle mergingVehicle, in Entity mergeSegment)
    {
        var connectorSegment = SystemAPI.GetComponent<Segment>(mergeSegment);
        var connector = SystemAPI.GetComponent<Connector>(mergeSegment);
        var newSegment = SystemAPI.GetComponent<Segment>(connector.SegmentB);

        foreach (var otherVehicle in SystemAPI.Query<RefRO<Vehicle>>())
        {
            if (!connector.SegmentB.Equals(otherVehicle.ValueRO.CurrentSegment))
                continue;

            // Change to segment.speed?
            var mergingSpeed = mergingVehicle.SpeedToReach;

            var start = BezierUtilities.EvaluateCubicBezier(connectorSegment, 0);
            var destination = BezierUtilities.EvaluateCubicBezier(connectorSegment, 1);
            var travelDistance = math.distance(destination, start);
            var travelTime = travelDistance / mergingSpeed;

            var predictedOtherVehicleT = BezierUtilities.TranslateT(newSegment, otherVehicle.ValueRO.T, otherVehicle.ValueRO.CurrentSpeed * travelTime);
            var predictedOtherVehiclePosition = BezierUtilities.EvaluateCubicBezier(newSegment, predictedOtherVehicleT);
            if (math.distance(destination, predictedOtherVehiclePosition) < RequiredMergeGapDistance)
                return false;
        }

        return true;
    }

    private Entity GetConnectionByT(ref SystemState state, Entity segment, ConnectionType type, float t, float distanceTolerance)
    {
        _connectionLookup.TryGetBuffer(segment, out var connections);
        var segmentComponent = SystemAPI.GetComponent<Segment>(segment);

        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connectionEntity = connections[i];
            var connection = SystemAPI.GetComponent<Connector>(connectionEntity.Entity);
            if (connection.Type != type)
                continue;

            var distanceToConnection =
                math.distance(BezierUtilities.EvaluateCubicBezier(segmentComponent, t),
                BezierUtilities.EvaluateCubicBezier(segmentComponent, connection.TransitionT));
            if (distanceToConnection > distanceTolerance)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            return connectionEntity.Entity;
        }

        return default;
    }

    private Entity GetConnectionToRandomNewSegment(ref SystemState state, Entity segment, int randomSeed)
    {
        _connectionLookup.TryGetBuffer(segment, out var connections);
        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connectorEntity = connections[i].Entity;
            var connector = SystemAPI.GetComponent<Connector>(connectorEntity);

            if (connector.TransitionT < 1)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            var random = new Random((uint)randomSeed);
            var randomIndex = random.NextInt(i, connections.Length);
            return connectorEntity;
        }

        return default;
    }
}
