using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)

    {
        // Bypass network setup if running in the Unity Editor
        if (Application.isEditor)
        {
            Debug.Log("Running in Unity Editor. Skipping network setup logic.");
            AutoConnectPort = 7979;

            return base.Initialize(defaultWorldName);
        }
        else
        {
            // Define the IP and port for the server
            var serverEndPoint = NetworkEndpoint.Parse("167.172.235.108", 7979);

#if CLIENT_ONLY_BUILD
            // Client-only build logic
            // Create the client world only
            var clientWorld = CreateClientWorld(defaultWorldName);

            // Create a connection request entity for the client to connect to the server
            var connectRequest = clientWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestConnect));
            clientWorld.EntityManager.SetComponentData(connectRequest, new NetworkStreamRequestConnect { Endpoint = serverEndPoint });

#else
            // Server-only build logic for production
            // Create the server world only
            var serverWorld = CreateServerWorld(defaultWorldName);

            // Set server to listen on all available network interfaces (0.0.0.0)
            AutoConnectPort = 7979;
            var listenEndPoint = NetworkEndpoint.AnyIpv4.WithPort(7979);  // Listen on all addresses for production

            // Create a listen request entity for the server to start listening
            var listenRequest = serverWorld.EntityManager.CreateEntity(typeof(NetworkStreamRequestListen));
            serverWorld.EntityManager.SetComponentData(listenRequest, new NetworkStreamRequestListen { Endpoint = listenEndPoint });
            Debug.Log($"Server listening on port {listenEndPoint.Port}");

#endif

            return true;
        }
    }
}
