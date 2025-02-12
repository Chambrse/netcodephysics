using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct ExhaustTag : IComponentData { }


[DisallowMultipleComponent]
public class ExhaustAuthoring : MonoBehaviour
{
    class Baker : Baker<ExhaustAuthoring>
    {
        public override void Bake(ExhaustAuthoring authoring)
        {
            // Get the entity corresponding to this GameObject
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);
            AddComponent<ExhaustTag>(entity);
            AddComponent(entity, new PostTransformMatrix
            {
                Value = float4x4.identity // or some custom 4x4 transform
            });

        }
    }
}
