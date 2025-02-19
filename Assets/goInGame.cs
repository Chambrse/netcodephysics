using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

/// <summary>
/// This allows sending RPCs between a stand alone build and the editor for testing purposes in the event when you finish this example
/// you want to connect a server-client stand alone build to a client configured editor instance. 
/// </summary>
[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
[UpdateInGroup(typeof(CustomInitializaionSystemGroup))]
[CreateAfter(typeof(RpcSystem))]
public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
        state.Enabled = false;
    }
}

// RPC request from client to server for game to go "in game" and send snapshots / inputs
public struct GoInGameRequest : IRpcCommand
{
    public FixedString64Bytes PlayerName;
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
public partial struct GoInGameClientSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Run only on entities with a CraftSpawner component data 
        state.RequireForUpdate<CraftSpawner>();

        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<NetworkId>()
            .WithNone<NetworkStreamInGame>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // (If "Username" key doesn't exist, default to "Unknown").

        string username = PlayerPrefs.GetString("Username", "Unknown");

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (id, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess().WithNone<NetworkStreamInGame>())
        {
            Debug.Log("attempting connect");
            commandBuffer.AddComponent<NetworkStreamInGame>(entity);
            var req = commandBuffer.CreateEntity();
            commandBuffer.AddComponent(req, new GoInGameRequest
            {
                PlayerName = username
            });
            commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = entity });

        }
        commandBuffer.Playback(state.EntityManager);
    }
}

[BurstCompile]
// When server receives go in game request, go in game and delete request
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct GoInGameServerSystem : ISystem
{
    private ComponentLookup<NetworkId> networkIdFromEntity;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CraftSpawner>();
        var builder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<GoInGameRequest>()
            .WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(builder));
        networkIdFromEntity = state.GetComponentLookup<NetworkId>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get the prefab to instantiate
        var prefab = SystemAPI.GetSingleton<CraftSpawner>().Craft;
        
        // Ge the name of the prefab being instantiated
        state.EntityManager.GetName(prefab, out var prefabName);
        var worldName = new FixedString32Bytes(state.WorldUnmanaged.Name);

        var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
        networkIdFromEntity.Update(ref state);

        foreach (var (reqSrc, goInGameData, reqEntity) in SystemAPI
                     .Query<RefRO<ReceiveRpcCommandRequest>, RefRO<GoInGameRequest>>()
                     .WithEntityAccess())
        {
            // Now you have goInGameData.ValueRO.PlayerName
            var playerName = goInGameData.ValueRO.PlayerName;
            commandBuffer.AddComponent<NetworkStreamInGame>(reqSrc.ValueRO.SourceConnection);
            // Get the NetworkId for the requesting client
            var networkId = networkIdFromEntity[reqSrc.ValueRO.SourceConnection];

            // Log information about the connection request that includes the client's assigned NetworkId and the name of the prefab spawned.
            UnityEngine.Debug.Log($"'{worldName}' setting connection '{networkId.Value}' to in game, spawning a Ghost '{prefabName}' for them!");

            // Instantiate the prefab
            var player = commandBuffer.Instantiate(prefab);
            // Associate the instantiated prefab with the connected client's assigned NetworkId
            commandBuffer.SetComponent(player, new GhostOwner { NetworkId = networkId.Value});
            commandBuffer.SetComponent(reqSrc.ValueRO.SourceConnection, new CommandTarget { targetEntity = player });

            //   basePosition: where the first player (ID=0) should spawn
            //   sideLength: the overall size of the 10×10×10 cube
            float3 basePosition = new float3(0, 300f, 0);
            float sideLength = 450f; // example

            int index = networkId.Value; // zero-based index (0..99)
            int xIndex = index % 10;
            int yIndex = (index / 10) % 10;
            int zIndex = (index / 100) % 10;
            // For a 10×10×10 grid, we divide by 9 so that index=0 is at basePosition,
            // and index=9 is at basePosition + sideLength in that dimension:
            float cellSize = sideLength / 9f;

            float3 offset = new float3(xIndex, yIndex, zIndex) * cellSize;
            commandBuffer.SetComponent(player, new LocalTransform
            {
                Position = basePosition + offset,
                Rotation = quaternion.AxisAngle(math.up(), math.radians(0)),
                Scale = 1f
            });
            commandBuffer.SetComponent(player, new Username { Value = playerName });

            // Add the player to the linked entity group so it is destroyed automatically on disconnect
            commandBuffer.AppendToBuffer(reqSrc.ValueRO.SourceConnection, new LinkedEntityGroup{Value = player});
            commandBuffer.DestroyEntity(reqEntity);
        }
        commandBuffer.Playback(state.EntityManager);
    }
}