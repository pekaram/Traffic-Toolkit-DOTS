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
        var startWorld = TransformPoint(segment, segment.Start);
        var endWorld = TransformPoint(segment, segment.End);

        var newStart = Handles.PositionHandle(startWorld, Quaternion.identity);
        var newEnd = Handles.PositionHandle(endWorld, Quaternion.identity);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(segment, "Move Bezier Handles");

            segment.Start = InverseTransformPoint(segment, newStart);
            segment.End = InverseTransformPoint(segment, newEnd);

            var direction = (segment.End - segment.Start).normalized;
            var length = Vector3.Distance(segment.Start, segment.End) / 3f;
            segment.StartTangent = segment.Start + direction * length;
            segment.EndTangent = segment.End - direction * length;
        }

        DrawSegment(segment);

    }

    private static void DrawSegment(SegmentAuthoring segment)
    {
        var startWorld = TransformPoint(segment, segment.Start);
        var startTangentWorld = TransformPoint(segment, segment.StartTangent);
        var endTangentWorld = TransformPoint(segment, segment.EndTangent);
        var endWorld = TransformPoint(segment, segment.End);

        Handles.color = Color.green;
        Handles.DrawLine(endWorld, startWorld);
    
        DrawSegmentDirection(startWorld, endWorld);
    }

    private static void DrawSegmentConnections(SegmentAuthoring segment)
    {
        foreach (var connection in segment.ConnectedSegments)
        {
            if (connection.EndPoint == null) 
                continue;

            GenerateTangents(segment, connection);
            DrawConnection(segment, connection);    
        }
    }

    private static void GenerateTangents(SegmentAuthoring segment, SegmentAuthoringConnection connection)
    {
        // TODO: [MTS-49] use distance between segments to calculate a variable T
        const float TipsSamplingT = 0.025f;

        var segmentA = segment;
        var segmentB = connection.EndPoint;

        var segmentAStart = TransformPoint(segmentA, segmentA.Start);
        var segmentAEnd = TransformPoint(segmentA, segmentA.End);
        var segmentAStartTangent = TransformPoint(segmentA, segmentA.StartTangent);
        var segmentAEndTangent = TransformPoint(segmentA, segmentA.EndTangent);

        var segmentBStart = TransformPoint(segmentB, segmentB.Start);
        var segmentBEnd = TransformPoint(segmentB, segmentB.End);
        var segmentBStartTangent = TransformPoint(segmentB, segmentB.StartTangent);
        var segmentBEndTangent = TransformPoint(segmentB, segmentB.EndTangent);

        var preSegmentAEnd = EvaluateCubicBezier(segmentAStart, segmentAStartTangent, segmentAEndTangent, segmentAEnd, 1 - TipsSamplingT);
        var postSegmentBStart = EvaluateCubicBezier(segmentBStart, segmentBStartTangent, segmentBEndTangent, segmentBEnd, 0 + TipsSamplingT);

        var segmentADirection = (segmentAEnd - preSegmentAEnd).normalized;
        var segmentBDirection = (postSegmentBStart - segmentBStart).normalized;

        var tangentDistance = Vector3.Distance(segmentAEnd, segmentBStart) / 2f;
        var startTangent = segmentAEnd + segmentADirection * tangentDistance;
        var endTangent = segmentBStart - segmentBDirection * tangentDistance;

        connection.StartTangent = InverseTransformPoint(segmentA, startTangent);
        connection.EndTangent = InverseTransformPoint(segmentB, endTangent);
    }

    private static void DrawConnection(SegmentAuthoring fromSegment, SegmentAuthoringConnection connection)
    {
        var segmentA = fromSegment;
        var segmentB = connection.EndPoint;

        var segmentAEnd = TransformPoint(segmentA, segmentA.End);
        var segmentBStart = TransformPoint(segmentB, segmentB.Start);
        var startTangent = TransformPoint(segmentA, connection.StartTangent);
        var endTangent = TransformPoint(segmentB, connection.EndTangent);

        Handles.color = Color.yellow;
        Handles.DrawBezier(segmentAEnd, segmentBStart, startTangent, endTangent, Color.yellow, null, 3f);
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

        Vector3 previewEnd = ray.GetPoint(distance);
        Handles.color = new Color(1f, 1f, 1f, 0.5f);
        Handles.DrawDottedLine(_pendingStart.Value, previewEnd, 5f);
        SceneView.RepaintAll();
    }

    private static void DrawSegmentDirection(Vector3 start, Vector3 end)
    {
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

    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.Active)]
    private static void DrawSegmentGizmo(SegmentAuthoring segment, GizmoType gizmoType)
    {
        DrawSegment(segment);
        DrawSegmentConnections(segment);
    }
}
#endif
