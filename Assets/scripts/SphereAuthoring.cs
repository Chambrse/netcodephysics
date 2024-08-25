using Unity.Entities;
using UnityEngine;

class SphereAuthoring : MonoBehaviour
{
    
}

public struct Sphere : IComponentData
{
}

class SphereAuthoringBaker : Baker<SphereAuthoring>
{
    public override void Bake(SphereAuthoring authoring)
    {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Sphere>(entity);
    }
}
