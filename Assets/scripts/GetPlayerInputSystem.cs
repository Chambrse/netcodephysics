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

}

//client only
[UpdateInGroup(typeof(InitializationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class GetPlayerInputSystem : SystemBase
{
    private PlayerControls _playerControls;
    private MenuController _menuController;

    protected override void OnCreate()
    {
        RequireForUpdate<CraftInput>();

        // Initialize PlayerControls
        _playerControls = new PlayerControls();
        _playerControls.Enable();

        GameObject menuControllerPrefab = Resources.Load<GameObject>("prefabs/MenuController");
        GameObject menuControllerObject = Object.Instantiate(menuControllerPrefab);

        //get MenuController component
        _menuController = menuControllerObject.GetComponent<MenuController>();

        // Pass PlayerControls to the MenuController
        _menuController.Initialize(_playerControls);
    }

    protected override void OnUpdate()
    {
        var curMoveInput = _playerControls.Hover.Move.ReadValue<Vector2>();
        var curYawInput = _playerControls.Hover.YawVector.ReadValue<float>();

        foreach (var craftInput in SystemAPI.Query<RefRW<CraftInput>>().WithAll<PlayerTag>())
        {
            craftInput.ValueRW = new CraftInput
            {
                Move = curMoveInput,
                YawVector = curYawInput,
                Thrust = _playerControls.Hover.Thrust.ReadValue<float>(),
                Brakes = _playerControls.Hover.Brakes.ReadValue<float>()
            };
        }
    }

    protected override void OnDestroy()
    {
        _playerControls.Disable();
    }
}
