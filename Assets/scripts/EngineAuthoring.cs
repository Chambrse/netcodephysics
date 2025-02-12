using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct EngineTag : IComponentData { }


[DisallowMultipleComponent]
public class EngineAuthoring : MonoBehaviour
{
    class Baker : Baker<EngineAuthoring>
    {
        public override void Bake(EngineAuthoring authoring)
        {
            // Get the entity corresponding to this GameObject
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EngineTag>(entity);

        }
    }
}
