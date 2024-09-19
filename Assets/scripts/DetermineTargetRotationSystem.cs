using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;

public struct PhysicsProperties : IComponentData
{
    public Vector3 centerOfMass;
    public float3 inertiaTensor;
    public Vector3 centerOfPressure;
    public float totalMass;
}

public struct TargetRotation : IComponentData
{
    public quaternion Value;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateCraftPhysicsPropertiesSystem))]
[BurstCompile]
public partial struct DetermineTargetRotationSystem : ISystem
{

    // [BurstCompile]
    // public void OnCreate(ref SystemState state)
    // {
    //     // don't know what to put here yet 
    //     state.RequireForUpdate<PhysicsProperties>();
    // }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = SystemAPI.Time.DeltaTime;
        new DetermineTargetRotationJob
        {
            DeltaTime = deltaTime
        }.Schedule();
    }
}

[BurstCompile]
public partial struct DetermineTargetRotationJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in CraftInput craftInput, 
        in MovementMode movementMode, 
        in LocalTransform localTransform, 
        in CraftTuning tuning, 
        in PhysicsVelocity physicsVelocity,
        ref TargetRotation targetRotationComponent)
    {
        float yawInput = craftInput.YawVector;
        Vector3 currentForward = localTransform.Forward();

        // project current forward onto the xz plane
        Vector3 currentForwardXZ = new Vector3(currentForward.x, 0, currentForward.z);

        Vector3 yawedForward = Quaternion.AngleAxis(yawInput * tuning.yawSpeed, Vector3.up) * currentForwardXZ;
        yawedForward.Normalize();  // Make sure it's a unit vector

        // Calculate the target rotation aligned with the world's up vector
        Quaternion targetRotation = Quaternion.LookRotation(yawedForward, Vector3.up);

        Vector3 localVelocity = localTransform.InverseTransformDirection(physicsVelocity.Linear);


        float tiltAngleX;
        float tiltAngleY;

        float maxTiltAngle = 85;

        // if (playerControls.Hover.Stop.ReadValue<float>() > 0)
        // {
        //     float xCorrectionTilt = XACCPIDController.Update(0, localVelocity.x, Time.fixedDeltaTime);
        //     float zCorrectionTilt = ZACCPIDController.Update(0, localVelocity.z, Time.fixedDeltaTime);

        //     tiltAngleX = Mathf.Clamp(xCorrectionTilt, -maxTiltAngle, maxTiltAngle);
        //     tiltAngleY = Mathf.Clamp(zCorrectionTilt, -maxTiltAngle, maxTiltAngle);
        // }
        // else
        // {
            tiltAngleX = craftInput.Move.x * maxTiltAngle;
            tiltAngleY = craftInput.Move.y * maxTiltAngle;

        // }
        //Debug.Log("tiltAngleX: " + tiltAngleX);
        //Debug.Log("tiltAngleY: " + tiltAngleY);


        // Create quaternion rotation from Euler angles
        Vector3 eulerTilt = new Vector3(tiltAngleY, 0, -tiltAngleX);
        Quaternion tiltRotation = Quaternion.Euler(eulerTilt);

        targetRotationComponent.Value = targetRotation * tiltRotation;
    }
}

