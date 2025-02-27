#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrafficLightAuthoring))]
public class TrafficLightEditor : Editor
{
    private TrafficLightAuthoring _trafficLight;

    private readonly List<LaneAuthoring> _bakedLanes = new();

    private void OnEnable()
    {
        _trafficLight = (TrafficLightAuthoring)target;
        _trafficLight.OnValidated += OnValidate;
    }

    private void OnSceneGUI()
    {
        Visualize(_trafficLight);

    }

    private void OnValidate()
    {
        foreach (var _bakedLanes in _bakedLanes)
        {
            _bakedLanes.TrafficLight = null;
        }
        _bakedLanes.Clear();

        foreach (var lane in _trafficLight.Lanes)
        {
            lane.TrafficLight = _trafficLight;
            _bakedLanes.Add(lane);
        }
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

    private void OnDisable()
    {
        _trafficLight.OnValidated -= OnValidate;
    }
}
#endif