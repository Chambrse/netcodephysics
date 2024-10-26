using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using System.Net;

//debugsettings icomponentdata
public struct DebugSettings : IComponentData
{
    public bool DebugMode;
    public bool ShowTargetRotation;
}


//debugsystem systembase
[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
//client only
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DebugVizSystem : ISystem
{

    private DebugSettings _debugSettings;
    private EntityManager _entityManager;

    //requireforupdate debugsettings
    public void OnCreate(ref SystemState state)
    {

        //_entityManager = state.EntityManager;

        // Create an EntityQuery to check if any entity has the DebugSettings component
        EntityQuery debugSettingsQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<DebugSettings>());


        // Check if the DebugSettings singleton already exists
        if (debugSettingsQuery.IsEmpty)
        {
            // Error is produced referencing this line complaining that getsingletonentity didn't find a match.
            Entity debugSettingsEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(debugSettingsEntity, new DebugSettings
            {
                DebugMode = true,          // Initial value for DebugMode
                ShowTargetRotation = true  // Initial value for ShowTargetRotation
            });

            // Optionally, you can name the entity for easier debugging
            state.EntityManager.SetName(debugSettingsEntity, "DebugSettingsSingleton");
        }

        debugSettingsQuery.Dispose();

        state.RequireForUpdate<DebugSettings>();
        state.RequireForUpdate<PlayerTag>();

        //entitymanager

        _debugSettings = SystemAPI.GetSingleton<DebugSettings>();
    }

    [BurstCompile]
    //[BurstCompile(Disable = true)]
    private void UpdateRotationVisualizations(ref SystemState state)
    {
        foreach (var (localTransform, parent, rotationViz) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<Parent>>()
            .WithAll<RotationViz>()
            .WithEntityAccess())
        {
            // Enable or disable based on DebugSettings
            if (_debugSettings.ShowTargetRotation)
            {
                // If disabled, remove the Disabled component to make it active
                if (state.EntityManager.HasComponent<Disabled>(rotationViz))
                {
                    state.EntityManager.RemoveComponent<Disabled>(rotationViz);
                }

                Entity playerEntity = parent.ValueRO.Value;

                //get targetrotation component from playerEntity
                TargetRotation targetRotation = state.EntityManager.GetComponentData<TargetRotation>(playerEntity);
                LocalToWorld playerLocaltoWorld = state.EntityManager.GetComponentData<LocalToWorld>(playerEntity);

                LocalTransform currentTransform = localTransform.ValueRO;

                quaternion parentRotation = math.inverse(playerLocaltoWorld.Rotation);
                quaternion localRotation = math.mul(parentRotation, targetRotation.Value);

                state.EntityManager.SetComponentData<LocalTransform>(rotationViz, new LocalTransform
                {
                    Position = currentTransform.Position, // Keep the same position
                    Rotation = localRotation,               // Update the rotation
                    Scale = currentTransform.Scale        // Keep the same scale
                });

            }
            else
            {
                // Disable the visualization if active
                if (!state.EntityManager.HasComponent<Disabled>(rotationViz))
                {
                    state.EntityManager.AddComponent<Disabled>(rotationViz);
                }
            }
        }
    }


    [BurstCompile]
    private void UpdateVectorVisualizations(ref SystemState state)
    {

        var ecb = new EntityCommandBuffer(Allocator.TempJob);


        foreach (var (localTransform, parent, VectorViz) in
    SystemAPI.Query<RefRW<LocalTransform>, RefRO<Parent>>()
    .WithAll<LinearVectorViz>()
    .WithEntityAccess())
        {
            //// If disabled, remove the Disabled component to make it active
            //if (state.EntityManager.HasComponent<Disabled>(VectorViz))
            //{
            //    state.EntityManager.RemoveComponent<Disabled>(VectorViz);
            //}



            Entity playerEntity = parent.ValueRO.Value;

            float localVerticalAcceleration = state.EntityManager.GetComponentData<LinearAcceleration>(playerEntity).localLinearAcceleration.y;

            // Get the current LocalTransform values
            //LocalTransform currentTransform = localTransform.ValueRW;

            //state.EntityManager.SetComponentData(VectorViz, new LocalTransform
            //{
            //    Position = currentTransform.Position, // Keep the same position
            //    Rotation = currentTransform.Rotation,               // Update the rotation
            //    Scale = currentTransform.Scale  // Scale only Y-axis
            //});


            float4x4 nonUniformScaleMatrix = new float4x4(
    new float4(1, 0, 0, 0),                        // X-axis (1 for no scaling)
    new float4(0, localVerticalAcceleration, 0, 0),// Y-axis (scaled by localVerticalAcceleration)
    new float4(0, 0, 1, 0),                        // Z-axis (1 for no scaling)
    new float4(0, 0, 0, 1)                         // W (no translation in this matrix)
            );

            // Add or set the PostTransformMatrix for non-uniform scaling using the ECB
            if (state.EntityManager.HasComponent<PostTransformMatrix>(VectorViz))
            {
                // If PostTransformMatrix already exists, set its value
                ecb.SetComponent(VectorViz, new PostTransformMatrix
                {
                    Value = nonUniformScaleMatrix
                });
            }
            else
            {
                // Otherwise, add the PostTransformMatrix component
                ecb.AddComponent(VectorViz, new PostTransformMatrix
                {
                    Value = nonUniformScaleMatrix
                });
            }
            //// Disable the visualiza;tion if VectorViz
            //if (!state.EntityManager.HasComponent<Disabled>(VectorViz))
            //{
            //    state.EntityManager.AddComponent<Disabled>(VectorViz);
            //}



        }

        // Execute the commands that were queued in the ECB 
        ecb.Playback(state.EntityManager);
        ecb.Dispose(); // Dispose the command buffer once done
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        _debugSettings = SystemAPI.GetSingleton<DebugSettings>();

        if (!_debugSettings.DebugMode)
        {
            return;
        }

        UpdateRotationVisualizations(ref state);
        UpdateVectorVisualizations(ref state);


    }
}

