using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))] // Ensure we run after all transforms are updated
[BurstCompile]
partial struct CameraSystem : ISystem
{
    // Offset behind and above the player
    static readonly float3 k_Offset = new float3(0, 10, -30);

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Only run once we’re in game
        state.RequireForUpdate<NetworkStreamInGame>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var camera = Camera.main;
        if (camera == null)
            return;

        float deltaTime = SystemAPI.Time.DeltaTime;

        // Tune this to increase or decrease smoothing
        // Higher => more damped / less jitter, but more lag
        const float smoothingSpeed = 10f;

        // Query: we want both the predicted transform (LocalToWorld) and our smoothing data
        foreach (var (localToWorld, followData) in
                 SystemAPI.Query<RefRO<LocalToWorld>, RefRW<cameraFollowPosition>>()
                          .WithAll<GhostOwnerIsLocal>())
        {
            // 1. Extract raw predicted transform
            float3 rawPos = localToWorld.ValueRO.Position;
            float3 rawFwd = localToWorld.ValueRO.Forward;

            // 2. We only want yaw for the camera (ignore pitch/roll).
            float3 yawFwd = math.normalize(new float3(rawFwd.x, 0f, rawFwd.z));

            // 3. Compute desired position behind the craft
            float3 desiredPos = rawPos - yawFwd * math.abs(k_Offset.z) + new float3(0f, k_Offset.y, 0f);

            // 4. Smooth from old "presentation" transform toward the new raw transform
            //    (Exponential smoothing: 1 - e^(-speed * dt))
            float factor = 1f - math.exp(-smoothingSpeed * deltaTime);

            float3 oldPos = followData.ValueRW.SmoothedPosition;
            quaternion oldRot = followData.ValueRW.SmoothedRotation;

            // If first frame, initialize to raw instantly so we don't lerp from zero
            if (math.all(oldPos == float3.zero) && math.all(oldRot.value == float4.zero))
            {
                oldPos = desiredPos;
                oldRot = quaternion.LookRotationSafe(yawFwd, math.up());
            }

            float3 newPos = math.lerp(oldPos, desiredPos, factor);

            // For rotation, we only track yaw as well
            quaternion desiredRot = quaternion.LookRotationSafe(yawFwd, math.up());
            quaternion newRot = math.slerp(oldRot, desiredRot, factor);

            // 5. Store updated "presentation" in the component for next frame
            followData.ValueRW.SmoothedPosition = newPos;
            followData.ValueRW.SmoothedRotation = newRot;

            // 6. Finally, position the camera from the smoothed transform
            camera.transform.position = newPos;
            camera.transform.rotation = newRot;
        }
    }
}
