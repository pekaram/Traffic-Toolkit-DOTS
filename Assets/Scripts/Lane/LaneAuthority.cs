using UnityEngine;
using Unity.Entities;


class LaneAuthoring : MonoBehaviour
{
    public Renderer Start;
    public Renderer End;
}

class LaneBaker : Baker<LaneAuthoring>
{
    public override void Bake(LaneAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Lane
        {
            Width = authoring.Start.bounds.size.x,
            StartPoint = authoring.Start.bounds.center,
            EndPoint = authoring.End.bounds.center,
        });
    }
}