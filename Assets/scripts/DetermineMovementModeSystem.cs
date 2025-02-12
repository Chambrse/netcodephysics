using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Burst;
using Unity.Physics;

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

public struct MovementMode : IInputComponentData
{
    public MovementModes mode;
    public HoverMode_Player hoverMode;
}

[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
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
    private void Execute(ref MovementMode mode, in CraftInput input)
    {
        if (mode.hoverMode == HoverMode_Player.VTOL)
        {
            // Check if the forward velocity exceeds the threshold
            if (input.Thrust > math.EPSILON) // Use abs if direction doesn't matter
            {
                mode.mode = MovementModes.Fly;
            }
            else
            {
                mode.mode = MovementModes.VTOL;
            }
        }
        else
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


