using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.NetCode;

public struct CraftInput : IInputComponentData
{
    public Vector2 Move;

    public float YawVector;

    public float Thrust;

    public float Brakes;

    public HoverMode_Player hoverMode;

}

//client only
[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GetPlayerInputSystem : SystemBase
{
    private PlayerControls _playerControls;
    private MenuController _menuController;

    private bool _changeHoverMode = false;

    protected override void OnCreate()
    {
        RequireForUpdate<CraftInput>();
        RequireForUpdate<MovementMode>();

        // Initialize PlayerControls
        _playerControls = new PlayerControls();
        _playerControls.Enable();

        _playerControls.Hover.Mode.performed += ctx => onModeChange(ctx);

        GameObject menuControllerPrefab = Resources.Load<GameObject>("prefabs/MenuController");
        GameObject menuControllerObject = Object.Instantiate(menuControllerPrefab);

        //get MenuController component
        _menuController = menuControllerObject.GetComponent<MenuController>();

        // Pass PlayerControls to the MenuController
        _menuController.Initialize(_playerControls);
    }

    private void onModeChange(InputAction.CallbackContext ctx)
    {

            // swap hover mode
            _changeHoverMode = true;
        
    }

    protected override void OnUpdate()
    {
        var curMoveInput = _playerControls.Hover.Move.ReadValue<Vector2>();
        var curYawInput = _playerControls.Hover.YawVector.ReadValue<float>();
        var Brakes = _playerControls.Hover.Brakes.ReadValue<float>();

        foreach (var (craftInput, movementMode) in SystemAPI.Query<RefRW<CraftInput>, RefRW<MovementMode>>().WithAll<PlayerTag>())
        {

            var currentPlayerMovementMode = craftInput.ValueRW.hoverMode;

            var swapHovermode = (currentPlayerMovementMode == HoverMode_Player.VTOL ? HoverMode_Player.Locked : HoverMode_Player.VTOL);


            craftInput.ValueRW = new CraftInput
            {
                Move = curMoveInput,
                YawVector = curYawInput,
                Thrust = _playerControls.Hover.Thrust.ReadValue<float>(),
                Brakes = Brakes,
                hoverMode = _changeHoverMode ? swapHovermode : currentPlayerMovementMode
            };

        }


        _changeHoverMode = false;
    }

    protected override void OnDestroy()
    {
        _playerControls.Disable();
    }
}
