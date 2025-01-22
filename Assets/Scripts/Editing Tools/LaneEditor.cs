#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LaneAuthoring))]
public class LaneEditor : Editor
{
    private LaneAuthoring _lane;

    private void OnEnable()
    {
        _lane = (LaneAuthoring)target;
    }

    private void OnSceneGUI()
    {
        if (_lane == null || _lane.Waypoints == null)
            return;

        Draw();
        HandleInput();
    }

    private void Draw()
    {
        for (var i = 0; i < _lane.Waypoints.Count; i++)
        {
            DrawPoint(i);
            DrawPointConnection(i);
        }
        
        DrawLaneConnections();

        if (_lane.TrafficLight)
        {
           TrafficLightEditor.Visualize(_lane.TrafficLight);
        }
    }

    private void HandleInput()
    {
        for (var i = 0; i < _lane.Waypoints.Count; i++)
        {
            HandlePointDrag(i);
            HandlePointDelete(i);
        }

        HandleWaypointAdd();
        HandleLaneConnectionDelete();
    }

    private void DrawLaneConnections()
    {
       if (_lane.ConnectedLanes == null)
            return;

        for (var i = 0; i < _lane.ConnectedLanes.Count; i++)
        {
            var nextLane = _lane.ConnectedLanes[i];
            if (nextLane == null || nextLane.Waypoints.Count == 0)
                continue;

            Handles.color = Color.yellow;
            var from = TransformPoint(_lane.Waypoints[^1]);
            var to = nextLane.transform.TransformPoint(nextLane.Waypoints[0]);
            Handles.DrawDottedLine(from, to, 10);

            Handles.Label(to, $"Connection {i}");
            Handles.SphereHandleCap(0, to, Quaternion.identity, 0.5f, EventType.Repaint);
        } 
    }

    private void HandlePointDrag(int i)
    {
        EditorGUI.BeginChangeCheck();

        var currentWaypoint = _lane.Waypoints[i];
        var worldPosition = Handles.PositionHandle(TransformPoint(currentWaypoint), Quaternion.identity);
 
        if (!EditorGUI.EndChangeCheck())
            return;
 
        Undo.RecordObject(_lane, "Move Waypoint");
        _lane.Waypoints[i] = InverseTransformPoint(worldPosition);
        Event.current.Use();
    }

    private void HandlePointDelete(int i)
    {
        var isRightClick = Event.current.type == EventType.MouseDown && Event.current.button == 1;
        if (!isRightClick || !Event.current.shift)
            return;

        var currentWaypoint = TransformPoint(_lane.Waypoints[i]);
        var distance = HandleUtility.DistanceToCircle(currentWaypoint, 0.5f);

        if (distance < 1f)
        {
            DeleteWaypoint(_lane, i);
            Event.current.Use();
        }
    }

    private void HandleLaneConnectionDelete()
    {
        var isRightClick = Event.current.type == EventType.MouseDown && Event.current.button == 1;
        if (!isRightClick || !Event.current.shift)
            return;

        for (var i = 0; i < _lane.ConnectedLanes.Count; i++)
        {
            var localConnectionPoint = _lane.ConnectedLanes[i].Waypoints[0];
            var worldConnectionPoint = _lane.ConnectedLanes[i].transform.TransformPoint(localConnectionPoint);
            float distanceToConnectionPoint = HandleUtility.DistanceToCircle(worldConnectionPoint, 0.5f);

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
        var currentWaypoint = TransformPoint(_lane.Waypoints[i]);
        Handles.color = Color.blue;
        Handles.Label(currentWaypoint, $"Waypoint {i}");
        Handles.SphereHandleCap(0, currentWaypoint, Quaternion.identity, 0.5f, EventType.Repaint);
    }

    private void DrawPointConnection(int i)
    {
        if (_lane.Waypoints.Count - i > 1)
        {
            Handles.color = Color.green;
            Handles.DrawLine(TransformPoint(_lane.Waypoints[i]), TransformPoint(_lane.Waypoints[i + 1]));
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

        if (!plane.Raycast(ray, out float distance))
            return;

       var clickPosition = ray.GetPoint(distance);
        
        Undo.RecordObject(_lane, "Add Waypoint");
        var localPosition = InverseTransformPoint(clickPosition);
        _lane.Waypoints.Add(localPosition);

        EditorUtility.SetDirty(_lane);    
        Event.current.Use();
    }

    private bool TryAddConnection()
    {
        var clickedObject = HandleUtility.PickGameObject(Event.current.mousePosition, false);
        if (clickedObject == null)
            return false;

        var clickedLane = GetComponentSelfParentOrChildren<LaneAuthoring>(clickedObject);
        if (clickedLane == null || clickedLane == _lane)
            return false;

        Undo.RecordObject(_lane, "Add Connected Lane");

        if (!_lane.ConnectedLanes.Contains(clickedLane))
        {
            _lane.ConnectedLanes.Add(clickedLane);
        }

        EditorUtility.SetDirty(_lane);
        Event.current.Use(); 
        return true;
    }


    private void DeleteWaypoint(LaneAuthoring lane, int index)
    {
         if (lane.Waypoints.Count == 0) 
            return;

        Undo.RecordObject(lane, "Delete Waypoint");
        lane.Waypoints.RemoveAt(index);
    }

    private Vector3 TransformPoint(Vector3 localPosition)
    {
        return _lane.transform.TransformPoint(localPosition);
    }

    private Vector3 InverseTransformPoint(Vector3 worldPosition)
    {
        return _lane.transform.InverseTransformPoint(worldPosition);
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
