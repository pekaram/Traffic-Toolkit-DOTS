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

        float deltaTime = SystemAPI.Time.DeltaTime;

        foreach (var (controller, entity) in SystemAPI.Query<RefRW<TrafficController>>().WithEntityAccess())
        {
            _controlledTrafficLightLookup.TryGetBuffer(entity, out var lightsBuffer);

            controller.ValueRW.ElapsedTime += deltaTime;

            var phaseDuration = controller.ValueRO.CycleTime / lightsBuffer.Length;
            var currentPhase = (int)(controller.ValueRW.ElapsedTime / phaseDuration) % lightsBuffer.Length;

            for (int i = 0; i < lightsBuffer.Length; i++)
            {
                var lightEntity = lightsBuffer[i].Entity; 
                var trafficLight = SystemAPI.GetComponent<TrafficLight>(lightEntity);

                trafficLight.CurrentState = (i == currentPhase) ? 1 : 0;

                SystemAPI.SetComponent(lightEntity, trafficLight);
            }

            if (controller.ValueRW.ElapsedTime >= controller.ValueRW.CycleTime)
            {
                 controller.ValueRW.ElapsedTime = 0;
            }
        }
    }
}
