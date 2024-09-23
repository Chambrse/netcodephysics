using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;
using UnityEditor.Search;
using Unity.Collections;

public struct CraftPhysicsProperties : IComponentData
{
    public Vector3 centerOfMass;
    public float3 inertiaTensor;
    public Vector3 centerOfPressure;
    public float totalMass;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(DetermineMovementModeSystem))]
[BurstCompile]
public partial struct UpdateCraftPhysicsPropertiesSystem : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // don't know what to put here yet 
        state.RequireForUpdate<CraftPhysicsProperties>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        //physicsvelocitylookup
        var physicsVelocityLookup = state.GetComponentLookup<PhysicsVelocity>(isReadOnly: true);

        var deltaTime = SystemAPI.Time.DeltaTime;
        new UpdateCraftPhysicsPropertiesJob
        {
            DeltaTime = deltaTime
        }.Schedule();

        new setCurrentStateOnControllers
        {
            physicsVelocityLookup = physicsVelocityLookup
        }.Schedule();

    }
}

[BurstCompile]
public partial struct UpdateCraftPhysicsPropertiesJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(ref CraftPhysicsProperties physicsProperties)
    {
            physicsProperties.centerOfMass = new Vector3(0, 0, 0);
            physicsProperties.inertiaTensor = new float3(1, 1, 1);
            physicsProperties.centerOfPressure = new Vector3(0, 0, 0);
            physicsProperties.totalMass = 1;
    }
}

[BurstCompile]
public partial struct setCurrentStateOnControllers : IJobEntity
{

    // entitymanager
    // public EntityManager entityManager;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> physicsVelocityLookup;

    [BurstCompile]
    private void Execute(ref PIDInputs_Vector pidInputs, in Parent parent)
    {
        pidInputs.AngularVelocity = physicsVelocityLookup[parent.Value].Angular;
    }
}

