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

    public float3 targetRotationError;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(UpdateCraftPhysicsPropertiesSystem))]
[BurstCompile]
public partial struct DetermineTargetRotationSystem : ISystem
{

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

        float tiltAngleX;
        float tiltAngleY;

        float maxTiltAngle = 85;

        tiltAngleX = craftInput.Move.x * maxTiltAngle;
        tiltAngleY = craftInput.Move.y * maxTiltAngle;

        // Create quaternion rotation from Euler angles
        Vector3 eulerTilt = new Vector3(tiltAngleY, 0, -tiltAngleX);
        Quaternion tiltRotation = Quaternion.Euler(eulerTilt);

        Quaternion finalTargetRotationQuaternion = targetRotation * tiltRotation;
        // Vector3 finalTargetRotationEuler = finalTargetRotationQuaternion.eulerAngles;
        targetRotationComponent.Value = finalTargetRotationQuaternion;

        // 1. Transform local axes (Vector3.right and Vector3.up) using the targetRotation quaternion
        float3 targetRight = math.mul(targetRotationComponent.Value, new float3(1, 0, 0)); // Equivalent to target.TransformDirection(Vector3.right)
        float3 targetUp = math.mul(targetRotationComponent.Value, new float3(0, 1, 0));    // Equivalent to target.TransformDirection(Vector3.up)

        // 2. Transform current local axes (Vector3.right and Vector3.up) using the current rotation quaternion
        float3 currentRight = math.mul(localTransform.Rotation, new float3(1, 0, 0));      // Equivalent to current.TransformDirection(Vector3.right)
        float3 currentUp = math.mul(localTransform.Rotation, new float3(0, 1, 0));         // Equivalent to current.TransformDirection(Vector3.up)

        // 3. Compute the cross products between the corresponding axes
        float3 crossProductY = math.cross(currentUp, targetUp);
        float3 crossProductX = math.cross(currentRight, targetRight);

        // 4. Sum the cross products
        float3 crossProductSum = crossProductX + crossProductY;

        targetRotationComponent.targetRotationError = crossProductSum;

    }
}

