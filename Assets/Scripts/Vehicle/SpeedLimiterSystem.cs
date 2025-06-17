using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SpeedLimiterSystem : ISystem
{
    private const float TrafficLightStopGap = 0.5f; 

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var vehicle in SystemAPI.Query<RefRW<Vehicle>>())
        {
            if(vehicle.ValueRW.CurrentSegment == Entity.Null)
                continue;

            var segment = SystemAPI.GetComponent<Segment>(vehicle.ValueRO.CurrentSegment);

            if (segment.AssociatedTrafficLight == Entity.Null)
            {
                // Handle case where there is no next segment or traffic lightq
                vehicle.ValueRW.DesiredSpeed = segment.SpeedLimit;
                continue;
            }

            var remainingDistance = math.distance(BezierUtilities.EvaluateCubicBezier(segment, 1), BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T));
            var brakingDistance = (vehicle.ValueRO.CurrentSpeed * vehicle.ValueRO.CurrentSpeed) / (2f * ActiveCollisionAvoidanceSystem.BrakingPowerPerSecond) + TrafficLightStopGap;
            var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);

            if (remainingDistance <= brakingDistance && trafficLight.Signal != TrafficLightSignal.Green)
            {
                vehicle.ValueRW.DesiredSpeed = 0;
            }
            else
            {       
                vehicle.ValueRW.DesiredSpeed = math.max(vehicle.ValueRO.DesiredSpeed, segment.SpeedLimit);
            }
        }
    }
}
