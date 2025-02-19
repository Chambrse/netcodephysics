using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.NetCode;
using System.Diagnostics;
using System;
using Unity.Mathematics;

public struct CraftInput : IInputComponentData
{
    public Vector2 Move;

    public float YawVector;

    public float Thrust;

    public float Brakes;

}

[UpdateInGroup(typeof(CustomInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
public partial class ThinClientInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkId>();

    }

    protected override void OnUpdate()
    {
        // Check if the connection has no command target set yet, if not then create it (this is the dummy thin client player)
        if (SystemAPI.TryGetSingleton<CommandTarget>(out var commandTarget) && commandTarget.targetEntity == Entity.Null)
            CreateThinClientPlayer();

        // Thin clients do not spawn anything so there will be only one PlayerInput component
        foreach (var (inputData, movementMode) in SystemAPI.Query<RefRW<CraftInput>,RefRW<MovementMode>>())
        {
            inputData.ValueRW.Brakes = 1;
            movementMode.ValueRW.mode = MovementModes.bellyFirst;
            movementMode.ValueRW.locked = true;

        }
    }

    void CreateThinClientPlayer()
    {
        // Create dummy entity to store the thin clients inputs
        // When using IInputComponentData the entity will need the input component and its generated
        // buffer, the GhostOwner set up with the local connection ID and finally the
        // CommandTarget needs to be manually set.
        var ent = EntityManager.CreateEntity();
        EntityManager.AddComponent<CraftInput>(ent);
        EntityManager.AddComponent<MovementMode>(ent);

        var connectionId = SystemAPI.GetSingleton<NetworkId>().Value;
        EntityManager.AddComponentData(ent, new GhostOwner() { NetworkId = connectionId });
        EntityManager.AddComponent<InputBufferData<CraftInput>>(ent);
        EntityManager.AddComponent<InputBufferData<MovementMode>>(ent);

        // NOTE: The server also has to manually set the command target for the thin client player
        // even though auto command target is used on the player prefab (and normal clients), see
        // SpawnPlayerSystem.
        SystemAPI.SetSingleton(new CommandTarget { targetEntity = ent });
    }
}


//client only
[UpdateInGroup(typeof(CustomInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GetPlayerInputSystem : SystemBase
{
    private PlayerControls _playerControls;
    private MenuController _menuController;

    private bool toggleLock = false;

    private float lastYawInput = 0;

    protected override void OnCreate()
    {
        RequireForUpdate<CraftInput>();
        RequireForUpdate<MovementMode>();

        // Initialize PlayerControls
        _playerControls = new PlayerControls();
        _playerControls.Enable();

        _playerControls.Hover.hoverLock.performed += HoverLock_performed;



        GameObject menuControllerPrefab = Resources.Load<GameObject>("prefabs/MenuController");
        GameObject menuControllerObject = UnityEngine.Object.Instantiate(menuControllerPrefab);

        //get MenuController component
        _menuController = menuControllerObject.GetComponent<MenuController>();

        // Pass PlayerControls to the MenuController
        _menuController.Initialize(_playerControls);
    }

    private void HoverLock_performed(InputAction.CallbackContext ctx)
    {

            // swap hover mode
            toggleLock = true;
        
    }

    protected override void OnUpdate()
    {
        var curMoveInput = _playerControls.Hover.Move.ReadValue<Vector2>();
        var curYawInput = _playerControls.Hover.YawVector.ReadValue<float>();
        var Brakes = _playerControls.Hover.Brakes.ReadValue<float>();

        //lerp
        float lerpedYaw = math.lerp(lastYawInput, curYawInput, 0.01f);
        lastYawInput = lerpedYaw;

        foreach (var (craftInput, movementMode) in SystemAPI.Query<RefRW<CraftInput>, RefRW<MovementMode>>().WithAll<GhostOwnerIsLocal>())
        {
            
            craftInput.ValueRW = new CraftInput
            {
                Move = curMoveInput,
                YawVector = lerpedYaw,
                Thrust = _playerControls.Hover.Thrust.ReadValue<float>(),
                Brakes = Brakes,
            };

            movementMode.ValueRW = new MovementMode { 
                locked = toggleLock ? !movementMode.ValueRO.locked : movementMode.ValueRO.locked
            };

        }


        toggleLock = false;
    }

    protected override void OnDestroy()
    {
        _playerControls.Disable();
    }
}
