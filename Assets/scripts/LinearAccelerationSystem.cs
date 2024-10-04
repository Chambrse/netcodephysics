using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Burst;
using Unity.Transforms;
// using Unity.Jobs;
using Unity.Collections;
using Unity.Physics.Systems;
using Unity.NetCode;
// using System.Diagnostics;
using UnityEngine;

public struct VerticalAcceleration : IComponentData
{
    public float3 verticalAcceleration;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(PIDSystem))]
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

        // physicsWorldLookup.Update(ref state);

        Debug.Log("LinearAccelerationSystem OnUpdate");
        // Initialize ComponentLookup for angular acceleration
        // var angularAccelerationLookup = state.GetComponentLookup<AngularAcceleration>();
        var linearAccelerationLookup = state.GetComponentLookup<VerticalAcceleration>();

        // First job: getAngularAcceleration using ComponentLookup instead of EntityManager
        var getLinearAccelerationJob = new getVerticalAcceleration
        {
            // angularAccelerationLookup = angularAccelerationLookup
            linearAccelerationLookup = linearAccelerationLookup
        };

        state.Dependency = getLinearAccelerationJob.Schedule(state.Dependency);


        var job = new ApplyHoverForce
        {
            DeltaTime = SystemAPI.Time.DeltaTime
        };

        state.Dependency = job.Schedule(state.Dependency);
    }
}

[BurstCompile]
public partial struct getVerticalAcceleration : IJobEntity
{
    // [ReadOnly] public ComponentLookup<PIDOutputs_Vector> pidOutputsLookup;
    public ComponentLookup<VerticalAcceleration> linearAccelerationLookup;

    [BurstCompile]
    private void Execute(
        in PIDOutputs_Scalar pidOutputs,
        ref Parent parent)
    {
        // Retrieve the desired angular acceleration from the PID outputs
        float3 desiredVerticalAcceleration = pidOutputs.linearAcceleration;
        if (linearAccelerationLookup.HasComponent(parent.Value))
        {
            linearAccelerationLookup[parent.Value] = new VerticalAcceleration { verticalAcceleration = desiredVerticalAcceleration };
        }
    }
}

[BurstCompile]
public partial struct ApplyHoverForce : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(
        in VerticalAcceleration verticalAcceleration,
        ref PhysicsVelocity physicsVelocity,
        in PhysicsMass physicsMass,
        in CraftInput craftInput)
    {

        // // gravity
        // var gravity = new float3(0, -9.81f, 0);

        // var mg = 1/physicsMass.InverseMass * gravity;

        physicsVelocity.Linear.y += (verticalAcceleration.verticalAcceleration.y + 9.81f +(craftInput.Thrust*100)) * DeltaTime;
            // physicsVelocity.Linear.y += 10f * DeltaTime;

        // Debug.Print("mg: " + mg);
        // Debug.Log("mg: " + mg);
    }
}
