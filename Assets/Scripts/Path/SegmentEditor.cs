#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SegmentAuthoring)), CanEditMultipleObjects]
public class SegmentEditor : Editor
{
    private SegmentAuthoring _segment;
    private Vector3? _pendingStart = null;

    private void OnEnable()
    {
        _segment = (SegmentAuthoring)target;
    }

    public void OnSceneGUI()
    {
        HandleInput(Event.current);
        DrawPreview();

        DrawSegment();
        DrawSegmentConnections(_segment);
    }

    private void HandleInput(Event e)
    {
        if (_pendingStart != null &&  e.keyCode == KeyCode.Escape)
        {
            _pendingStart = null;
            return;
        }

        if (e.type != EventType.MouseDown || !(e.button == 0 && e.shift))
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
        var direction = (end - start).normalized;
        var length = Vector3.Distance(start, end) / 3f;

        Undo.RecordObject(_segment, "Set Segment Points");

        _segment.Start = start;
        _segment.End = end;
        _segment.StartTangent = start + direction * length;
        _segment.EndTangent = end -direction * length;

        EditorUtility.SetDirty(_segment);
    }

    private void DrawSegment()
    {
        EditorGUI.BeginChangeCheck();

        var newStart = Handles.PositionHandle(_segment.Start, Quaternion.identity);
        var newStartTangent = Handles.PositionHandle(_segment.StartTangent, Quaternion.identity);
        var newEndTangent = Handles.PositionHandle(_segment.EndTangent, Quaternion.identity);
        var newEnd = Handles.PositionHandle(_segment.End, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(_segment, "Move Bezier Handles");

            _segment.Start = newStart;
            _segment.StartTangent = newStartTangent;
            _segment.EndTangent = newEndTangent;
            _segment.End = newEnd;
        }
 
        Handles.color = Color.green;
        Handles.DrawBezier(newStart, newEnd, newStartTangent, newEndTangent, Color.green, null, 3f);

        Handles.SphereHandleCap(0, newStart, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.SphereHandleCap(0, newStartTangent, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.SphereHandleCap(0, newEndTangent, Quaternion.identity, 0.5f, EventType.Repaint);
        Handles.SphereHandleCap(0, newEnd, Quaternion.identity, 0.5f, EventType.Repaint);
    }

    private void DrawSegmentConnections(SegmentAuthoring segment)
    {
        foreach (var connection in segment.ConnectedSegments)
        {
            if (connection.EndPoint == null) 
                continue;

            DrawConnection(connection);    
        }
    }

    private void DrawConnection(SegmentAuthoringConnection connection)
    {
        const float TipsSamplingDistance = 0.025f;

        var segmentA = _segment;
        var segmentB = connection.EndPoint;

        var preSegmentAEnd = EvaluateCubicBezier(segmentA.Start, segmentA.StartTangent, segmentA.EndTangent, segmentA.End, 1 - TipsSamplingDistance);
        var postSegmentBStart = EvaluateCubicBezier(segmentB.Start, segmentB.StartTangent, segmentB.EndTangent, segmentB.End, 0 + TipsSamplingDistance);

        var segmentADirection = (segmentA.End - preSegmentAEnd).normalized;
        var segmentBDirection = (postSegmentBStart - segmentB.Start).normalized;

        var tangentDistance = Vector3.Distance(segmentA.End, segmentB.Start) / 2f;
        var startTangent = segmentA.End + segmentADirection * tangentDistance;
        var endTangent = segmentB.Start - segmentBDirection * tangentDistance;

        connection.StartTangent = startTangent;
        connection.EndTangent = endTangent;

        Handles.color = Color.yellow;
        Handles.SphereHandleCap(0, segmentB.Start, quaternion.identity, 0.5f, EventType.Repaint); 
        Handles.DrawBezier(segmentA.End, segmentB.Start, connection.StartTangent, connection.EndTangent, Color.yellow, null, 3f);
    }

    private void DrawPreview()
    {
        if (_pendingStart == null)
            return;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);

        if (!plane.Raycast(ray, out float distance))
            return;

        Vector3 previewEnd = ray.GetPoint(distance);
        Handles.color = new Color(1f, 1f, 1f, 0.5f);
        Handles.DrawDottedLine(_pendingStart.Value, previewEnd, 5f);
        SceneView.RepaintAll();
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
}
#endif
