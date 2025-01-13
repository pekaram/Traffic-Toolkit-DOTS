#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 
[CustomEditor(typeof(LaneAuthoring))]
public class LaneAuthoringEditor : Editor
{
    private LaneAuthoring _lane;

    private void OnEnable()
    {
        if (Selection.activeGameObject != null)
        {
            _lane = Selection.activeGameObject.GetComponent<LaneAuthoring>();
        }
    }

    private void OnSceneGUI()
    {
        if (_lane == null || _lane.Waypoints == null || _lane.Waypoints.Count == 0)
            return;

        for (var i = 0; i < _lane.Waypoints.Count; i++)
        {
            DrawPoint(i);         
            HandlePointDrag(i);
            HandlePointDelete(i);
        }

        HandleWaypointAdd();
        HandleConnectionsDelete();
        DrawConnections();
    }


    private void DrawConnections()
    {
        var laneAuthoring = _lane;
        if (laneAuthoring.ConnectedLanes == null)
            return;

        foreach (var connectedLane in laneAuthoring.ConnectedLanes)
        {
            if (connectedLane != null && connectedLane.Waypoints.Count > 0)
            {
                Vector3 endPoint = laneAuthoring.Waypoints[laneAuthoring.Waypoints.Count - 1];
                Vector3 connectedStartPoint = connectedLane.Waypoints[0];

                Handles.color = Color.red;
                Handles.DrawLine(endPoint, connectedStartPoint);

                Handles.Label(connectedStartPoint, $"Connection");
                Handles.SphereHandleCap(0, connectedStartPoint, Quaternion.identity, 0.5f, EventType.Repaint);
            }
        }

    }

    private void HandlePointDrag(int i)
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

    private void HandlePointDelete(int i)
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

    private void HandleConnectionsDelete()
    {
        var isRightClick = Event.current.type == EventType.MouseDown && Event.current.button == 1;
        if (!isRightClick || !Event.current.shift)
            return;

        for (var i = 0; i < _lane.ConnectedLanes.Count; i++)
        {  
            float distanceToConnectionPoint = HandleUtility.DistanceToCircle(_lane.ConnectedLanes[i].Waypoints[0], 0.5f);

            if (distanceToConnectionPoint < 10f) 
            {
                Undo.RecordObject(_lane, "Remove Connected Lane");

                _lane.ConnectedLanes.RemoveAt(i);
                EditorUtility.SetDirty(_lane);

                Event.current.Use(); 
                break;
            }
        }
    }

    private void DrawPoint(int i)
    {
        var currentWaypoint = _lane.Waypoints[i];
        Handles.color = Color.blue;
        Handles.Label(currentWaypoint, $"Waypoint {i}");
        Handles.SphereHandleCap(0, currentWaypoint, Quaternion.identity, 0.5f, EventType.Repaint);

        if (_lane.Waypoints.Count - i > 1)
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

        if (TryAddConnection())
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

    private bool TryAddConnection()
    {
        GameObject clickedObject = HandleUtility.PickGameObject(Event.current.mousePosition, false);
        if (clickedObject == null)
            return false;

        var clickedLane = GetComponentSelfParentOrChildren<LaneAuthoring>(clickedObject);
        if (clickedLane == null || clickedLane == _lane) 
        {
            return false;
        }

        Undo.RecordObject(_lane, "Add Connected Lane");

        if (!_lane.ConnectedLanes.Contains(clickedLane))
        {
            _lane.ConnectedLanes.Add(clickedLane);
            EditorUtility.SetDirty(_lane);
        }

        Event.current.Use(); 
        return true;
    }

    private void AddWaypoint(LaneAuthoring lane, Vector3 position)
    {
        var waypoints = lane.Waypoints;
        waypoints.Add(position);
    }

    private void DeleteWaypoint(LaneAuthoring lane, int index)
    {
         if (lane.Waypoints.Count == 0) 
            return;

        Undo.RecordObject(lane, "Delete Waypoint");

        lane.Waypoints.RemoveAt(index);
        
        EditorUtility.SetDirty(lane);
    }
    private static T GetComponentSelfParentOrChildren<T>(GameObject gameObject) where T : Component
    {
        // Check on self
        var component = gameObject.GetComponent<T>();
        if (component != null)
            return component;

        // Check on parent
        component = gameObject.GetComponentInParent<T>();
        if (component != null)
            return component;

        // Check on children
        component = gameObject.GetComponentInChildren<T>();
        if (component != null)
            return component;

        // Nothing found
        return null;
    }
}
#endif
