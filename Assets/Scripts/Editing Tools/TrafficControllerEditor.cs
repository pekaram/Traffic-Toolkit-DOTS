#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(TrafficControllerAuthoring))]
public class TrafficControllerEditor : Editor
{
    private TrafficControllerAuthoring _controller;

    private void OnEnable()
    {
        _controller = (TrafficControllerAuthoring)target;
    }

    private void OnSceneGUI()
    {  
        foreach (var trafficLight in _controller.ControlledTrafficLights)
        {
            TrafficLightEditor.Visualize(trafficLight);
        }
    }
}
#endif