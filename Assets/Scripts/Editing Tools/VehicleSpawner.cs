using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
[CustomEditor(typeof(VehicleSpawner))]

public class VehicleSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var spawner = (VehicleSpawner)target;

        if (GUILayout.Button("Spawn Vehicles"))
        {
            spawner.SpawnVehicles();
        }

        if (GUILayout.Button("Clear Vehicles"))
        {
            spawner.DestroySpawnedVehicles();
        }
    }
}

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField, HideInInspector, FormerlySerializedAs("_spawnedVehicles")]
    private List<GameObject> _spawnedVehicles = new();

    [Header("Spawner Settings")]
    public GameObject _vehiclePrefab;
    public SegmentAuthoring _targetSegment;
    public Transform _parentContainer;
    public float _vehicleCount;

    public void SpawnVehicles()
    {
        for (int i = 0; i < _vehicleCount; i++)
        {     
            var vehicle = PrefabUtility.InstantiatePrefab(_vehiclePrefab, _parentContainer).GameObject();
            var vehicleSettings = vehicle.AddComponent<VehicleAuthoring>();
            var vehicleTransform = vehicle.transform;

            var t = i * (1f / _vehicleCount);
            vehicleSettings.Segment = _targetSegment;
            vehicleSettings.T = t;
            vehicleSettings.MaxSpeed = Random.Range(15, 20);
            vehicle.name = $"{_vehiclePrefab.name} {_spawnedVehicles.Count}";

            var spawnPos = EvaluateCubicBezier(
                _targetSegment.Start,
                _targetSegment.StartTangent,
                _targetSegment.EndTangent,
                _targetSegment.End,
                t);
            var worldSegment = _targetSegment.transform.TransformPoint(_targetSegment.Start);
            var worldSegmentEnd = _targetSegment.transform.TransformPoint(_targetSegment.End);
            vehicleTransform.position = _targetSegment.transform.TransformPoint(spawnPos);
            vehicleTransform.rotation = Quaternion.LookRotation(worldSegmentEnd - worldSegment);
            vehicleTransform.SetParent(_parentContainer);

            _spawnedVehicles.Add(vehicle);

            EditorUtility.SetDirty(_parentContainer);
        }
    }

    public void DestroySpawnedVehicles()
    {
        while (_spawnedVehicles.Any())
        {
            var vehicle = _spawnedVehicles.First();
            DestroyImmediate(vehicle);
            _spawnedVehicles.RemoveAt(0);
        }

        EditorUtility.SetDirty(_parentContainer);
    }


    private Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1 - t;
        return u * u * u * p0 +
               3 * u * u * t * p1 +
               3 * u * t * t * p2 +
               t * t * t * p3;
    }
}
#endif