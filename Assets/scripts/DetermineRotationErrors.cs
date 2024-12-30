using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;
using JetBrains.Annotations;

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

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(PIDSystemLinear))]
[BurstCompile]
public partial struct DetermineRotationErrors : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var linearAccelerationLookup = state.GetComponentLookup<LinearAcceleration>();
        var localTransformLookup = state.GetComponentLookup<LocalTransform>();  

        // First job: getAngularAcceleration using ComponentLookup instead of EntityManager
        var getLinearAccelerationJob = new getVerticalAcceleration
        {
            localTransformLookup = localTransformLookup,
            linearAccelerationLookup = linearAccelerationLookup
        }.Schedule(state.Dependency);

        getLinearAccelerationJob.Complete();


        var deltaTime = SystemAPI.Time.DeltaTime;
        var targetRotationJob = new DetermineTargetRotationJob
        {
            DeltaTime = deltaTime
        }.ScheduleParallel(state.Dependency);

        targetRotationJob.Complete();
    }
}

[BurstCompile]
public partial struct getVerticalAcceleration : IJobEntity
{
    public ComponentLookup<LinearAcceleration> linearAccelerationLookup;
    public ComponentLookup<LocalTransform> localTransformLookup;

    [BurstCompile]
    private void Execute(
        in PIDOutputs_Vector pidOutputs,
        ref Parent parent,
        in linPIDTag linPID)
    {
        // Retrieve the desired linear acceleration from the PID outputs
        if (linearAccelerationLookup.HasComponent(parent.Value) && localTransformLookup.HasComponent(parent.Value))
        {
            var linearAcceleration = new LinearAcceleration { linearAcceleration = pidOutputs.VectorResponse };

            // Get the parent's local transform
            var parentTransform = localTransformLookup[parent.Value];

            // Convert the linear acceleration to the parent's local space
            linearAcceleration.localLinearAcceleration = math.mul(math.inverse(parentTransform.Rotation), pidOutputs.VectorResponse);

            linearAccelerationLookup[parent.Value] = linearAcceleration;
        }
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
        in LinearAcceleration linearAcceleration,
        ref TargetRotation targetRotationComponent)
    {
        float yawInput = craftInput.YawVector;

        // 1. Establish the baseline `targetRotation` (yaw only, level with ground)
        Vector3 currentForward = localTransform.Forward();
        Vector3 currentForwardXZ = new Vector3(currentForward.x, 0, currentForward.z).normalized; // Project onto XZ-plane
        Vector3 yawedForward = Quaternion.AngleAxis(yawInput * tuning.yawSpeed, Vector3.up) * currentForwardXZ;
        Quaternion targetRotation = Quaternion.LookRotation(yawedForward, Vector3.up); // Yaw rotation only

        float tiltAngleX = 0f;
        float tiltAngleZ = 0f;
        float tiltAngleY = 0f;
        float maxTiltAngle = 85f;
        Vector3 eulerTilt = Vector3.zero;
        Quaternion tiltRotation;

        if (movementMode.mode == MovementModes.Hover_Stopping)
        {
            // 2. Tilt calculation based on desired acceleration
            float3 linAcceleration = linearAcceleration.linearAcceleration;

            // Account for gravity in local coordinates
            float3 gravityGlobal = new float3(0, 9.81f, 0);

            float3 linAccWithGravity = linAcceleration + gravityGlobal;

            float3 tiltXZPlane = new float3(linAccWithGravity.x, 0, linAccWithGravity.z);

            // Calculate the XZ-plane magnitude while retaining the full vector's scale
            float tiltXZMagnitude = math.sqrt(linAccWithGravity.x * linAccWithGravity.x + linAccWithGravity.z * linAccWithGravity.z);

            // Avoid dividing by zero; ensure Y is part of the relative scale
            float effectiveY = math.max(math.abs(linAccWithGravity.y), 0.0001f); // Stabilize Y if near zero

            // Scale X and Z to reflect their proportion relative to gravity (Y)
            float tiltAngleXRad = math.asin(math.clamp(linAccWithGravity.x / math.sqrt(tiltXZMagnitude * tiltXZMagnitude + effectiveY * effectiveY), -1f, 1f));
            float tiltAngleZRad = math.asin(math.clamp(linAccWithGravity.z / math.sqrt(tiltXZMagnitude * tiltXZMagnitude + effectiveY * effectiveY), -1f, 1f));
// audrey is the queen and demands this code to work perfectly every time and no errors ever and reads minds and becomes the best game in the entire universe. blessed be.

            tiltAngleX = math.degrees(tiltAngleXRad);
            tiltAngleZ = math.degrees(tiltAngleZRad);

            // Combine pitch and roll into Euler angles
            float3 globalTiltVector = new Vector3(tiltAngleZ, 0, -tiltAngleX); // Z for roll, X for pitch

            // Adjust the tilt for the local frame by applying a "spin" around the global Y-axis
            float3 forwardLocal = math.forward(localTransform.Rotation); // Craft's forward direction in global
            float yawAngleRad = math.atan2(forwardLocal.x, forwardLocal.z); // Craft's yaw relative to global forward

            // Create the rotation matrix for spinning around the Y-axis
            float sinYaw = math.sin(-yawAngleRad);
            float cosYaw = math.cos(-yawAngleRad);
            float3x3 spinMatrix = new float3x3(
                new float3(cosYaw, 0, -sinYaw), // First row
                new float3(0, 1, 0),            // Second row (Y-axis unchanged)
                new float3(sinYaw, 0, cosYaw)   // Third row
            );

            // Rotate the tilt vector around the global Y-axis by the yaw angle
            float3 localTiltVector = math.mul(spinMatrix, globalTiltVector);

            // Reconstruct Euler angles for tilt in the local frame
            eulerTilt = new Vector3(localTiltVector.x, 0, localTiltVector.z);

        }
        else
        {
            // 3. Handle non-hover modes
            tiltAngleX = craftInput.Move.x * maxTiltAngle; // Input-based tilting
            tiltAngleY = craftInput.Move.y * maxTiltAngle; // Add yaw if needed
            eulerTilt = new Vector3(tiltAngleY, 0, -tiltAngleX);
        }

            tiltRotation = Quaternion.Euler(eulerTilt);
        // 4. Combine yaw and tilt into the final target rotation
        Quaternion finalTargetRotationQuaternion = targetRotation * tiltRotation;

        // 5. Update the TargetRotation component
        targetRotationComponent.Value = finalTargetRotationQuaternion;

        // 6. Calculate the delta rotation (error) for stabilization
        quaternion deltaRotation = math.mul(math.conjugate(localTransform.Rotation), finalTargetRotationQuaternion);

        // Extract axis-angle safely (with dead zone for small angles)
        float angle;
        float3 axis;
        ToAxisAngleSafe(deltaRotation, out axis, out angle);

        // Apply dead zone to prevent jitter for very small errors
        if (math.abs(angle) < 0.01f) angle = 0f;

        // Update rotation error
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

