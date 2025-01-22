#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrafficLightAuthoring))]
public class TrafficLightEditor : Editor
{
    private TrafficLightAuthoring _trafficLight;

    private void OnEnable()
    {
        _trafficLight = (TrafficLightAuthoring)target;
    }

    private void OnSceneGUI()
    {
        Visualize(_trafficLight);
    }

    public static void Visualize(TrafficLightAuthoring trafficLight)
    {
        const float sphereSize = 1;
        var colors = new List<Color>() { Color.red, Color.green };

        for (var i = 0; i < colors.Count; i++)
        {
            Handles.color = colors[i];
            Handles.SphereHandleCap(0, trafficLight.transform.position + (i * sphereSize * Vector3.down), Quaternion.identity, sphereSize, EventType.Repaint);
        }
    }
}
#endif