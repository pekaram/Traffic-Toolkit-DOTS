using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System;

public class FixedEntityIdAuthority : MonoBehaviour
{
}

public class FixedEntityIdBaker : Baker<FixedEntityIdAuthority> 
{
    public override void Bake(FixedEntityIdAuthority authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);

        AddComponent(entity, new FixedEntityId
        {
            Id = Guid.NewGuid().ToString(),
            DebugName = authoring.name
        });
    }
}

public struct FixedEntityId : IComponentData
{
    public FixedString64Bytes Id;

    public FixedString32Bytes DebugName;
}
