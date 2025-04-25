#if UNITY_EDITOR
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
        var extraPt = EvaluateCubicBezier(_segment.Start, _segment.StartTangent, _segment.EndTangent, _segment.End, 0.975f);
        var extraPt2 = EvaluateCubicBezier(_segment.Start, _segment.StartTangent, _segment.EndTangent, _segment.End, 1);

        var extra1Pt = EvaluateCubicBezier(connection.EndPoint.Start, connection.EndPoint.StartTangent,  connection.EndPoint.EndTangent, connection.EndPoint.End, 0.025f);
        var extra1Pt2 = EvaluateCubicBezier(connection.EndPoint.Start,connection.EndPoint.StartTangent,  connection.EndPoint.EndTangent, connection.EndPoint.End, 0);

        Vector3 start = _segment.End;
        Vector3 end = connection.EndPoint.Start;

        Vector3 dirA = (extraPt2 - extraPt).normalized;
        Vector3 dirB = (extra1Pt - extra1Pt2).normalized;

        float len = Vector3.Distance(start, end) / 2f;

        Vector3 qp1 = start + dirA * len;
        Vector3 qp2 = end - dirB * len;

        Handles.SphereHandleCap(0, qp1, quaternion.identity, 1, EventType.Repaint);
        Handles.color = Color.blue;
        Handles.SphereHandleCap(0, qp2, quaternion.identity, 1, EventType.Repaint);

        connection.StartTangent = qp1;
        connection.EndTangent = qp2;

        Vector3 p0 = _segment.End;
        Vector3 p1 = connection.StartTangent;
        Vector3 p2 = connection.EndTangent;
        Vector3 p3 = connection.EndPoint.Start;

        // Interactive Handles
        Vector3 newP0 = Handles.PositionHandle(p0, Quaternion.identity);
        Vector3 newP1 = Handles.PositionHandle(p1, Quaternion.identity);
        Vector3 newP2 = Handles.PositionHandle(p2, Quaternion.identity);
        Vector3 newP3 = Handles.PositionHandle(p3, Quaternion.identity);

        Handles.color = Color.yellow;
        Handles.DrawBezier(newP0, newP3, newP1, newP2, Color.yellow, null, 3f);
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

    private void DrawPreview()
    {
        if (_pendingStart == null)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 previewEnd = ray.GetPoint(distance);
            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawDottedLine(_pendingStart.Value, previewEnd, 5f);
            SceneView.RepaintAll();
        }
    }
}
#endif
