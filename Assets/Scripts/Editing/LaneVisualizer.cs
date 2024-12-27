#if UNITY_EDITOR
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
        Draw(_lane);
    }

    private void Draw(LaneAuthoring lane)
    {
        if (lane.Waypoints == null || lane.Waypoints.Length == 0)
            return;

        // Iterate over waypoints and draw position handles
        for (int i = 0; i < lane.Waypoints.Length; i++)
        {
            var currentWaypoint = lane.Waypoints[i];

            // Draw a PositionHandle
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(currentWaypoint, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                // Record the change for undo/redo functionality
                Undo.RecordObject(lane, "Move Waypoint");
                lane.Waypoints[i] = newPosition;
                EditorUtility.SetDirty(lane);
            }

            // Draw a label and a small sphere for better visualization
            Handles.Label(currentWaypoint, $"Waypoint {i}");
            Handles.SphereHandleCap(0, currentWaypoint, Quaternion.identity, 0.5f, EventType.Repaint);

            // Right-click to delete a waypoint
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                // Handle right-click (Delete waypoint)
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                float distance = HandleUtility.DistanceToCircle(currentWaypoint, 0.5f);

                if (distance < 1f) // Right-click is near a waypoint
                {
                    Event.current.Use(); // Consume the event
                    DeleteWaypoint(lane, i);
                }
            }
        }

        // Draw connecting lines between waypoints
        Handles.color = Color.yellow;
        for (int i = 0; i < lane.Waypoints.Length - 1; i++)
        {
            Handles.DrawLine(lane.Waypoints[i], lane.Waypoints[i + 1]);
        }
    }

    private void DeleteWaypoint(LaneAuthoring lane, int index)
    {
        // Ensure we have more than one waypoint to delete
        if (lane.Waypoints.Length == 0) return;

        // Record undo state for deleting waypoint
        Undo.RecordObject(lane, "Delete Waypoint");

        // Remove the waypoint at the specified index
        var waypointList = new System.Collections.Generic.List<Vector3>(lane.Waypoints);
        waypointList.RemoveAt(index);
        lane.Waypoints = waypointList.ToArray();

        EditorUtility.SetDirty(lane);
    }
}
#endif
