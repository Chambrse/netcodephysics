using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

// Create a custom bootstrap, which enables auto-connect.
// The bootstrap can also be used to configure other settings as well as to
// manually decide which worlds (client and server) to create based on user input
[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{

    //public static NetworkEndpoint DefaultConnectAddress;
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979; // Enabled auto connect
        //networkendpoint
        //new NetworkEndPoint { Address = NetworkEndPoint.LoopbackIpv4, Port = 7979 };
        DefaultConnectAddress = NetworkEndpoint.Parse("167.172.235.108", 7979);

        return base.Initialize(defaultWorldName); // Use the regular bootstrap
    }
}
