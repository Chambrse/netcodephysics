using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;
using UnityEngine.SocialPlatforms;

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

[UpdateInGroup(typeof(CustomPhysicsSystemGroup))]
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
        float maxTiltAngle = 85f;

        quaternion desiredRotationLocal;
        quaternion desiredRotationWithYawInput;
        if (movementMode.mode == MovementModes.Hover_Stopping)
        {
            // get desired local acceleration (output from linear PID controller
            float3 linAccelerationLocal = linearAcceleration.localLinearAcceleration;
            //add gravity so we can get the angle that will counteract our current velocity and gravity at the same time.
            float3 gravityGlobal = new float3(0, 9.81f, 0); // Gravity in global space
            float3 gravityLocal = math.rotate(math.conjugate(localTransform.Rotation), gravityGlobal); // Transform gravity into local space
            float3 linAccWithGravity_Local = linAccelerationLocal + gravityLocal;

            // Calculate tilt angles in radians
            float tiltAngleXRad = math.atan2(-linAccWithGravity_Local.x, linAccWithGravity_Local.y); 
            float tiltAngleZRad = math.atan2(linAccWithGravity_Local.z, linAccWithGravity_Local.y);
            float3 localTiltVector = new float3(tiltAngleZRad, 0, tiltAngleXRad); // Z for roll, X for pitch
            quaternion desiredTiltLocal = quaternion.Euler(localTiltVector);

            
            desiredRotationLocal = math.mul(localTransform.Rotation, desiredTiltLocal);


            //now add in the yaw input
            //further manipulations need to use the already calculated desiredrotationglobal as a starting point
            float3 desiredForward = math.forward(desiredRotationLocal); // Forward direction from desired rotation
            float3 desiredUp = math.rotate(desiredRotationLocal, math.up()); // Up direction derived from desired rotation

            float yawInput = math.radians(craftInput.YawVector * tuning.yawSpeed); // Convert yaw input to radians
            quaternion yawSpin = quaternion.AxisAngle(desiredUp, yawInput); // Yaw around the desired up axis

            // Step 3: Apply the yaw spin to the forward direction of the desired rotation
            float3 newForward = math.rotate(yawSpin, desiredForward); // Rotate the forward vector using the yaw input

            // Step 4: Create the final spin rotation using LookRotationSafe
            desiredRotationWithYawInput = quaternion.LookRotationSafe(newForward, desiredUp); // Align forward and up

        }
        else if (movementMode.mode == MovementModes.Fly)
        {
            // New Fly mode: Map move input directly to roll and pitch
            float roll = math.radians(craftInput.Move.x * tuning.rollSpeed); // Input for roll
            float pitch = math.radians(craftInput.Move.y * tuning.pitchSpeed); // Input for pitch

            quaternion desiredTiltLocal = quaternion.Euler(pitch, 0, -roll); // Local roll and pitch rotation
            desiredRotationLocal = math.mul(localTransform.Rotation, desiredTiltLocal);

            //now add in the yaw input
            //further manipulations need to use the already calculated desiredrotationglobal as a starting point
            float3 desiredForward = math.forward(desiredRotationLocal); // Forward direction from desired rotation
            float3 desiredUp = math.rotate(desiredRotationLocal, math.up()); // Up direction derived from desired rotation

            float yawInput = math.radians(craftInput.YawVector * tuning.yawSpeed); // Convert yaw input to radians
            quaternion yawSpin = quaternion.AxisAngle(desiredUp, yawInput); // Yaw around the desired up axis

            // Step 3: Apply the yaw spin to the forward direction of the desired rotation
            float3 newForward = math.rotate(yawSpin, desiredForward); // Rotate the forward vector using the yaw input

            // Step 4: Create the final spin rotation using LookRotationSafe
            desiredRotationWithYawInput = quaternion.LookRotationSafe(newForward, desiredUp); // Align forward and up

        }
        else
        {
            // Get input and define tilt angle - since the orientation of the game object is not yet considered, 
            // and the resulting quaternion desiredTiltGlobal should be conceptualized as a global rotation from identity - not useful yet
            float tiltAngleX = math.radians(craftInput.Move.x * maxTiltAngle); // Input-based tilting
            float tiltAngleY = math.radians(craftInput.Move.y * maxTiltAngle); // Add yaw if needed
            quaternion desiredTiltLocal = quaternion.Euler(new Vector3(tiltAngleY, 0, -tiltAngleX));

            // Step 1: Get current forward direction
            Vector3 currentForward = localTransform.Forward();

            // Step 2: Project forward onto the XZ plane
            Vector3 currentForwardXZ = new Vector3(currentForward.x, 0, currentForward.z).normalized;

            // Step 3: Add yaw input to the spin quaternion
            float yawInput = math.radians(craftInput.YawVector * tuning.yawSpeed); // Convert yaw input to radians
            quaternion yawSpin = quaternion.AxisAngle(math.up(), yawInput); // Yaw rotation around global up
            quaternion forwardSpin = quaternion.LookRotationSafe(currentForwardXZ, math.up()); // Spin based on projected forward
            quaternion spin = math.mul(yawSpin, forwardSpin); // Combine yaw and forward alignment

            // Step 4: Apply the combined spin to the desired tilt in local space
            desiredRotationLocal = math.mul(spin, desiredTiltLocal);

            desiredRotationWithYawInput = desiredRotationLocal;
        }

        // 5. Update the TargetRotation component
        targetRotationComponent.Value = desiredRotationWithYawInput;

        // 6. Calculate the delta rotation (error) for stabilization
        quaternion deltaRotation = math.mul(math.conjugate(localTransform.Rotation), desiredRotationWithYawInput);

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

