using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ChessClient : NetworkObject
{
    Socket clientSocket;

    public string serverIPAdress;
    public bool bIsLocal = false;
    private bool bIsTeamAssigned = false;

    private float lastHeardTimer = 0f; //timer to track last received message for timeout

    public bool bIsConnected { get { return clientSocket != null && clientSocket.Connected; } }

    /// <summary>
    /// Sets up the client socket and connects to the server.
    /// if the game is local, connects to loopback address
    /// </summary>
    public bool Initialize(IPAddress _clientIPAddress, IPAddress _serverIPAddress, bool isFirstInitialization)
    {
        bool isConnected = false;
        if (isFirstInitialization)
            ChessGameManager.Instance.playerTurn.AddListener(PlayerTurn);
        if (bIsLocal)
        {
            IPAddress ipAddress = IPAddress.Loopback;
            clientSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            isConnected = Connect(ipAddress);
        }
        else
        {
            clientSocket = new Socket(_clientIPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            isConnected = Connect(_serverIPAddress);
        }
        if (isFirstInitialization)
        {
            clientSocket.Blocking = false;
            StartCoroutine(TimeoutAlertCoroutine());
        }
        return isConnected;
    }

    /// <summary>
    /// Connects the client socket to the given IP address and port. Returns true if successful, false otherwise.
    /// </summary>
    public bool Connect(IPAddress adress)
    {
        IPEndPoint serverEP = new IPEndPoint(adress, port);
        try
        {
            clientSocket.Connect(serverEP);
            Debug.Log("Socket connected to " + clientSocket.RemoteEndPoint.ToString());
            GUIManager.Instance.LaunchPlay();
            ChessGameManager.Instance.PrepareMultiplayerGame();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Socket exception: " + e.ToString());
            Shutdown();
            return false;
        }
    }

    protected override void Shutdown()
    {
        if (clientSocket != null && bIsConnected)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            clientSocket = null;
        }
        base.Shutdown();
    }

    /// <summary>
    /// Function called by event when the player makes a move. Tells the server the move that was made.
    /// </summary>
    void PlayerTurn(ChessGameManager.Move move, ChessGameManager.EChessTeam team)
    {
        //create a string that is the team + move
        string moveString = team.ToString()[0] + move.GetMoveCode();
        SendChessMessage(clientSocket, "Move", moveString);

    }

    // Update is called once per frame
    void Update()
    {
        if (bIsConnected)
        {
            lastHeardTimer += Time.deltaTime;
            string message = TryReceiveChessMessage(clientSocket);
            if (message != string.Empty)
            {
                lastHeardTimer = 0f; //reset timer
                ParseChessMessages(message);
                Debug.Log(message);
            }
            if (lastHeardTimer > timeoutDuration)
            {
                Debug.Log("Connection to server lost, returning to main menu");
                Shutdown();
            }
        }
    }

    /// <summary>
    /// Attempts to play a move received from the server
    /// </summary>
    protected override void ProcessMove((ChessGameManager.EChessTeam, ChessGameManager.Move) move)
    {
        ChessGameManager.Instance.PlayTurn(move.Item2);
    }

    /// <summary>
    /// When receving the available teams from the server, if we dont have a team yet, request to be assigned one.
    /// Otherwise, update the team buttons to reflect available teams.
    /// </summary>
    protected override void ProcessTeamInfo(List<ChessGameManager.EChessTeam> availableTeams)
    {
        if (!bIsTeamAssigned)
        {
            string teamString;
            if (availableTeams.Contains(ChessGameManager.EChessTeam.White))
            {
                teamString = "W";
            }
            else if (availableTeams.Contains(ChessGameManager.EChessTeam.Black))
            {
                teamString = "B";
            }
            else
            {
                teamString = "N";
            }
            SendChessMessage(clientSocket, "TeamRequest", teamString + id.ToString());
        }
        else
        {
            GUIManager.Instance.UpdateTeamButtons(availableTeams);
        }
    }

    /// <summary>
    /// For when the server assigns us a team. Sets our local team and marks that we have a team assigned.
    /// </summary>
    protected override void ProcessTeamAssignation(ChessGameManager.EChessTeam _assignedTeam)
    {
        GameManager.SetLocalTeam(_assignedTeam);
        bIsTeamAssigned = true;
    }

    /// <summary>
    /// When the player writes a message in the chatbox, send it to the server
    /// </summary>
    protected override void OnMessageWritten(string message)
    {
        base.OnMessageWritten(message);
        SendChessMessage(clientSocket, "ChatMessage", id.ToString() + ":" + message);
    }

    /// <summary>
    /// For when we receive the entire board state from the server (for example when joining a game in progress).
    /// Updates the board to be correctly synchronized.
    /// </summary>
    protected override void ProcessBoardState(string boardState)
    {
        Debug.Log("Received board state");
        ChessGameManager.Instance.SetBoardState(boardState);
    }

    /// <summary>
    /// For when we hotjoin, the server tells us whose turn it is.
    /// </summary>
    protected override void ProcessTeamTurn(string teamTurn)
    {
        ChessGameManager.EChessTeam team;
        switch(teamTurn[0])
        {
            case 'W':
                team = ChessGameManager.EChessTeam.White;
                break;
            case 'B':
                team = ChessGameManager.EChessTeam.Black;
                break;
            default:
                Debug.LogError("Invalid team turn received: " + teamTurn);
                return;
        }
        GameManager.SetTeamTurn(team);
    }

    protected override void OnPing()
    {
        SendChessMessage(clientSocket, "ping", "");
    }

    /// <summary>
    /// When we receive a shutdown message from the server, display a message and return to main menu.
    /// </summary>
    protected override void ProcessShutdownMessage()
    {
        UIManager.chatbox.AddErrorMessage("Server has closed the connection", Color.red);
        Shutdown();
    }

    /// <summary>
    /// Sends a message to the chatbox when another player disconnects.
    /// </summary>
    protected override void ProcessClientDisconnect(int clientId)
    {
        UIManager.chatbox.AddErrorMessage("Player " + clientId.ToString() + " has disconnected.", Color.yellow);
    }

    private void OnDestroy()
    {
        if(bIsConnected)
            SendChessMessage(clientSocket, "ClientDisconnect", id.ToString()); //inform server we are disconnecting
        Shutdown();
    }

    /// <summary>
    /// Alerts the user if the server is unresponsive.
    /// Disconnects after timeoutDuration seconds of no response.
    /// </summary>
    IEnumerator TimeoutAlertCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (lastHeardTimer > timeoutDuration / 2) //send a chat alert if we havent received anything for half the timeout duration
            {
                UIManager.chatbox.AddErrorMessage("No response from server in " + ((int)lastHeardTimer).ToString() + " seconds. Server will timeout in " + ((int)(timeoutDuration - lastHeardTimer)).ToString() + " seconds", Color.red);
            }

        }
    }

    protected override void TryJoinTeam(ChessGameManager.EChessTeam newTeam)
    {
        if (newTeam != GameManager.localPlayerTeam)
        {
            string teamString = "";
            teamString += newTeam.ToString()[0];
            SendChessMessage(clientSocket, "TeamRequest", teamString + id.ToString());
        }
    }
}
