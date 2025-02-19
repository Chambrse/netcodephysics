using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Burst;
using Unity.Transforms;
using UnityEngine;
public struct VerticalAcceleration : IComponentData
{
    public float verticalAcceleration;
    public float localVerticalAcceleration;
}

public struct LinearAcceleration : IComponentData
{
    public float3 linearAcceleration;
    public float3 localLinearAcceleration;
    public float3 totalLocalLinearAcceleration;
}

[UpdateInGroup(typeof(CustomPhysicsSystemGroup))]
[UpdateAfter(typeof(RotationTorqueSystem))]
[BurstCompile]
public partial struct LinearAccelerationSystem : ISystem
{

    // ComponentLookup<PhysicsWorldSingleton> physicsWorldLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Initialization code here
        // physicsWorldLookup = state.GetComponentLookup<PhysicsWorldSingleton>(true);

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // Cleanup code here
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new ApplyHoverForce
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.Schedule(state.Dependency);
    }
}



//[BurstCompile]
public partial struct ApplyHoverForce : IJobEntity
{
    public float DeltaTime;

    //[BurstCompile]
    private void Execute(
        //ref VerticalAcceleration verticalAcceleration,
        ref LinearAcceleration linearAcceleration,
        ref PhysicsVelocity physicsVelocity,
        in PhysicsMass physicsMass,
        in LocalTransform localTransform,
        in MovementMode movementMode,
        in CraftInput craftInput,
        in CraftTuning craftTuning,
        in AeroForces aeroForces)
    {
        float3 dragVector = aeroForces.linearAeroForces;
        // get rotation
        quaternion rotation = localTransform.Rotation;

        // Define the world +y direction as a vector
        float3 worldUp = new float3(0, 1, 0);

        // Rotate worldUp vector into the entity's local space using the rotation quaternion
        float3 globalUp = math.mul(rotation, worldUp);
        float3 globalForward = math.mul(rotation, new float3(0, 0, 1));

        float totalVerticalAcceleration = 0;
        float totalRearAcceleration = 0;
        if (movementMode.mode == MovementModes.bellyFirst)
        {
            totalVerticalAcceleration = linearAcceleration.linearAcceleration.y
                + (50f * craftInput.Brakes);

            totalRearAcceleration += craftInput.Thrust * craftTuning.maxThrust;
        } else if (movementMode.mode == MovementModes.bellyFirst)
        {
            totalVerticalAcceleration = linearAcceleration.linearAcceleration.y + 9.81f;
        } else if (movementMode.mode == MovementModes.Fly)
        {
            totalRearAcceleration += craftInput.Thrust * craftTuning.maxThrust;
        }
    
        float localVerticalAccelerationMagnitude = totalVerticalAcceleration / math.dot(globalUp, worldUp);

        // Calculate total acceleration vector in world space
        float3 totalAcceleration =
            (globalUp * localVerticalAccelerationMagnitude) + (globalForward * totalRearAcceleration);

        float3 localTotalAcceleration= math.mul(math.inverse(localTransform.Rotation), totalAcceleration);

        linearAcceleration.totalLocalLinearAcceleration = localTotalAcceleration;
        // Apply total acceleration to physics velocity
        physicsVelocity.Linear += (totalAcceleration + dragVector) * DeltaTime; 
    }
}
