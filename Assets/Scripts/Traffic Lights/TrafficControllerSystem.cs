using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct TrafficControllerSystem : ISystem
{
    private BufferLookup<ControlledTrafficLight> _controlledTrafficLightLookup;

    public void OnCreate(ref SystemState state)
    {
        _controlledTrafficLightLookup = state.GetBufferLookup<ControlledTrafficLight>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _controlledTrafficLightLookup.Update(ref state);

        var deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (controller, entity) in SystemAPI.Query<RefRW<TrafficController>>().WithEntityAccess())
        {
            _controlledTrafficLightLookup.TryGetBuffer(entity, out var lightsBuffer);

            controller.ValueRW.ElapsedTime += deltaTime;
            if (controller.ValueRW.ElapsedTime >= controller.ValueRW.CycleTime)
            {
                controller.ValueRW.ElapsedTime = 0;
            }

            var phaseDuration = controller.ValueRO.CycleTime / lightsBuffer.Length;
            var currentPhase = (int)(controller.ValueRW.ElapsedTime / phaseDuration) % lightsBuffer.Length;

            var elaspsedPhaseTime = controller.ValueRW.ElapsedTime - (phaseDuration * currentPhase);
            var isYellow = elaspsedPhaseTime / phaseDuration > 1 - controller.ValueRO.YellowSignalPercentage;

            for (int i = 0; i < lightsBuffer.Length; i++)
            {
                var lightEntity = lightsBuffer[i].Entity; 
                var trafficLight = SystemAPI.GetComponent<TrafficLight>(lightEntity);

                trafficLight.Signal = (i == currentPhase) ? !isYellow ? TrafficLightSignal.Green : TrafficLightSignal.Yellow: TrafficLightSignal.Red;
                SystemAPI.SetComponent(lightEntity, trafficLight);
            }
        }
    }
}
