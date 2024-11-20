using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[BurstCompile]
partial struct CameraSystem : ISystem
{
    public static readonly float3 k_CameraOffset = new float3(0, 10, -30); // Global offset behind the player

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var camera = UnityEngine.Camera.main;
        if (camera == null) return;

        // Query for the LocalToWorld of the local player's entity
        foreach (var localToWorld in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<GhostOwnerIsLocal>())
        {
            // Extract the player's position and forward direction
            var playerPosition = localToWorld.ValueRO.Position;
            var playerForward = localToWorld.ValueRO.Forward;

            // Remove pitch and roll from the player's forward direction
            var playerYawForward = math.normalize(new float3(playerForward.x, 0, playerForward.z));

            // Calculate the camera position behind the player using the yaw-only forward direction
            var cameraPosition = playerPosition - playerYawForward * math.abs(k_CameraOffset.z) + new float3(0, k_CameraOffset.y, 0);

            // Smoothly move the camera to the new position
            //camera.transform.position = math.lerp(camera.transform.position, cameraPosition, 0.5f);
            camera.transform.position = cameraPosition;

            // Rotate the camera to look at the player, ignoring pitch and roll
            camera.transform.rotation = quaternion.LookRotationSafe(playerYawForward, math.up());
        }
    }
}
