using Unity.Entities;
using UnityEngine;

public struct CraftTuning : IComponentData
{
    public float yawSpeed;
}
public struct PlayerTag : IComponentData { }

[DisallowMultipleComponent]
public class CraftAuthoring : MonoBehaviour
{

    public float yawspeed = 1.0f;

    class Baker : Baker<CraftAuthoring>
    {
        public override void Bake(CraftAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Craft craftComponent = new Craft { yawSpeed = _yawspeed };
            AddComponent(entity, new CraftTuning { yawSpeed = authoring.yawspeed });

            // AddComponent<Craft>(entity);
            AddComponent<PlayerTag>(entity);
            AddComponent<MovementMode>(entity);
            AddComponent<PhysicsProperties>(entity);
            AddComponent<CraftInput>(entity);
            AddComponent<TargetRotation>(entity);
        }

        public static bool IntToBool(int i)
        {
            return i == 1;
        }
    }
}