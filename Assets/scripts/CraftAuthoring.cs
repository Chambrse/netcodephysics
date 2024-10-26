using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Entities.Serialization;
using Unity.NetCode;

public struct CraftTuning : IComponentData
{
    public float yawSpeed;

}

public struct PlayerTag : IComponentData { }

public struct rotPIDTag : IComponentData 
{
    
};

public struct linPIDTag : IComponentData { };

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
            AddComponent<LinearAcceleration>(entity);

            var rotationPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "RotationPID");
            AddComponent(rotationPID, new PIDGainSet { Kp = authoring.Kp_ROT, Ki = authoring.Ki_ROT, Kd = authoring.Kd_ROT });  
            AddComponent(rotationPID, new PIDInputs_Vector
            {
                VectorError = new float3(0, 0, 0),
                DeltaVectorError = new float3(0, 0, 0)
            });
            AddComponent(rotationPID, new PIDOutputs_Vector { VectorResponse = new float3(0, 0, 0) });
            AddComponent(rotationPID, new Parent { Value = entity });
            AddComponent(rotationPID, new rotPIDTag { });
            AddComponent(rotationPID, new PIDGainFromInput { GainInput = 0f });

            var linearVectorPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "LinearPID");
            AddComponent(linearVectorPID, new PIDGainSet { Kp = authoring.Kp_LIN, Ki = authoring.Ki_LIN, Kd = authoring.Kd_LIN });
            AddComponent(linearVectorPID, new PIDInputs_Vector
            {
                VectorError = new float3(0, 0, 0),
                DeltaVectorError = new float3(0, 0, 0)
            });
            AddComponent(linearVectorPID, new PIDOutputs_Vector { VectorResponse = new float3(0, 0, 0) });
            AddComponent(linearVectorPID, new linPIDTag { });
            AddComponent(linearVectorPID, new PIDGainFromInput { GainInput = 0f });
            AddComponent(linearVectorPID, new Parent { Value = entity });

            //var hoverHeightPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "HoverHeightPID");
            //AddComponent(hoverHeightPID, new PIDGainSet { Kp = authoring.Kp_LIN_Height, Ki = authoring.Ki_LIN_Height, Kd = authoring.Kd_LIN_Height });
            //AddComponent(hoverHeightPID, new PIDInputs_Scalar { Error = 0 });
            //AddComponent(hoverHeightPID, new PIDOutputs_Scalar { linearAcceleration = 0 });
            //AddComponent(hoverHeightPID, new Parent { Value = entity });

            //var horizontalPID = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, false, "HorizontalPID");
            //AddComponent(horizontalPID, new PIDGainSet { Kp = authroing })


        }
    }
}
