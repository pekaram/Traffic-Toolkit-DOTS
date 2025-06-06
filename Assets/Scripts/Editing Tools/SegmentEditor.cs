#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SegmentAuthoring)), CanEditMultipleObjects]
public class SegmentEditor : Editor
{
    private SegmentAuthoring _segment;
    private Vector3? _pendingStart = null;

    private TrafficLightAuthoring _cachedTrafficLight;

    private void OnEnable()
    {
        _segment = (SegmentAuthoring)target;
    }

    public void OnSceneGUI()
    {
        HandleInput(Event.current);
        DrawPreview();

        PositionSegmentHandles(_segment);
        DrawSegmentConnections(_segment);

        if (_cachedTrafficLight != _segment.AssociatedTrafficLight)
        {
            SyncTrafficLight();
        }

        if (_segment.AssociatedTrafficLight)
        {
            TrafficLightEditor.Visualize(_segment.AssociatedTrafficLight);
        }
    }

    private void SyncTrafficLight()
    {
        if (_cachedTrafficLight != null)
        {
            _cachedTrafficLight.ControlledSegments.Remove(_segment);
        }

        if (_segment.AssociatedTrafficLight != null && !_segment.AssociatedTrafficLight.ControlledSegments.Contains(_segment))
        {
            _segment.AssociatedTrafficLight.ControlledSegments.Add(_segment);
        }

        _cachedTrafficLight = _segment.AssociatedTrafficLight;
    }


    private void HandleInput(Event e)
    {
        if (_pendingStart != null &&  e.keyCode == KeyCode.Escape)
        {
            _pendingStart = null;
            return;
        }

        if (e.type != EventType.MouseDown || e.button != 0)
            return;

        if (_pendingStart == null && !e.shift)
            return;

        e.Use();

        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);

        if (!plane.Raycast(ray, out float distance))
            return;

        var hitPoint = ray.GetPoint(distance);

        if (_pendingStart == null)
        {
            _pendingStart = hitPoint;
            return;
        }

        CreateSegment(_pendingStart.Value, hitPoint);

        _pendingStart = null;
    }

    private void CreateSegment(Vector3 start, Vector3 end)
    {
        Undo.RecordObject(_segment, "Set Segment Points");

        _segment.Start = InverseTransformPoint(_segment, start);
        _segment.End = InverseTransformPoint(_segment, end);

        var direction = (_segment.End - _segment.Start).normalized;
        var length = Vector3.Distance(_segment.Start, _segment.End) / 3f;
        _segment.StartTangent = _segment.Start + direction * length;
        _segment.EndTangent = _segment.End -direction * length;

        EditorUtility.SetDirty(_segment);
    }

    private void PositionSegmentHandles(SegmentAuthoring segment)
    {
        EditorGUI.BeginChangeCheck();

        var newStart = Handles.PositionHandle(segment.WorldStart, Quaternion.identity);
        var newEnd = Handles.PositionHandle(segment.WorldEnd, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(segment, "Move Bezier Handles");

            segment.Start = InverseTransformPoint(segment, newStart);
            segment.End = InverseTransformPoint(segment, newEnd);
        }

        var direction = (segment.End - segment.Start).normalized;
        var length = Vector3.Distance(segment.Start, segment.End) / 3f;
        segment.StartTangent = segment.Start + direction * length;
        segment.EndTangent = segment.End - direction * length;

        DrawSegment(segment);
    }

    private static void DrawSegment(SegmentAuthoring segment)
    {
        Handles.color = Color.green;
        Handles.DrawLine(segment.WorldStart, segment.WorldEnd);
    
        DrawSegmentDirection(segment.WorldStart, segment.WorldEnd);
    }

    private static void DrawSegmentConnections(SegmentAuthoring segment)
    {
        foreach (var connection in segment.ConnectedSegments)
        {
            if (connection.EndPoint == null) 
                continue;

            var (startTangent, endTangent) = GenerateTangents(segment, connection.EndPoint, connection.fromT, connection.toT);
            connection.StartTangent = startTangent;
            connection.EndTangent = endTangent;

            DrawConnection(segment, connection);
        }
    }

    /// <summary>
    /// workload for translating T value linearly based on distance
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="distance"></param>
    /// <returns></returns>
    private static float TranslateTLinearly(SegmentAuthoring segment, float distance)
    {
        var totalLength = Vector3.Distance(segment.Start, segment.End);
        if (totalLength < distance)
        {
            Debug.LogError("Segment has zero length, cannot translate T.");
            return 1;
        }

        var t = distance / totalLength;
        return t;
    }

    private static (Vector3 startTangent, Vector3 endTangent) GenerateTangents(SegmentAuthoring segment, SegmentAuthoring segment2, float aT, float bT)
    {
        // TODO: [MTS-49] use distance between segments to calculate a variable T  
        const float TipsSamplingT = 0.025f;

        var segmentA = segment;
        var segmentB = segment2;
        var connectionStartPosition = EvaluateCubicBezier(segmentA.WorldStart, segmentA.WorldStartTangent, segmentA.WorldEndTangent, segmentA.WorldEnd, aT);
        var connectionEndPosition = EvaluateCubicBezier(segmentB.WorldStart, segmentB.WorldStartTangent, segmentB.WorldEndTangent, segmentB.WorldEnd, bT);

        var preSegmentAEnd = EvaluateCubicBezier(segmentA.WorldStart, segmentA.WorldStartTangent, segmentA.WorldEndTangent, segmentA.WorldEnd, aT - TipsSamplingT);
        var postSegmentBStart = EvaluateCubicBezier(segmentB.WorldStart, segmentB.WorldStartTangent, segmentB.WorldEndTangent, segmentB.WorldEnd, bT + TipsSamplingT);

        var segmentADirection = (connectionStartPosition - preSegmentAEnd).normalized;
        var segmentBDirection = (postSegmentBStart - connectionEndPosition).normalized;

        var tangentDistance = Vector3.Distance(connectionStartPosition, connectionEndPosition) / 2f;
        var startTangent = connectionStartPosition + segmentADirection * tangentDistance;
        var endTangent = connectionEndPosition - segmentBDirection * tangentDistance;

        startTangent = InverseTransformPoint(segmentA, startTangent);
        endTangent = InverseTransformPoint(segmentB, endTangent);

        return (startTangent, endTangent);
    }

    private static void DrawConnection(SegmentAuthoring fromSegment, SegmentAuthoringConnection connection)
    {
        var segmentA = fromSegment;
        var segmentB = connection.EndPoint;

        var start = EvaluateCubicBezier(segmentA.WorldStart, segmentA.WorldStartTangent, segmentA.WorldEndTangent, segmentA.WorldEnd, connection.fromT);
        var end = EvaluateCubicBezier(segmentB.WorldStart, segmentB.WorldStartTangent, segmentB.WorldEndTangent, segmentB.WorldEnd, connection.toT);
        var startTangent = TransformPoint(segmentA, connection.StartTangent);
        var endTangent = TransformPoint(segmentB, connection.EndTangent);

        Handles.color = Color.yellow;
        Handles.DrawBezier(start, end, startTangent, endTangent, Color.yellow, null, 3f);
        DrawSegment(segmentB);
    }

    private void DrawPreview()
    {
        if (_pendingStart == null)
            return;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);

        if (!plane.Raycast(ray, out float distance))
            return;

        var previewEnd = ray.GetPoint(distance);
        Handles.color = new Color(1f, 1f, 1f, 0.5f);
        Handles.DrawDottedLine(_pendingStart.Value, previewEnd, 5f);
        SceneView.RepaintAll();
    }

    private static void DrawSegmentDirection(Vector3 start, Vector3 end)
    {
        if (start.Equals(end))
            return; 

        Handles.color = Color.yellow;
        Handles.ArrowHandleCap(0, start, Quaternion.LookRotation(end - start), 3, EventType.Repaint);
    }

    private static Vector3 EvaluateCubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
    {
        float u = 1 - t;
        return
            u * u * u * p0 +
            3 * u * u * t * p1 +
            3 * u * t * t * p2 +
            t * t * t * p3;
    }

    private static Vector3 InverseTransformPoint(SegmentAuthoring segment, Vector3 worldPosition)
    {
        var localPosition = segment.transform.InverseTransformPoint(worldPosition);
        // TODO: [MTS-28] basic y clamp to lane orientation, revisit with elevation support
        return new Vector3(localPosition.x, 0, localPosition.z);
    }

    private static Vector3 TransformPoint(SegmentAuthoring segment, Vector3 localPosition)
    {
        return segment.transform.TransformPoint(localPosition);
    }

    private static void UpdateWorldCoordinates(SegmentAuthoring _segment)
    {
        _segment.WorldEnd = TransformPoint(_segment, _segment.End);
        _segment.WorldStart = TransformPoint(_segment, _segment.Start);
        _segment.WorldStartTangent = TransformPoint(_segment, _segment.StartTangent);
        _segment.WorldEndTangent = TransformPoint(_segment, _segment.EndTangent);

        foreach (var connection in _segment.ConnectedSegments)
        {
            if (connection.EndPoint == null)
                continue;

            connection.WorldSegment.Start = EvaluateCubicBezier(_segment.WorldStart, _segment.WorldStartTangent, _segment.WorldEndTangent, _segment.WorldEnd, connection.fromT);
            connection.WorldSegment.StartTangent = TransformPoint(_segment, connection.StartTangent);
            connection.WorldSegment.EndTangent = TransformPoint(connection.EndPoint, connection.EndTangent);
            connection.WorldSegment.End = EvaluateCubicBezier(connection.EndPoint.WorldStart, connection.EndPoint.WorldStartTangent, connection.EndPoint.WorldEndTangent, connection.EndPoint.WorldEnd, connection.toT);
        }
    }

    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Active)]
    private static void DrawSegmentGizmo(SegmentAuthoring segment, GizmoType gizmoType)
    {
        DrawSegment(segment);
        DrawSegmentConnections(segment);
        UpdateWorldCoordinates(segment);
    }
}
#endif
