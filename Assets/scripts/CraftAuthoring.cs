using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Entities.Serialization;

public struct CraftTuning : IComponentData
{
    public float yawSpeed;

}

public struct PlayerTag : IComponentData { }

//public struct DebugVizEntityPrefabs : IComponentData
//{
//    public Entity rotationVizPrefab;
//    public Entity vectorVizPrefab;
//}



[DisallowMultipleComponent]
public class CraftAuthoring : MonoBehaviour
{
    public float yawspeed = 1.0f;

    public float pitchSpeed = 1.0f;

    //editor group
    [Header("Rotational Gains")]
    
    public float Kp_ROT = 1.0f;

    public float Ki_ROT = 0.0f;

    public float Kd_ROT = 1.0f;

    [Header("Linear Gains")]
    public float Kp_LIN = 1.0f;

    public float Ki_LIN = 0.0f;

    public float Kd_LIN = 1.0f;

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
            AddComponent<AngularAcceleration>(entity);
            AddComponent<TargetRelativeVelocity>(entity);
            AddComponent<PreviousVelocity>(entity);
            AddComponent<VerticalAcceleration>(entity);

            // Create a new entity for the PID controller using CreateAdditionalEntity
            var rotationPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "RotationPID");

            // Add PID components to the new PID entity using Baker API
            AddComponent(rotationPID, new PIDGainSet { Kp = authoring.Kp_ROT, Ki = authoring.Ki_ROT, Kd = authoring.Kd_ROT });  
            AddComponent(rotationPID, new PIDInputs_Vector
            {
                AngleError = new float3(0, 0, 0),
                AngularVelocity = new float3(0, 0, 0)
            });
            AddComponent(rotationPID, new PIDOutputs_Vector { angularAcceleration = new Vector3(0, 0, 0) });

            // Set the parent component for the PID entity using Baker API
            AddComponent(rotationPID, new Parent { Value = entity });

            var hoverHeightPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "HoverHeightPID");

            AddComponent(hoverHeightPID, new PIDGainSet { Kp = authoring.Kp_LIN, Ki = authoring.Ki_LIN, Kd = authoring.Kd_LIN });
            AddComponent(hoverHeightPID, new PIDInputs_Scalar { Error = 0 });
            AddComponent(hoverHeightPID, new PIDOutputs_Scalar { linearAcceleration = 0 });

            AddComponent(hoverHeightPID, new Parent { Value = entity });




        }
    }
}
