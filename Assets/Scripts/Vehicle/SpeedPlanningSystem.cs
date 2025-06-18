using Bezier;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SpeedPlanningSystem : ISystem
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
            var remainingDistance = math.distance(BezierUtilities.EvaluateCubicBezier(segment, 1), BezierUtilities.EvaluateCubicBezier(segment, vehicle.ValueRO.T));
            var minimumBrakingDistance = (vehicle.ValueRO.CurrentSpeed * vehicle.ValueRO.CurrentSpeed) / (2f * SpeedControlSystem.BrakingPowerPerSecond) + TrafficLightStopGap;

            vehicle.ValueRW.SpeedToReach = segment.SpeedLimit;

            if (remainingDistance > minimumBrakingDistance)
                continue;

            if (segment.IsDeadEnd)
            {
                vehicle.ValueRW.SpeedToReach = 0;
                continue;
            }

            if (segment.AssociatedTrafficLight == Entity.Null)
            {
                continue;
            }

            var trafficLight = SystemAPI.GetComponent<TrafficLight>(segment.AssociatedTrafficLight);
            if (trafficLight.Signal != TrafficLightSignal.Green)
            {
                vehicle.ValueRW.SpeedToReach = 0;
            }
        }
    }
}
