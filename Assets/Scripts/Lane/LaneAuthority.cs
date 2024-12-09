using System.Text;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using System.Collections.Generic;
using System.Linq;

class LaneAuthoring : MonoBehaviour
{
    public List<Renderer> Renderers;
}

class LaneBaker : Baker<LaneAuthoring>
{
    public override void Bake(LaneAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Lane
        {
            Width = authoring.Renderers[0].bounds.size.x,
            StartPoint = authoring.Renderers[0].bounds.center,
            EndPoint = authoring.Renderers[authoring.Renderers.Count - 1].bounds.center,
        });
    }
}