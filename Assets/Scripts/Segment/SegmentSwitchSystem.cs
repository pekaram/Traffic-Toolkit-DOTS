using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentSwitchSystem : ISystem
{
    private const float RequiredGapDistance = 10;

    private const float SegmentSwitchDistance = 0.05f;

    private BufferLookup<ConnectorElementData> _connectionLookup;

    public void OnCreate(ref SystemState state)
    {
        _connectionLookup = state.GetBufferLookup<ConnectorElementData>(true);
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
                if (TryEnterNext(ref state, vehicle, entity.Index * 100000))
                {
                   SystemAPI.SetComponentEnabled<MergingPlan>(entity, false);
                }

                continue;
            }
        
            var isInConnector = SystemAPI.HasComponent<Connector>(vehicle.ValueRO.CurrentSegment);
            if (isInConnector)
                continue;

            var isMerging = TryChangeToAdjacent(ref state, vehicle, ConnectionType.LeftAdjacent);

            //if (vehicle.ValueRO.DriverSpeedBias > 1)
            //{
            //    TryChangeToAdjacent(ref state, vehicle, ConnectionType.LeftAdjacent);
            //    continue;
            //}

            //if (vehicle.ValueRO.DriverSpeedBias < 0.9f)
            //{
            //    TryChangeToAdjacent(ref state, vehicle, ConnectionType.RightAdjacent);
            //}

            if (isMerging)
            {
                UpdateMergePlan(ref state, entity, vehicle.ValueRO);
            }
        }
    }

    private void UpdateMergePlan(ref SystemState state, Entity meringVehicleEntity, Vehicle vehicle)
    {
        var connector = SystemAPI.GetComponent<Connector>(vehicle.CurrentSegment);
        var segmentToEnter = connector.SegmentB;
        SystemAPI.SetComponent(meringVehicleEntity, new MergingPlan() { SegmentToJoin = segmentToEnter });
        SystemAPI.SetComponentEnabled<MergingPlan>(meringVehicleEntity, true);
    }

    private bool TryEnterNext(ref SystemState state, RefRW<Vehicle> vehicle, int randomSeed)
    {
        if (vehicle.ValueRO.CurrentSegment == Entity.Null)
            return false;

        var isConnector = SystemAPI.HasComponent<Connector>(vehicle.ValueRO.CurrentSegment);
        if (isConnector)
        {
            var connector = SystemAPI.GetComponent<Connector>(vehicle.ValueRO.CurrentSegment);
            vehicle.ValueRW.CurrentSegment = connector.SegmentB;
            vehicle.ValueRW.T = connector.MergeT;
            return true;
        }

        var connectorEntity = GetAnyIntersectionConnectionToJoin(ref state, vehicle, randomSeed);
        if (connectorEntity == Entity.Null)
            return false;

        vehicle.ValueRW.CurrentSegment = connectorEntity;
        vehicle.ValueRW.T = 0;
        return true;
    }

    private bool TryChangeToAdjacent(ref SystemState state, RefRW<Vehicle> mergingVehicle, ConnectionType direction)
    {
        var connectionEntity = GetNearestAdjacentConnector(ref state, mergingVehicle.ValueRO, direction);
        if (connectionEntity == Entity.Null)
            return false;

        var hasGap = WillHaveGap(ref state, mergingVehicle.ValueRO, connectionEntity);
        if (!hasGap)
            return false;

        mergingVehicle.ValueRW.CurrentSegment = connectionEntity;
        mergingVehicle.ValueRW.T = 0;
        return true;
    }

    private bool WillHaveGap(ref SystemState state, in Vehicle mergingVehicle, in Entity connectorSegmentEntity)
    {
        var connectorSegment = SystemAPI.GetComponent<Segment>(connectorSegmentEntity);
        var connector = SystemAPI.GetComponent<Connector>(connectorSegmentEntity);
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
            if (math.distance(destination, predictedOtherVehiclePosition) < RequiredGapDistance)
            {
                if (connector.Type == ConnectionType.Join)
                {
                    UnityEngine.Debug.LogError("Join connections are false" + " " + mergingVehicle.T + " SPEED:" + mergingVehicle.SpeedToReach);

                }
                return false;
            }
        }

        return true;
    }

    private Entity GetNearestAdjacentConnector(ref SystemState state, Vehicle vehicle, ConnectionType direction)
    {
        _connectionLookup.TryGetBuffer(vehicle.CurrentSegment, out var connections);
        var vehicleSegment = SystemAPI.GetComponent<Segment>(vehicle.CurrentSegment);

        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connectionEntity = connections[i];
            var connection = SystemAPI.GetComponent<Connector>(connectionEntity.ConnectorSegmentEntity); 
            if (connection.Type != direction)
                continue;

            var distanceToMergingPoint = math.distance(
                BezierUtilities.EvaluateCubicBezier(vehicleSegment, vehicle.T),
                BezierUtilities.EvaluateCubicBezier(vehicleSegment, connection.TransitionT));

            if (distanceToMergingPoint > SegmentSwitchDistance)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            return connectionEntity.ConnectorSegmentEntity;
        }

        return default;
    }

    private Entity GetAnyIntersectionConnectionToJoin(ref SystemState state, RefRW<Vehicle> vehicle, int randomSeed)
    {
        _connectionLookup.TryGetBuffer(vehicle.ValueRO.CurrentSegment, out var connections);
        if (connections.Length == 0)
        {
            return Entity.Null;
        }    
        // Oh must be a segment with many connections, 
        for (var i = 0; i < connections.Length; i++)
        {
            var connectorEntity = connections[i].ConnectorSegmentEntity;
            var connector = SystemAPI.GetComponent<Connector>(connectorEntity);
            if (connector.Type != ConnectionType.Intersection)
                continue;

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
