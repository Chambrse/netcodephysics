using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
            // Get the entity corresponding to this GameObject
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // Add components to the main entity using Baker API
            AddComponent(entity, new CraftTuning { yawSpeed = authoring.yawspeed });
            AddComponent<PlayerTag>(entity);
            AddComponent<MovementMode>(entity);
            AddComponent<CraftPhysicsProperties>(entity);
            AddComponent<CraftInput>(entity);
            AddComponent<TargetRotation>(entity);
            // AddComponent<LocalTransform>(entity);
            // AddComponent<LocalToWorld>(entity);
            // Create a new entity for the PID controller using CreateAdditionalEntity
            var rotationPID = CreateAdditionalEntity(TransformUsageFlags.None, false, "RotationPID");

            // Add PID components to the new PID entity using Baker API
            AddComponent(rotationPID, new PIDGainSet { Kp = 1.0f, Ki = 0.0f, Kd = 0.0f });
            AddComponent(rotationPID, new PIDInputs_Vector
            {
                AngleError = new float3(0, 0, 0),
                AngularVelocity = new float3(0, 0, 0)
            });
            AddComponent(rotationPID, new PIDOutputs_Vector { angularAcceleration = new Vector3(0, 0, 0) });

            // Set the parent component for the PID entity using Baker API
            AddComponent(rotationPID, new Parent { Value = entity });
            // Also, ensure the PID controller entity has transform-related components
            AddComponent<LocalTransform>(rotationPID);
            AddComponent<LocalToWorld>(rotationPID);
        }
    }
}
