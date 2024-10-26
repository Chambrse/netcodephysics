using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;


public struct TargetRelativeVelocity : IComponentData
{
    public float Value;

    public float targetRelativeVelocityError;
}

public struct PreviousVelocity : IComponentData
{
    public float3 Value;
}


[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(DetermineMovementModeSystem))]
[BurstCompile]
public partial struct DetermineVelocityErrors : ISystem
{

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Require entities with CraftInput to be present for the system to run
        state.RequireForUpdate<CraftInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        //loop through all pid entities with linear
        foreach (var (pidInputs, parent) in SystemAPI.Query<RefRW<PIDInputs_Vector>, Parent>().WithAll<linPIDTag>())
        {
            // to do:
            // calculate error based on movement mode and per PID controller.
            Entity parentEntity = parent.Value;

            PhysicsVelocity physicsVelocity = SystemAPI.GetComponent<PhysicsVelocity>(parentEntity);

            PreviousVelocity previousVelocity = SystemAPI.GetComponent<PreviousVelocity>(parentEntity);

            pidInputs.ValueRW.VectorError = -physicsVelocity.Linear;

            float3 currentVelocity = physicsVelocity.Linear;

            // calculate the change in velocity
            float3 deltaVelocity = currentVelocity - previousVelocity.Value;

            // calculate the acceleration
            pidInputs.ValueRW.DeltaVectorError = deltaVelocity / SystemAPI.Time.DeltaTime;

            previousVelocity.Value = currentVelocity;

        }
    }
}