using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Collections;
using Unity.Physics;

// Example custom group name
[UpdateInGroup(typeof(CustomPresentationSystemGroup))]
[UpdateAfter(typeof(LinearAccelerationSystem))]
[BurstCompile]
public partial struct ThrusterSystem : ISystem
{
    private ComponentLookup<LinearAcceleration> LinearAccelerationLookup;
    private ComponentLookup<AngularAcceleration> AngularAccelerationLookup;
    private ComponentLookup<LocalTransform> TransformLookup;
    private ComponentLookup<PhysicsMass> PhysicsMassLookup;
    private ComponentLookup<MovementMode> MovementModeLookup;
    private ComponentLookup<ExhaustTag> ExhaustTagLookup;

    private BufferLookup<Child> ChildBufferLookup;
    private BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;

    // NEW: For non-uniform scaling, we use PostTransformMatrix
    private ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialize Lookups
        LinearAccelerationLookup = state.GetComponentLookup<LinearAcceleration>(true);
        AngularAccelerationLookup = state.GetComponentLookup<AngularAcceleration>(true);
        PhysicsMassLookup = state.GetComponentLookup<PhysicsMass>(true);
        MovementModeLookup = state.GetComponentLookup<MovementMode>(true);
        ExhaustTagLookup = state.GetComponentLookup<ExhaustTag>(true);

        LinkedEntityGroupLookup = state.GetBufferLookup<LinkedEntityGroup>(true);
        ChildBufferLookup = state.GetBufferLookup<Child>(true);
        TransformLookup = state.GetComponentLookup<LocalTransform>(false);

        // IMPORTANT: PostTransformMatrix
        PostTransformMatrixLookup = state.GetComponentLookup<PostTransformMatrix>(false);

        // Require this tag so system only updates if an entity with EngineTag exists
        state.RequireForUpdate<EngineTag>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup code if needed
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Update lookups each frame
        LinearAccelerationLookup.Update(ref state);
        AngularAccelerationLookup.Update(ref state);
        TransformLookup.Update(ref state);
        ChildBufferLookup.Update(ref state);
        PhysicsMassLookup.Update(ref state);
        MovementModeLookup.Update(ref state);
        LinkedEntityGroupLookup.Update(ref state);
        ExhaustTagLookup.Update(ref state);

        PostTransformMatrixLookup.Update(ref state);  // NEW

        var thrusterJob = new UpdateThrusters
        {
            LinearAccelerationLookup = LinearAccelerationLookup,
            AngularAccelerationLookup = AngularAccelerationLookup,
            PhysicsMassLookup = PhysicsMassLookup,
            MovementModeLookup = MovementModeLookup,
            ExhaustTagLookup = ExhaustTagLookup,
            LinkedEntityGroupLookup = LinkedEntityGroupLookup,
            ChildBufferLookup = ChildBufferLookup,
            TransformLookup = TransformLookup,

            // Pass in PostTransformMatrix Lookup
            PostTransformMatrixLookup = PostTransformMatrixLookup
        };

        state.Dependency = thrusterJob.Schedule(state.Dependency);
    }
}


[BurstCompile]
public partial struct UpdateThrusters : IJobEntity
{
    [ReadOnly] public ComponentLookup<LinearAcceleration> LinearAccelerationLookup;
    [ReadOnly] public ComponentLookup<AngularAcceleration> AngularAccelerationLookup;
    [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
    [ReadOnly] public ComponentLookup<MovementMode> MovementModeLookup;
    [ReadOnly] public ComponentLookup<ExhaustTag> ExhaustTagLookup;

    [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;
    [ReadOnly] public BufferLookup<Child> ChildBufferLookup;

    public ComponentLookup<LocalTransform> TransformLookup;

    // NEW: We'll read/write PostTransformMatrix
    public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;

    [BurstCompile]
    private void Execute(Entity engineEntity, in Parent engineParent, in EngineTag engineTag)
    {
        var parentEntity = engineParent.Value;

        if (!LinearAccelerationLookup.HasComponent(parentEntity) ||
            !AngularAccelerationLookup.HasComponent(parentEntity) ||
            !TransformLookup.HasComponent(parentEntity) ||
            !TransformLookup.HasComponent(engineEntity))
        {
            return;
        }

        // 1) Retrieve parent's data
        float3 linearAcceleration = LinearAccelerationLookup[parentEntity].totalLocalLinearAcceleration;
        float3 angularAcceleration = AngularAccelerationLookup[parentEntity].angularAcceleration;
        var craftTransform = TransformLookup[parentEntity];
        var engineTransform = TransformLookup[engineEntity];
        var craftPhysicsMass = PhysicsMassLookup[parentEntity];
        MovementModes craftMovementMode = MovementModeLookup[parentEntity].mode;

        // 2) Compute thruster angles (same logic as before)
        float3 thrusterOffsetFromCraftCG = engineTransform.Position - craftPhysicsMass.Transform.pos;

        float rearThrusterContributionToLinearAccelY = 1f / 6f;
        float rearThrusterContributionToRotation = 0.25f;

        float3 invInertia = craftPhysicsMass.InverseInertia;
        float3 torqueWithInertia = angularAcceleration / invInertia;
        float3 torque = torqueWithInertia; // simplified for now

        float rollForce = (torque.z * rearThrusterContributionToRotation) / thrusterOffsetFromCraftCG.x;
        float pitchForce = (torque.x * rearThrusterContributionToRotation) / -thrusterOffsetFromCraftCG.z;
        float yawForce = (torque.y * rearThrusterContributionToRotation) / thrusterOffsetFromCraftCG.x;

        // Example clamp logic for hover modes
        float rollForceClamped = math.max(rollForce, 0);
        float pitchForceClamped = math.max(pitchForce, 0);

        float rollForceForCalc = ( craftMovementMode == MovementModes.bellyFirst)
                                  ? rollForceClamped
                                  : rollForce;

        float pitchForceForCalc = (craftMovementMode == MovementModes.bellyFirst)
                                  ? pitchForceClamped
                                  : pitchForce;

        float downwardThrust = linearAcceleration.y * rearThrusterContributionToLinearAccelY;
        float rearwardThrust = linearAcceleration.z * rearThrusterContributionToLinearAccelY;

        float yComponent = downwardThrust + rollForceForCalc + pitchForceForCalc;
        float zComponent = rearwardThrust - yawForce;

        float3 force = new float3(0f, yComponent, zComponent);
        float mag = math.length(force);

        // 3) Compute thruster tilt
        float angleRadians = 0f;
        if (mag > 1e-5f)
            angleRadians = math.atan2(force.z, force.y);

        // Apply rotation around local X
        engineTransform.Rotation = quaternion.AxisAngle(math.right(), angleRadians);
        TransformLookup[engineEntity] = engineTransform;

        // 4) Traverse children to find ExhaustTag and apply non-uniform scaling via PostTransformMatrix
        if (ChildBufferLookup.HasBuffer(engineEntity))
        {
            var stack = new NativeList<Entity>(Allocator.Temp);
            stack.Add(engineEntity);

            while (stack.Length > 0)
            {
                var current = stack[stack.Length - 1];
                stack.RemoveAt(stack.Length - 1);

                if (ChildBufferLookup.HasBuffer(current))
                {
                    var childBuffer = ChildBufferLookup[current];
                    for (int i = 0; i < childBuffer.Length; i++)
                    {
                        var childEntity = childBuffer[i].Value;

                        // If child has ExhaustTag, apply a custom PostTransformMatrix scale
                        if (ExhaustTagLookup.HasComponent(childEntity))
                        {
                            // We need PostTransformMatrix to do non-uniform scaling
                            if (PostTransformMatrixLookup.HasComponent(childEntity))
                            {
                                // For example, scale the Y axis by (1 + mag * 0.01f)
                                var postTx = PostTransformMatrixLookup[childEntity];

                                float extraY = 1.5f + math.log(mag);
                                // Build a matrix that does uniform scale 1 on X,Z and extraY on Y
                                postTx.Value = float4x4.Scale(new float3(1f, extraY, 1f));

                                // or if you want additive, you'd do something different

                                PostTransformMatrixLookup[childEntity] = postTx;
                            }
                            // If the entity doesn't have PostTransformMatrix, you might want to add it at bake time
                            // so that the transform system can handle it properly.
                        }

                        // Push for grandchildren
                        stack.Add(childEntity);
                    }
                }
            }
            stack.Dispose();
        }
    }
}

