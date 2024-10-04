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
public partial struct DetermineRotationErrors : ISystem
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

        // Project current forward onto the xz plane
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

        // Combine target yaw rotation and tilt
        Quaternion finalTargetRotationQuaternion = targetRotation * tiltRotation;
        targetRotationComponent.Value = finalTargetRotationQuaternion;

        // Calculate the delta rotation (difference between current and target)
        quaternion deltaRotation = math.mul(math.conjugate(localTransform.Rotation), finalTargetRotationQuaternion);

        // Extract the axis-angle representation of the delta rotation
        float angle;
        float3 axis;
        ToAxisAngleSafe(deltaRotation, out axis, out angle);

        // Calculate the rotation error based on the angle and axis
        targetRotationComponent.targetRotationError = axis * angle;

    }

    // Helper function to convert quaternion to axis-angle representation safely
    private void ToAxisAngleSafe(quaternion q, out float3 axis, out float angle)
    {
        // Normalize the quaternion to avoid errors
        q = math.normalize(q);

        // Calculate the angle (clamp to avoid invalid values for acos)
        angle = 2.0f * math.acos(math.clamp(q.value.w, -1f, 1f));

        // Clamp angle to the range [-pi, pi] to avoid flipping issues
        if (angle > math.PI)
        {
            angle -= 2.0f * math.PI;
        }

        // Calculate the axis
        float sinHalfAngle = math.sqrt(1.0f - q.value.w * q.value.w);

        // Avoid division by zero by checking for small angles
        if (sinHalfAngle < 0.001f)
        {
            // If the angle is very small, we approximate the axis
            axis = new float3(1, 0, 0);
        }
        else
        {
            axis = q.value.xyz / sinHalfAngle;
        }

        // Make sure axis is normalized
        axis = math.normalize(axis);
    }
}

