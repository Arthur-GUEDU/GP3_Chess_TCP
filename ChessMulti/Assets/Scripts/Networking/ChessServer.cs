using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
//we are NOT using unity networking
using System.Net.Sockets;   
using System.Text;
using UnityEngine;
using static ChessGameManager;

public class ChessServer : NetworkObject
{
    Socket clientSocket;
    Socket serverSock;
    IPEndPoint localEndPoint;
    public bool bIsLocal = false;
    public bool bGameStarted = false;

    public bool bIsConnected { get { return clientSocket != null && clientSocket.Connected; } }

    ChessGameManager.EChessTeam localTeam = ChessGameManager.EChessTeam.White;
    List<ChessPlayer> players = new List<ChessPlayer>();
    List<int> freeIds = new List<int>();

    public class ChessPlayer
    {
        public int id;
        public bool bIsConnected { get { return playerSocket != null && playerSocket.Connected; } }
        public Socket playerSocket;
        public ChessGameManager.EChessTeam team;

        public float lastHeardTimer = 0f;

        public ChessPlayer(int id)
        {
            this.id = id;
            playerSocket = null;
            team = ChessGameManager.EChessTeam.None;
        }

        public ChessPlayer(string code)
        {
            playerSocket = null;
            id = int.Parse(code.Substring(0, code.Length - 1)); //code.Length -1 to account for any id size
            char teamChar = code[code.Length - 1];
            switch(teamChar)
            {
                case 'W':
                    team = ChessGameManager.EChessTeam.White;
                    break;
                case 'B':
                    team = ChessGameManager.EChessTeam.Black;
                    break;
                default:
                    team = ChessGameManager.EChessTeam.None;
                    break;
            }
        }

        public string GetCode()
        {
            char teamChar = (team == EChessTeam.White) ? 'W' : (team == EChessTeam.Black) ? 'B' : 'N';
            return id.ToString()+ teamChar;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void Initialize(IPAddress _serverAdress)
    {
        ChessGameManager.Instance.playerTurn.AddListener(PlayerTurn);

        players.Add(new ChessPlayer(1));

        IPAddress ipAddress;
        //setup server
        ipAddress = _serverAdress;

        // create socket server
        serverSock = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        localEndPoint = new IPEndPoint(ipAddress, port);
        serverSock.Bind(localEndPoint);
        serverSock.Listen(10);
        serverSock.Blocking = false;
    }

    public void InitializeLocal()
    {
        bIsLocal = true;
        Initialize(IPAddress.Loopback);
    }

    // Update is called once per frame
    void Update()
    {
        TryAcceptConnection(); //tries and accept a new connection if there is one
        ReceiveAllClients(); //receives messages from all connected clients
        CheckPlayerTimeout(); //check if any player is timed out
    }

    /// <summary>
    /// Tries and accept a new connection if there is one
    /// If a connection is made, sends all necessary info to the client and starts listening for the next one
    /// </summary>
    void TryAcceptConnection()
    {
        try
        {
            ChessPlayer player = players[^1]; //last one in list is always listening
            players[^1].playerSocket = serverSock.Accept();
            players[^1].lastHeardTimer = 0f;
            Console.WriteLine("Client connected!");
            UIManager.chatbox.AddErrorMessage("Player " + player.id.ToString() + " has connected.", Color.yellow);

            //recreate a new player to listen for next connection
            int idToAssign = players.Count + 1;
            if(freeIds.Count > 0) //if there are any free ids, use the first one
            {
                idToAssign = freeIds[0];
                freeIds.RemoveAt(0);
            }
            players.Add(new ChessPlayer(idToAssign)); //add a new player to listen for next connection

            //setup game if it hasnt been started yet
            if (!bGameStarted)
            {
                GUIManager.Instance.LaunchPlay();
                bGameStarted = true;
                ChessGameManager.Instance.PrepareMultiplayerGame();
                ChessGameManager.Instance.SetLocalTeam(ChessGameManager.EChessTeam.White); //server is always white at launch
            }

            //assign player id
            SendChessMessage(player.playerSocket, "IdAssignation", player.id.ToString());
            //send the player socket the available teams
            string availableTeams = GetAvailableTeams();
            SendChessMessage(player.playerSocket, "TeamInfo", availableTeams);

            //send board state
            SendChessMessage(player.playerSocket, "BoardState", ChessGameManager.Instance.GetBoardState().GetCode());

            //send current turn
            SendChessMessage(player.playerSocket, "CurrentTurn", ChessGameManager.Instance.teamTurn.ToString());
        }
        catch (SocketException e)
        {
            if(e.SocketErrorCode != SocketError.WouldBlock) //ignore this exception, it just means there was no connection to accept
                Debug.Log("Socket exception: " + e.ToString());
        }
    }

    /// <summary>
    /// Checks if any player has timed out (no message received for a certain duration)
    /// if a player has timed out, disconnects them
    /// </summary>
    private void CheckPlayerTimeout() //check if any player is timed out
    {
        foreach(ChessPlayer player in players.ToArray())
        {
            player.lastHeardTimer += Time.deltaTime;
            if (!player.bIsConnected)
                continue;
            if(player.lastHeardTimer > timeoutDuration)
            {
                ShutdownClientConnection(player);
                //sends a message to the chatbox to inform the user
                UIManager.chatbox.AddErrorMessage("Player "+player.id.ToString()+" has timed out.", Color.red);
            }
        }
    }

    /// <summary>
    /// function called by event when it is the player's turn, broadcasts the move to all clients
    /// </summary>
    void PlayerTurn(ChessGameManager.Move move, ChessGameManager.EChessTeam _team) 
    {
        //send move to client
        ChessGameManager.Instance.PlayTurn(move); //play the turn locally as well


        //create a string that is the team + move
        string moveString = _team.ToString()[0] + move.GetMoveCode();
        TellAllClients("Move", moveString);
    }

    /// <summary>
    /// When the server receives a move from a client, it processes it here
    /// </summary>
    protected override void ProcessMove((ChessGameManager.EChessTeam, ChessGameManager.Move) move)
    {
        ChessGameManager.Instance.PlayTurn(move.Item2);
        TellAllClients("Move", move.Item1.ToString()[0]+move.Item2.GetMoveCode());
    }

    /// <summary>
    /// When the server receives a team request from a client, it processes it here. 
    /// If the requested team is available, it assigns it to the player and tells the client
    /// </summary>
    protected override void ProcessTeamRequest((int, ChessGameManager.EChessTeam) _teamRequestInfo)
    {
        bool isAvailable = true;

        if (_teamRequestInfo.Item2 != ChessGameManager.EChessTeam.None)
        {
            if (localTeam == _teamRequestInfo.Item2)
                isAvailable = false;
            else
            {
                foreach (ChessPlayer player in players)
                {
                    if (!player.bIsConnected)
                        continue;
                    if (player.team == _teamRequestInfo.Item2)
                        isAvailable = false;
                }
            }
        }

        if (isAvailable)
        {
            ChessPlayer selectedPlayer = null;
            foreach (ChessPlayer player in players)
            {
                if (!player.bIsConnected)
                    continue;
                if (player.id == _teamRequestInfo.Item1)
                    selectedPlayer = player;
            }
            try
            {
                if (selectedPlayer != null)
                {
                    selectedPlayer.team = _teamRequestInfo.Item2;
                    SendChessMessage(selectedPlayer.playerSocket, "TeamAssignation", _teamRequestInfo.Item2.ToString().Substring(0, 1));
                    UpdateAvailableTeams();
                }
            }
            catch (SocketException e)
            {
                Debug.Log("Socket exception: " + e.ToString());
            }
            catch (Exception e)
            {
                Debug.Log("Id exception: " + e.ToString());
            }
        }
    }

    /// <summary>
    /// When the server receives a chat message from a client, it processes it here and relays it to all clients
    /// </summary>
    protected override void ProcessChatMessage(string chatMessage)
    {
        base.ProcessChatMessage(chatMessage);
        TellAllClients("ChatMessage", chatMessage); //relay chat message to all clients 
    }

    /// <summary>
    /// When a client disconnects, this function is called to clean up the connection and notify other clients
    /// </summary>
    protected override void ProcessClientDisconnect(int clientId)
    {
        ShutdownClientConnection(players.Find(p => p.id == clientId), false);
        UIManager.chatbox.AddErrorMessage("Player " + clientId.ToString() + " has disconnected.", Color.yellow);
        TellAllClients("ClientDisconnect", clientId.ToString());
    }

    /// <summary>
    /// Function called by event for when the local player has written something in the chatbox. 
    /// </summary>
    protected override void OnMessageWritten(string message) 
    {
        base.OnMessageWritten(message);
        TellAllClients("ChatMessage", id.ToString()+":"+message); //tell all clients
    }

    /// <summary>
    /// Sends a message to all clients
    /// </summary>
    public void TellAllClients(string type, string message)
    {
        foreach(ChessPlayer player in players)
        {
            if (!player.bIsConnected)
                continue;
            SendChessMessage(player.playerSocket, type, message);
        }
    }

    /// <summary>
    /// Receives messages from all connected clients and processes them
    /// </summary>
    public void ReceiveAllClients()
    {
        //create a copy of the list to avoid modification during iteration
        foreach(ChessPlayer player in players.ToArray())
        {
            if (!player.bIsConnected)
                continue;
            string message = TryReceiveChessMessage(player.playerSocket);
            if (message != string.Empty)
            {
                player.lastHeardTimer = 0f; //reset timer
                ParseChessMessages(message, player.id);
            }
        }
    }

    /// <summary>
    /// Returns all teams not currently taken by a connected player (there is an infinite number of spectators), as a string
    /// </summary>
    public string GetAvailableTeams()
    {
        bool whiteAvailable = true;
        bool blackAvailable = true;
        //local team is the server's team
        if (localTeam == ChessGameManager.EChessTeam.White)
            whiteAvailable = false;
        else if (localTeam == ChessGameManager.EChessTeam.Black)
            blackAvailable = false;

        //check all connected players
        foreach (ChessPlayer player in players)
        {
            if(!player.bIsConnected)
                continue;
            if (player.team == ChessGameManager.EChessTeam.White)
                whiteAvailable = false;
            else if (player.team == ChessGameManager.EChessTeam.Black)
                blackAvailable = false;
        }

        //prepare and return the string with the available teams
        string availableTeams = "N";
        if (whiteAvailable)
            availableTeams += "W";
        if (blackAvailable)
            availableTeams += "B";
        return availableTeams;
    }



    private void OnDestroy()
    {
        Shutdown();
    }

    /// <summary>
    /// Shuts down the server and all client connections
    /// </summary>
    protected override void Shutdown()
    {
        
        Debug.Log("Shutting down server");
        try
        {
            //shut down all sockets
            foreach (ChessPlayer player in players)
            {
                if (player.bIsConnected)
                {
                    SendChessMessage(player.playerSocket, "Shutdown", "");
                    player.playerSocket.Shutdown(SocketShutdown.Both);
                    player.playerSocket.Close();
                }
            }
        }
        catch (SocketException e)
        {
            Debug.Log("Socket exception: " + e.ToString());
        }
        finally
        {
            if (clientSocket != null)
                clientSocket.Close();
        }
        if (serverSock != null)
            serverSock.Close();
        base.Shutdown();
    }

    /// <summary>
    /// shuts down the connection to a specific client, notifies them if specified, and removes them from the player list
    /// </summary>
    private void ShutdownClientConnection(ChessPlayer player, bool notify = true)
    {
        try
        {
            if (player.bIsConnected)
            {
                if(notify)
                    SendChessMessage(player.playerSocket, "Shutdown", "");
                player.playerSocket.Shutdown(SocketShutdown.Both);
                player.playerSocket.Close();
            }
        }
        catch (SocketException e)
        {
            Debug.Log("Socket exception: " + e.ToString());
        }
        finally
        {
            if (player.playerSocket != null)
                player.playerSocket.Close();
            freeIds.Add(player.id);
            players.Remove(player);
            UpdateAvailableTeams();
        }
    }

    /// <summary>
    /// function called by coroutine, sends a ping message to all clients to notify them the server is still alive
    /// </summary>
    protected override void OnPing()
    {
        TellAllClients("ping", ""); //ping all clients
    }

    protected override void TryJoinTeam(EChessTeam newTeam)
    {
        GameManager.SetLocalTeam(newTeam);
        localTeam = newTeam;
        UpdateAvailableTeams();
    }

    /// <summary>
    /// Updates the currently available teams (not used by a player) and tells the clients
    /// </summary>
    void UpdateAvailableTeams()
    {
        string availableTeams = GetAvailableTeams();
        TellAllClients("TeamInfo", availableTeams);

        List<EChessTeam> teamList = new List<EChessTeam>();
        if (availableTeams.Contains('W'))
            teamList.Add(EChessTeam.White);
        if (availableTeams.Contains('B'))
            teamList.Add(EChessTeam.Black);
        if (availableTeams.Contains('N'))
            teamList.Add(EChessTeam.None);

        GUIManager.Instance.UpdateTeamButtons(teamList);
    }
}
