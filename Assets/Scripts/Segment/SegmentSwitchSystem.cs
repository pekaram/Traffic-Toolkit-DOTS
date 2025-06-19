using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentSwitchSystem : ISystem
{
    private const float RequiredGapDistance = 20;

    private const float SegmentSwitchDistance = 0.05f;

    private BufferLookup<ConnectionPoint> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _connectionLookup = state.GetBufferLookup<ConnectionPoint>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _connectionLookup.Update(ref state);
        foreach (var (vehicle, entity) in SystemAPI.Query<RefRW<Vehicle>>().WithEntityAccess())
        {
            if (vehicle.ValueRO.CurrentSegment == Entity.Null)
                continue;

            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);

            if (vehicle.ValueRO.T >= 1)
            {
                TryEnterNext(ref state, vehicle, entity.Index * 100000);
                continue;
            }

            if (vehicle.ValueRO.DriverSpeedBias > 1)
            {
                TryChangeToAdjacent(ref state, vehicle, ConnectionType.LeftAdjacent);
                continue;
            }

            if (vehicle.ValueRO.DriverSpeedBias < 0.9f)
            {
                TryChangeToAdjacent(ref state, vehicle, ConnectionType.RightAdjacent);
            }
        }
    }
    
    private bool TryEnterNext(ref SystemState state, RefRW<Vehicle> vehicle, int randomSeed)
    {
        if (vehicle.ValueRO.CurrentSegment == Entity.Null)
            return false;

        var connectionPoint = GetRandomIntersectionPoint(vehicle, randomSeed);
        if (connectionPoint.ConnectedSegmentEntity == Entity.Null)
            return false;

        vehicle.ValueRW.CurrentSegment = connectionPoint.ConnectedSegmentEntity;
        vehicle.ValueRW.T = connectionPoint.ConnectedSegmentT;
        return true;
    }

    private void TryChangeToAdjacent(ref SystemState state, RefRW<Vehicle> mergingVehicle, ConnectionType direction)
    {
        var connection = GetNearestAdjacentPoint(ref state, mergingVehicle.ValueRO, direction);
        if(connection.ConnectedSegmentEntity == Entity.Null)
            return;

        var hasGap = HasEnoughGap(ref state, mergingVehicle.ValueRO, connection);
        if (!hasGap)
            return;

        mergingVehicle.ValueRW.CurrentSegment = connection.ConnectedSegmentEntity;
        mergingVehicle.ValueRW.T = connection.ConnectedSegmentT;
    }

    private bool HasEnoughGap(ref SystemState state, in Vehicle mergingVehicle, in ConnectionPoint connectionStart)
    {
        var connectorSegment = SystemAPI.GetComponent<Segment>(connectionStart.ConnectedSegmentEntity);
        _connectionLookup.TryGetBuffer(connectionStart.ConnectedSegmentEntity, out var connectionEndpoints);
        var connectionEnd = connectionEndpoints[0];
        var newSegment = SystemAPI.GetComponent<Segment>(connectionEnd.ConnectedSegmentEntity);

        var vehicleSegment = SystemAPI.GetComponent<Segment>(mergingVehicle.CurrentSegment);

        foreach (var otherVehicle in SystemAPI.Query<RefRO<Vehicle>>())
        {
            if (!connectionEnd.ConnectedSegmentEntity.Equals(otherVehicle.ValueRO.CurrentSegment))
                continue;

            var mergingSpeed = mergingVehicle.SpeedToReach;
            var start = BezierUtilities.EvaluateCubicBezier(connectorSegment, 0);
            var destination = BezierUtilities.EvaluateCubicBezier(connectorSegment, 1);
            var direction = math.normalize(destination - start);
            var travelDistance = math.distance(destination, start);
            var travelTime = travelDistance / mergingSpeed;

            var predictedOtherVehicleT = BezierUtilities.TranslateT(newSegment, otherVehicle.ValueRO.T, otherVehicle.ValueRO.CurrentSpeed * travelTime);
            var predictedOtherVehiclePosition = BezierUtilities.EvaluateCubicBezier(newSegment, predictedOtherVehicleT); 
            if (math.distance(destination, predictedOtherVehiclePosition) < RequiredGapDistance)
                return false;
        }

        return true;
    }

    private ConnectionPoint GetNearestAdjacentPoint(ref SystemState state, Vehicle vehicle, ConnectionType direction)
    {
        _connectionLookup.TryGetBuffer(vehicle.CurrentSegment, out var connections);
        var vehicleSegment = SystemAPI.GetComponent<Segment>(vehicle.CurrentSegment);

        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            if (connection.Type != direction)
                continue;

            var distanceToMergingPoint = math.distance(
                BezierUtilities.EvaluateCubicBezier(vehicleSegment, vehicle.T),
                BezierUtilities.EvaluateCubicBezier(vehicleSegment, connection.TransitionT));

            if (distanceToMergingPoint > SegmentSwitchDistance)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            return connection;
        }

        return default;
    }

    private ConnectionPoint GetRandomIntersectionPoint(RefRW<Vehicle> vehicle, int randomSeed)
    {
        _connectionLookup.TryGetBuffer(vehicle.ValueRO.CurrentSegment, out var connections);
        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            if (connection.TransitionT < 1)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            var random = new Random((uint)randomSeed);
            var randomIndex = random.NextInt(i, connections.Length);
            return connections[randomIndex];
        }

        return default;
    }
}
