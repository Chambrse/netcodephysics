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


[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateAfter(typeof(GetPlayerInputSystem))]
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
        foreach (var (pidInputs, parent) in SystemAPI.Query<RefRW<PIDInputs_Scalar>, Parent>()) {
            // to do:
            // calculate error based on movement mode and per PID controller.
            Entity parentEntity = parent.Value;

            PhysicsVelocity physicsVelocity = SystemAPI.GetComponent<PhysicsVelocity>(parentEntity);

            PreviousVelocity previousVelocity = SystemAPI.GetComponent<PreviousVelocity>(parentEntity);

            pidInputs.ValueRW.Error = -physicsVelocity.Linear.y;

            float3 currentVelocity = physicsVelocity.Linear;

            // calculate the change in velocity
            float3 deltaVelocity = currentVelocity - previousVelocity.Value;

            // calculate the acceleration
            pidInputs.ValueRW.DeltaError = deltaVelocity.y / SystemAPI.Time.DeltaTime;

            previousVelocity.Value = currentVelocity;

        }

    }
}

// [BurstCompile]
// public partial struct DetermineTargetRelativeVelocityJob : IJobEntity
// {
//     public float DeltaTime;

//     [BurstCompile]
//     private void Execute(
//         in CraftInput craftInput,
//         in MovementMode movementMode,
//         in PhysicsVelocity physicsVelocity,
//         ref TargetRelativeVelocity targetRelativeVelocityComponent
//             )
//     {
//         switch (movementMode.mode)
//         {
//             case MovementModes.Hover:

//                 targetRelativeVelocityComponent.Value = 0;
//                 targetRelativeVelocityComponent.targetRelativeVelocityError = physicsVelocity.Linear.y;

//                 break;
//             case MovementModes.Fly:
//                 // targetRelativeVelocityComponent.Value = craftInput.ForwardVector * craftInput.ForwardSpeed;
//                 break;
//         }
//     }
// }
// [BurstCompile]
// public partial struct AssignModeJob : IJobEntity
// {
//     public float DeltaTime;

//     [BurstCompile]
//     private void Execute(ref MovementMode mode, in CraftInput input)
//     {
//             mode.mode = MovementModes.Hover;
//     }
// }

