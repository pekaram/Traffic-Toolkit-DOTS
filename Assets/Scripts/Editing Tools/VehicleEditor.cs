#if UNITY_EDITOR
using Bezier;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleAuthoring))]
public class VehicleEditor : Editor
{
    private VehicleAuthoring _vehicle;

    private float _cachedT = float.NaN;
    private SegmentAuthoring _cachedSegment = default;

    private void OnEnable()
    {
        _vehicle = (VehicleAuthoring)target;
    }

    private void OnSceneGUI()
    {
        if (_vehicle.Segment == _cachedSegment && _cachedT == _vehicle.T)
            return;

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        _cachedSegment = _vehicle.Segment;
        _cachedT = _vehicle.T;

        if (!_cachedSegment)
            return;

        _vehicle.transform.position = BezierUtilities.EvaluateCubicBezier(_cachedSegment, _cachedT);
        _vehicle.transform.rotation = Quaternion.LookRotation(BezierUtilities.EvaluateCubicBezier(_cachedSegment, _cachedT + 0.1f) - _vehicle.transform.position);
        EditorUtility.SetDirty(_vehicle.transform);
    }
}
#endif