using Unity.Entities;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

//debugsettings icomponentdata
public struct DebugSettings : IComponentData
{
    public bool DebugMode;
    public bool ShowTargetRotation;
}

public struct DebugVizPrefabs : IComponentData
{
    public Entity rotationVizPrefab;
}

public struct VizLabelComponent : IComponentData
{
    public FixedString64Bytes Label;  // Example: "ShowTargetRotation"
}

//debugsystem systembase
[UpdateInGroup(typeof(InitializationSystemGroup))]
//client only
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct DebugVizSystem : ISystem
{

    private EntityManager EntityManager;

    //requireforupdate debugsettings
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<DebugSettings>();
        state.RequireForUpdate<PlayerTag>();

        //entitymanager
        EntityManager = state.EntityManager;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Debug.Log("DebugVizSystem OnUpdate");

        // Get the DebugSettings and the prefab for rotation visualization
        var debugSettings = SystemAPI.GetSingleton<DebugSettings>();
        var rotationVizPrefab = SystemAPI.GetSingleton<DebugVizPrefabs>().rotationVizPrefab;
        // var ecbSystem = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);


        // Iterate over all player entities
        foreach (var (targetRotation, playerEntity) in SystemAPI.Query<TargetRotation>().WithEntityAccess().WithAll<PlayerTag>())
        {
            if (debugSettings.DebugMode && debugSettings.ShowTargetRotation)
            {
                Debug.Log("ShowTargetRotation is enabled");
                // Check if any child of this player already has a visualization with "ShowTargetRotation" label
                bool vizExists = false;
                Entity vizEntity = Entity.Null;

                foreach (var (labelComponent, parent, entity) in SystemAPI.Query<RefRO<VizLabelComponent>, RefRO<Parent>>().WithEntityAccess())
                {
                    if (parent.ValueRO.Value == playerEntity && labelComponent.ValueRO.Label.Equals("ShowTargetRotation"))
                    {
                        vizExists = true;
                        vizEntity = entity; // Store the entity reference for later updates
                        break;
                    }
                }

                // If the visualization doesn't exist, queue the structural changes
                if (!vizExists)
                {
                    // Instantiate the rotation visualization prefab via the ECB
                    Entity rotationVizEntity = ecb.Instantiate(rotationVizPrefab);

                    // Attach it to the player entity
                    ecb.AddComponent(rotationVizEntity, new Parent { Value = playerEntity });

                    // Set the initial transform (adjust as needed)
                    // ecb.SetComponent(rotationVizEntity, new LocalTransform());

                    // Add the label component to the visualization
                    ecb.AddComponent(rotationVizEntity, new VizLabelComponent
                    {
                        Label = new FixedString64Bytes("ShowTargetRotation")
                    });

                    Debug.Log("Rotation visualization instantiated and attached to player.");
                }
                else
                {
                    // Update the rotation of the existing visualization entity
                    var currentTransform = EntityManager.GetComponentData<LocalTransform>(vizEntity);
                    
                    // Get the parent (playerEntity) LocalToWorld matrix to retrieve the global transform
                    var parentLocalToWorld = EntityManager.GetComponentData<LocalToWorld>(playerEntity);
                    
                    // Calculate the local rotation relative to the parent by applying the inverse of the parent's rotation
                    Quaternion parentRotationInverse = math.inverse(parentLocalToWorld.Rotation);
                    Quaternion localRotation = math.mul(parentRotationInverse, targetRotation.Value);  // Local rotation

                    // Set the updated transform with the new rotation
                    EntityManager.SetComponentData(vizEntity, new LocalTransform
                    {
                        Position = currentTransform.Position, // Keep the same position
                        Rotation = localRotation,               // Update the rotation
                        Scale = currentTransform.Scale        // Keep the same scale
                    });

                    Debug.Log("Rotation visualization with label 'ShowTargetRotation' already exists.");
                }
            }
        }
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

