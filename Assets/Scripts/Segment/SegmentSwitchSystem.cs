using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SegmentSwitchSystem : ISystem
{
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

            if (vehicle.ValueRO.MaxSpeed > segment.MaxSpeed)
            {
                // Try to switch to adjacent lane if speed is below max speed
                TryToSwitchToFasterLane(ref state, vehicle);
            }
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

        var connection = GetRandomConnection(vehicle, randomSeed);
        if (connection.ConnectedSegmentEntity == Entity.Null)
            return false;

        vehicle.ValueRW.CurrentSegment = connection.ConnectedSegmentEntity;
        vehicle.ValueRW.T = connection.ConnectedSegmentT;

        return true;
    }

    private void TryToSwitchToFasterLane(ref SystemState state, RefRW<Vehicle> mergingVehicle)
    {
        var connection = GetLeftAdacentConnector(mergingVehicle.ValueRO);
        if(connection.ConnectedSegmentEntity == Entity.Null)
            return;

        var hasGap = FindGap(ref state, mergingVehicle.ValueRO, connection);
        if (!hasGap)
            return;

        //
        mergingVehicle.ValueRW.CurrentSegment = connection.ConnectedSegmentEntity;
        mergingVehicle.ValueRW.T = connection.ConnectedSegmentT;
    }

    private bool FindGap(ref SystemState state, Vehicle mergingVehicle, in ConnectionPoint connectionStart)
    {
        var connectorSegment = SystemAPI.GetComponent<Segment>(connectionStart.ConnectedSegmentEntity);
        _connectionLookup.TryGetBuffer(connectionStart.ConnectedSegmentEntity, out var connectionEndpoints);
        var connectionEnd = connectionEndpoints[0];
        var newSegment = SystemAPI.GetComponent<Segment>(connectionEnd.ConnectedSegmentEntity);

        foreach (var otherVehicle in SystemAPI.Query<RefRO<Vehicle>>())
        {
            if (!connectionEnd.ConnectedSegmentEntity.Equals(otherVehicle.ValueRO.CurrentSegment))
                continue;

            var mergingSpeed = mergingVehicle.MaxSpeed;
            var start = EvaluateCubicBezier(connectorSegment, 0);
            var destination = EvaluateCubicBezier(connectorSegment, 1);
            var travelDistance = math.distance(destination, start);
            var travelTime = travelDistance / mergingSpeed;

            var predictedOtherVehicleT = TranslateT(newSegment, otherVehicle.ValueRO.T, otherVehicle.ValueRO.Speed * travelTime);


            UnityEngine.Debug.LogError(
                $"Found other vehicle in the way. predictedOtherVehicleT={predictedOtherVehicleT}, " +
                $"connectionStart.TransitionT={connectionStart.TransitionT}, " +
                $"connectionEnd.ConnectedSegmentT={connectionEnd.ConnectedSegmentT}"
            );

            if (predictedOtherVehicleT < connectionEnd.ConnectedSegmentT)
                return false;
        }

        return true;
    }

    private ConnectionPoint GetLeftAdacentConnector(Vehicle vehicle)
    {
        _connectionLookup.TryGetBuffer(vehicle.CurrentSegment, out var connections);
        if (connections.Length == 0)
            return default;

        for (var i = 0; i < connections.Length; i++)
        {
            var connection = connections[i];
            if (connection.Type != 1)
                continue;

            if (connection.TransitionT > vehicle.T)
                continue;

            // Check distance  

            // Assumes buffer is sorted by T, rest of the connections are >= T
            return connection;
        }

        return default;
    }

    private ConnectionPoint GetRandomConnection(RefRW<Vehicle> vehicle, int randomSeed)
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
        const int steps = 1000;
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

        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }
}
