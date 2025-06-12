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
                TrySwitchToNextLane(ref state, vehicle, entity.Index * 100000);
                continue;
            }

            if (vehicle.ValueRO.DesiredSpeed > segment.SpeedLimit)
            {
                TryMergeIntoFasterLane(ref state, vehicle);
            }
        }
    }

    private bool IsWaitingForGreen(RefRW<Vehicle> vehicle, ref SystemState state)
    {
        var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);

        if (segment.AssociatedTrafficLight == Entity.Null)
            return false;

        var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
        return trafficLight.Signal == TrafficLightSignal.Red;
    }

    private bool TrySwitchToNextLane(ref SystemState state, RefRW<Vehicle> vehicle, int randomSeed)
    {
        if (vehicle.ValueRO.CurrentSegment == Entity.Null)
            return false;

        if (IsWaitingForGreen(vehicle, ref state))
            return false;

        var connectionPoint = GetRandomConnectionPoint(vehicle, randomSeed);
        if (connectionPoint.ConnectedSegmentEntity == Entity.Null)
            return false;

        vehicle.ValueRW.CurrentSegment = connectionPoint.ConnectedSegmentEntity;
        vehicle.ValueRW.T = connectionPoint.ConnectedSegmentT;

        var speedLimit = (int)SystemAPI.GetComponent<Segment>(connectionPoint.ConnectedSegmentEntity).SpeedLimit;
        var random = new Random((uint)randomSeed);
        var desiredSpeed = random.NextInt(speedLimit - 1, speedLimit + 2);
        vehicle.ValueRW.DesiredSpeed = desiredSpeed;

        return true;
    }

    private void TryMergeIntoFasterLane(ref SystemState state, RefRW<Vehicle> mergingVehicle)
    {
        var connection = GetLeftAdacentConnector(ref state, mergingVehicle.ValueRO);
        if(connection.ConnectedSegmentEntity == Entity.Null)
            return;

        var hasGap = FindGap(ref state, mergingVehicle.ValueRO, connection);
        if (!hasGap)
            return;

        mergingVehicle.ValueRW.CurrentSegment = connection.ConnectedSegmentEntity;
        mergingVehicle.ValueRW.T = connection.ConnectedSegmentT;
    }

    private bool FindGap(ref SystemState state, Vehicle mergingVehicle, in ConnectionPoint connectionStart)
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

            var mergingSpeed = mergingVehicle.DesiredSpeed;
            var start = EvaluateCubicBezier(connectorSegment, 0);
            var destination = EvaluateCubicBezier(connectorSegment, 1);
            var direction = math.normalize(destination - start);
            var travelDistance = math.distance(destination, start);
            var travelTime = travelDistance / mergingSpeed;

            var predictedOtherVehicleT = TranslateT(newSegment, otherVehicle.ValueRO.T, otherVehicle.ValueRO.CurrentSpeed * travelTime);
            var predictedOtherVehiclePosition = EvaluateCubicBezier(newSegment, predictedOtherVehicleT); 
            if (math.distance(destination, predictedOtherVehiclePosition) < RequiredGapDistance)
            {
                return false;
            }
        }

        return true;
    }

    private ConnectionPoint GetLeftAdacentConnector(ref SystemState state, Vehicle vehicle)
    {
        _connectionLookup.TryGetBuffer(vehicle.CurrentSegment, out var connections);
        var vehicleSegment = SystemAPI.GetComponent<Segment>(vehicle.CurrentSegment);

        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            if (connection.Type != 1)
                continue;

            var distanceToMergingPoint = math.distance(
                EvaluateCubicBezier(vehicleSegment, vehicle.T),
                EvaluateCubicBezier(vehicleSegment, connection.TransitionT));

            if (distanceToMergingPoint > SegmentSwitchDistance)
                continue;

            // Assumes buffer is sorted by T, rest of the connections are >= T
            return connection;
        }

        return default;
    }

    private ConnectionPoint GetRandomConnectionPoint(RefRW<Vehicle> vehicle, int randomSeed)
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

    private static float TranslateT(Segment segment, float t, float targetDistance)
    {
        const int steps = 100;
        var newPosition = EvaluateCubicBezier(segment, t);
        var oldPosition = newPosition;
        for (var step = t * steps; step <= steps + 1; step += 1)
        {
            t = step / steps;
            if (t > 1)
                return 1;

            newPosition = EvaluateCubicBezier(segment, t);
            var steppedDistance = math.distance(newPosition, oldPosition);
            if (steppedDistance < targetDistance)
                continue;

            var previousT = (step - 1) / steps;
            var previousPosition = EvaluateCubicBezier(segment, previousT);
            var previousDistance = math.distance(oldPosition, previousPosition);

            var ratio = (targetDistance - previousDistance) / (steppedDistance - previousDistance);
            var interpretedT = math.lerp(previousT, t, ratio);

            return interpretedT;
        }

        UnityEngine.Debug.LogError("Failed to Translate T");
        return t;
    }

    private static float3 EvaluateCubicBezier(Segment segment, float t)
    {
        var p0 = segment.Start;
        var p1 = segment.StartTangent;
        var p2 = segment.EndTangent;
        var p3 = segment.End;

        var u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }
}
