#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrafficLightAuthoring))]
public class TrafficLightEditor : Editor
{
    private TrafficLightAuthoring _trafficLight;

    private List<SegmentAuthoring> _controlledSegmentsCache;

    private void OnEnable()
    {
        _trafficLight = (TrafficLightAuthoring)target;
        _trafficLight.OnValidated += OnValidate;

        _controlledSegmentsCache = new List<SegmentAuthoring>(_trafficLight.ControlledSegments);
    }

    private void OnSceneGUI()
    {
        Visualize(_trafficLight);
    }

    private void OnValidate()
    {
        foreach (var _bakedSegments in _controlledSegmentsCache)
        {
            _bakedSegments.AssociatedTrafficLight = null;
        }
        _controlledSegmentsCache.Clear();

        foreach (var segment in _trafficLight.ControlledSegments)
        {
            segment.AssociatedTrafficLight = _trafficLight;
            _controlledSegmentsCache.Add(segment);
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