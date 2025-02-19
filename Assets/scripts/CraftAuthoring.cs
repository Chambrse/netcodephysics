using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;


public struct CraftTuning : IComponentData
{
    public float yawSpeed;
    public float pitchSpeed;
    public float rollSpeed;
    public float maxThrust;
    public float3 dragVector;
    public float3 centerOfPressureOffset;
}

[GhostComponent(SendTypeOptimization = GhostSendType.DontSend)]
public struct PlayerTag : IComponentData { };
public struct rotPIDTag : IComponentData { };
public struct linPIDTag : IComponentData { };

// we use this to bypass the multiple physics steps and get a smooth camera
public struct cameraFollowPosition : IComponentData
{
    // The smoothed "presentation" transform that we use for rendering/camera
    public float3 SmoothedPosition;
    public quaternion SmoothedRotation;
}

public struct Username : IComponentData
{
    public FixedString64Bytes Value;
}

[DisallowMultipleComponent]
public class CraftAuthoring : MonoBehaviour
{
    public float yawspeed = 1.0f;

    public float pitchSpeed = 1.0f;

    public float rollSpeed = 1.0f;

    public float maxThrust = 1.0f;

    //editor group
    [Header("Rotational Gains")]
    
    public float Kp_ROT = 1.0f;

    public float Ki_ROT = 0.0f;

    public float Kd_ROT = 1.0f;

    [Header("Linear Gains")]
    public float Kp_LIN = 1.0f;

    public float Ki_LIN = 0.0f;

    public float Kd_LIN = 1.0f;

    [Header("Initial Movement Mode")]
    public MovementModes MovementMode;

    [Header("Drag Coefficients")]
    public float3 dragVector = float3.zero;

    [Header("centerOfPressure offset")]
    public float3 centerOfPressureOffset= float3.zero;



    class Baker : Baker<CraftAuthoring>
    {
        public override void Bake(CraftAuthoring authoring)
        {
            // Get the entity corresponding to this GameObject
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            var tuning = new CraftTuning
            {
                yawSpeed = authoring.yawspeed,
                rollSpeed = authoring.rollSpeed,
                pitchSpeed = authoring.pitchSpeed,
                maxThrust = authoring.maxThrust,
                dragVector = authoring.dragVector,
                centerOfPressureOffset = authoring.centerOfPressureOffset,
            };

            // Add components to the main entity using Baker API
            AddComponent(entity, tuning);
            AddComponent<PlayerTag>(entity);
            //AddComponent<MovementMode>(entity);
            AddComponent(entity, new MovementMode { 
                mode = authoring.MovementMode,
                locked = false
            });
            AddComponent<CraftPhysicsProperties>(entity);
            AddComponent<cameraFollowPosition>(entity);
            AddComponent<CraftInput>(entity);
            //AddComponent(entity, initialInput);
            AddComponent<TargetRotation>(entity);
            AddComponent<AngularAcceleration>(entity);
            AddComponent<TargetRelativeVelocity>(entity);
            AddComponent<PreviousVelocity>(entity);
            AddComponent<LinearAcceleration>(entity);
            AddComponent<AeroForces>(entity);
            AddComponent<Username>(entity);

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

        }
    }
}
