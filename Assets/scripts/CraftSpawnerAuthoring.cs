using Unity.Entities;
using UnityEngine;

public struct CraftSpawner : IComponentData
{
    public Entity Craft;
}

[DisallowMultipleComponent]
public class CraftSpawnerAuthoring : MonoBehaviour
{
    public GameObject Craft; // This is the prefab reference

    class Baker : Baker<CraftSpawnerAuthoring>
    {
        public override void Bake(CraftSpawnerAuthoring authoring)
        {
            var spawnerEntity = GetEntity(TransformUsageFlags.Dynamic);

            // Reference the craft prefab as an entity
            CraftSpawner spawnerComponent = new CraftSpawner
            {
                Craft = GetEntity(authoring.Craft, TransformUsageFlags.Dynamic)
            };

            // Add CraftSpawner component to the spawner entity
            AddComponent(spawnerEntity, spawnerComponent);
        }
    }
}