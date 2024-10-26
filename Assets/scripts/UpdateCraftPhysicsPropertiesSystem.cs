using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Unity.Physics;
using Unity.Collections;

public struct CraftPhysicsProperties : IComponentData
{
    public Vector3 centerOfMass;
    public float3 inertiaTensor;
    public Vector3 centerOfPressure;
    public float totalMass;
}

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
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

        //var deltaTime = SystemAPI.Time.DeltaTime;
        //new UpdateCraftPhysicsPropertiesJob
        //{
        //    DeltaTime = deltaTime
        //}.ScheduleParallel(state.Dependency);



        var  errorjob = new setCurrentStateOnVectorPIDs
        {
            physicsVelocityLookup = physicsVelocityLookup
        }.ScheduleParallel(state.Dependency);

        errorjob.Complete();

        //foreach (var (PIDOutputs_Vector pIDOutputs_Vector, Parent parent) in SystemAPI.Query<<RefRW<PIDOutputs_Vector>, <RefRW<Parent>>())
        //{

        //    return;
        //}

        foreach (var (PIV, parent, linPID) in
            SystemAPI.Query<RefRW<PIDInputs_Vector>, RefRO<Parent>>()
            .WithAll<linPIDTag>()
            .WithEntityAccess())
        {
            PIV.ValueRW.DeltaVectorError = physicsVelocityLookup[parent.ValueRO.Value].Linear;
        }

    }
}

//[BurstCompile]
//public partial struct UpdateCraftPhysicsPropertiesJob : IJobEntity
//{
//    public float DeltaTime;

//    [BurstCompile]
//    private void Execute(ref CraftPhysicsProperties physicsProperties)
//    {
//        physicsProperties.centerOfMass = new Vector3(0, 0, 0);
//        physicsProperties.inertiaTensor = new float3(1, 1, 1);
//        physicsProperties.centerOfPressure = new Vector3(0, 0, 0);
//        physicsProperties.totalMass = 1;
//    }
//}

[BurstCompile]
public partial struct setCurrentStateOnVectorPIDs : IJobEntity
{

    // entitymanager
    // public EntityManager entityManager;
    [ReadOnly] public ComponentLookup<PhysicsVelocity> physicsVelocityLookup;

    [BurstCompile]
    private void Execute(ref PIDInputs_Vector PIV, in Parent parent, in rotPIDTag rotPIDTag)
    {
        PIV.DeltaVectorError = physicsVelocityLookup[parent.Value].Angular;
        // PIS.Velocity = physicsVelocityLookup[parent.Value].Linear.y;
    }
}



