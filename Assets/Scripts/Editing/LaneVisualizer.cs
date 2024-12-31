#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaneAuthoring))]
public class LaneAuthoringEditor : Editor
{
    private LaneAuthoring _lane;

    private void OnEnable()
    {
        if(Selection.activeGameObject != null)
        _lane = Selection.activeGameObject.GetComponent<LaneAuthoring>();
    }

    private void OnSceneGUI()
    {
        if (_lane == null || _lane.Waypoints == null || _lane.Waypoints.Length == 0)
            return;

        for (int i = 0; i < _lane.Waypoints.Length; i++)
        {
            Draw(i);
            HandleWaypointDrag(i);
            HandleWaypointDelete(i);
        }

        HandleWaypointAdd();
    }

    private void HandleWaypointDrag(int i)
    {
        var currentWaypoint = _lane.Waypoints[i];

        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(currentWaypoint, Quaternion.identity);

        if (!EditorGUI.EndChangeCheck())
            return;

        Undo.RecordObject(_lane, "Move Waypoint");
        _lane.Waypoints[i] = newPosition;
        EditorUtility.SetDirty(_lane);
    }

    private void HandleWaypointDelete(int i)
    {
        var isRightClick = Event.current.type == EventType.MouseDown && Event.current.button == 1;
        if (!isRightClick || !Event.current.shift)
            return;

        var currentWaypoint = _lane.Waypoints[i];
        var distance = HandleUtility.DistanceToCircle(currentWaypoint, 0.5f);

        if (distance < 1f)
        {
            DeleteWaypoint(_lane, i);
            Event.current.Use();
        }
    }

    private void Draw(int i)
    {
        var currentWaypoint = _lane.Waypoints[i];
        Handles.color = Color.blue;
        Handles.Label(currentWaypoint, $"Waypoint {i}");
        Handles.SphereHandleCap(0, currentWaypoint, Quaternion.identity, 0.5f, EventType.Repaint);

        if (_lane.Waypoints.Length - i > 1)
        {
            Handles.color = Color.yellow;
            Handles.DrawLine(_lane.Waypoints[i], _lane.Waypoints[i + 1]);
        }
    }

    private void HandleWaypointAdd()
    {
        var leftMouseClick = Event.current.type == EventType.MouseDown && Event.current.button == 0;
        if (!leftMouseClick || !Event.current.shift)
            return;

        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var plane = new Plane(Vector3.up, Vector3.zero);

        // Find the intersection point
        if (!plane.Raycast(ray, out float distance))
            return;

        var clickPosition = ray.GetPoint(distance);
        
        Undo.RecordObject(_lane, "Add Waypoint");
        AddWaypoint(_lane, clickPosition);
        EditorUtility.SetDirty(_lane);

        Event.current.Use();
    }

    private void AddWaypoint(LaneAuthoring lane, Vector3 position)
    {
        var waypoints = lane.Waypoints.ToList();
        waypoints.Add(position);
        lane.Waypoints = waypoints.ToArray();
    }

    private void DeleteWaypoint(LaneAuthoring lane, int index)
    {
         if (lane.Waypoints.Length == 0) 
            return;

         Undo.RecordObject(lane, "Delete Waypoint");

        var waypointList = new System.Collections.Generic.List<Vector3>(lane.Waypoints);
        waypointList.RemoveAt(index);
        lane.Waypoints = waypointList.ToArray();

        EditorUtility.SetDirty(lane);
    }
}
#endif
