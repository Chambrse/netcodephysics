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
        Vector3 currentForward = localTransform.Forward();

        // Project current forward onto the xz plane
        Vector3 currentForwardXZ = new Vector3(currentForward.x, 0, currentForward.z);

        Vector3 yawedForward = Quaternion.AngleAxis(yawInput * tuning.yawSpeed, Vector3.up) * currentForwardXZ;
        yawedForward.Normalize();  // Make sure it's a unit vector

        // Calculate the target rotation aligned with the world's up vector
        Quaternion targetRotation = Quaternion.LookRotation(yawedForward, Vector3.up);

        float tiltAngleX;
        float tiltAngleY;
        float tiltAngleZ;

        float maxTiltAngle = 85;
        Vector3 eulerTilt;
        if (movementMode.mode == MovementModes.Hover_Stopping)
        {
            //do stuff with the output of the pid controller component that is a child of the entities returned by this query and has tag linPIDTag
            float3 linAcceleration = linearAcceleration.localLinearAcceleration;
            float3 linAccelerationWithGravityFeedForward = linAcceleration + new float3(0, 9.81f, 0);

            float3 linAccelerationNorm = math.normalize(linAccelerationWithGravityFeedForward);

            //var tiltAngleXRad = math.acos((linAccelerationNorm.y) / math.sqrt(math.pow(linAccelerationNorm.x, 2) + math.pow(linAccelerationNorm.y, 2)));
            //var tiltAngleZRad = math.acos((linAccelerationNorm.y) / math.sqrt(math.pow(linAccelerationNorm.z, 2) + math.pow(linAccelerationNorm.y, 2)));
            var tiltAngleXRad = math.atan(linAccelerationNorm.x / (linAccelerationNorm.y));
            var tiltAngleZRad = math.atan(linAccelerationNorm.z / (linAccelerationNorm.y));
            tiltAngleX = math.degrees(tiltAngleXRad);
            tiltAngleZ = math.degrees(tiltAngleZRad);
            eulerTilt = new Vector3(tiltAngleZ, 0, -tiltAngleX);

        }
        else
        {
            tiltAngleX = craftInput.Move.x * maxTiltAngle;
            tiltAngleY = craftInput.Move.y * maxTiltAngle;
        eulerTilt = new Vector3(tiltAngleY, 0, -tiltAngleX);

        }

        // Create quaternion rotation from Euler angles
        Quaternion tiltRotation = Quaternion.Euler(eulerTilt);

        // Combine target yaw rotation and tilt
        Quaternion finalTargetRotationQuaternion = targetRotation * tiltRotation;
        targetRotationComponent.Value = finalTargetRotationQuaternion;

        // Calculate the delta rotation (difference between current and target)
        quaternion deltaRotation = math.mul(math.conjugate(localTransform.Rotation), finalTargetRotationQuaternion);


        //quaternion deltaRotation = math.mul(math.conjugate(localTransform.Rotation), finalTargetRotationQuaternion);
        //float3 axis;
        //float angle;
        //math.toAxisAngle(deltaRotation, out axis, out angle);

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

