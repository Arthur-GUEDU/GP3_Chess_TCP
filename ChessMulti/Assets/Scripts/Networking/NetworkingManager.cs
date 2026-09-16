using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetworkingManager : MonoBehaviour
{
    #region Singleton
    static NetworkingManager instance = null;
    public static NetworkingManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<NetworkingManager>();
            return instance;
        }
    }
    #endregion

    public bool TryJoinServer(string _ipAddress)
    {
        try
        {
            IPAddress serverAddress = IPAddress.Parse(_ipAddress);
            ChessClient chessClient = FindFirstObjectByType<ChessClient>();
            IPAddress clientAddress = GetIPV4FromAddressList();

            if (serverAddress != null && clientAddress != null)
            {
                if (chessClient == null)
                {
                    ChessClient client = new GameObject("Client").AddComponent<ChessClient>();
                    return client.Initialize(clientAddress, serverAddress, true);
                }
                else
                {
                    return chessClient.Initialize(clientAddress, serverAddress, false);
                }
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public string CreateServer()
    {
        IPAddress serverAdress = GetIPV4FromAddressList();
        if (serverAdress != null)
        {
            ChessServer server = new GameObject("Server").AddComponent<ChessServer>();
            server.Initialize(serverAdress);

            return serverAdress.ToString();
        }
        return "";
    }

    public string CreateLocalServer()
    {
        IPAddress serverAdress = IPAddress.Loopback;
        if (serverAdress != null)
        {
            ChessServer server = new GameObject("Server").AddComponent<ChessServer>();
            server.InitializeLocal();

            return serverAdress.ToString();
        }
        return "";
    }

    

    public IPAddress GetIPV4FromAddressList()
    {
        string myHostName = Dns.GetHostName();

        foreach (IPAddress ip in Dns.Resolve(myHostName).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }
        return IPAddress.Any;
    }

    public void DestroyNetworkObjects()
    {
        NetworkObject[] networkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        for (int i = 0; i < networkObjects.Length; i++)
        {
            Object.Destroy(networkObjects[i]);
        }
    }
}
