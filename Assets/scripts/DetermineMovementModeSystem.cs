using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;

public enum MovementModes
{
    Hover,
    Hover_Stopping,
    Fly,
    VTOL
}

public enum HoverMode_Player
{
    VTOL,
    Locked
}

public struct MovementMode : IComponentData
{
    public MovementModes mode;
}

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[UpdateAfter(typeof(GetPlayerInputSystem))]
[BurstCompile]
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
    private void Execute(ref MovementMode mode, in CraftInput input)
    {
        if (input.hoverMode == HoverMode_Player.VTOL)
        {

            mode.mode = MovementModes.VTOL;
        } else
        {
            if (input.Brakes > 0)
            {
                mode.mode = MovementModes.Hover_Stopping;
            }
            else
            {
                mode.mode = MovementModes.Hover;
            }
        }
    }
}


