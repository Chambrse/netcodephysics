using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;

public enum MovementModes
{
    bellyFirst,
    Fly
}

public struct MovementMode : IInputComponentData
{
    public MovementModes mode;
    public bool locked;
}

[UpdateInGroup(typeof(CustomInputSystemGroup))]
[UpdateAfter(typeof(GetPlayerInputSystem))]
public partial struct DetermineMovementModeSystem : ISystem
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
        var deltaTime = SystemAPI.Time.DeltaTime;
        new AssignModeJob
        {
            DeltaTime = deltaTime
        }.Schedule();
    }
}

[BurstCompile]
public partial struct AssignModeJob : IJobEntity
{
    public float DeltaTime;

    [BurstCompile]
    private void Execute(ref MovementMode mode, in CraftInput input, in PhysicsVelocity physicsVelocity)
    {
        MovementMode oldMovementMode = mode;

        if (oldMovementMode.locked)
        {

        } else
        {
            mode.mode = MovementModes.Fly;
        }

    }
}


