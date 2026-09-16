using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

public class NetworkObject : MonoBehaviour
{
    public int id;

    public int port = 11000;

    protected GUIManager UIManager;
    protected ChessGameManager GameManager;

    protected float timeoutDuration = 20f; //time after which the connection is considered lost
    float pingInterval = 2f; //time between each ping
    float pingTimer = 0f;


    public void Awake()
    {
        //collects these references only once for easier access later
        UIManager = GUIManager.Instance;
        GameManager = ChessGameManager.Instance;

        //add listeners to the chatbox and team change buttons if the UIManager exists
        if (UIManager)
        {
            if (UIManager.chatbox)
                UIManager.chatbox.inputField.onEndEdit.AddListener(OnMessageWritten);

            if (UIManager.onChangeTeam != null)
                UIManager.onChangeTeam.AddListener(TryJoinTeam);
        }
        StartCoroutine(PingCoroutine(pingInterval));
    }

    /// <summary>
    /// Sends a message through the given socket, in the format "type[message]\n"
    /// </summary>
    /// <param name="socket">The socket to send the message to</param>
    /// <param name="type">The "type" of message</param>
    /// <param name="message">The message</param>
    public void SendChessMessage(Socket socket, string type, string message)
    {
        byte[] msg = Encoding.ASCII.GetBytes(type + "[" + message + "]" + "\n");
        try
        {
            socket.Send(msg);
        }
        catch (Exception e)
        {
            Debug.LogError("error sending message : " + e.ToString());
        }
    }

    /// <summary>
    /// Attempts to receive a message from the given socket, returning an empty string if no message is received
    /// </summary>
    protected string TryReceiveChessMessage(Socket socket)
    {
        try
        {
            byte[] messageReceived = new byte[1024];
            int nbBytes = socket.Receive(messageReceived);
            return Encoding.ASCII.GetString(messageReceived, 0, nbBytes);
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode != SocketError.WouldBlock)
                Debug.Log("error receiving message : " + e.ToString());
        }
        return String.Empty;
    }

    /// <summary>
    /// Parses a batch of chess messages received from a client. Chess messages are separated by new lines. (not ideal but works in this instance)
    /// </summary>
    protected void ParseChessMessages(string messages, int clientId = 0)
    {

        string[] splitMessages = messages.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string message in splitMessages)
        {
            ParseChessMessage(message, clientId);
        }
    }

    /// <summary>
    /// Parses a single chess message received from a client. Chess messages are in the format "type[message]"
    /// Divides the message into its type and content, then calls the appropriate processing function if it exists.
    /// </summary>
    protected virtual void ParseChessMessage(string message, int clientId = 0)
    {
        Debug.Log("Received message: " + message);

        //get the message type
        int typeEndIndex = message.IndexOf('[');
        if (typeEndIndex == -1)
        {
            Debug.LogError("Invalid message received: " + message);
            return;
        }
        string type = message.Substring(0, typeEndIndex);
        string info = message.Substring(typeEndIndex + 1, message.Length - typeEndIndex - 2); //remove the brackets
        switch (type)
        {
            case "ping":
                break;
            case "Shutdown":
                ProcessShutdownMessage();
                break;
            case "ClientDisconnect":
                ProcessClientDisconnect(Int32.Parse(info));
                break;
            case "Move":
                ProcessMove(MessageParser.ParseMove(info));
                break;
            case "TeamInfo":
                ProcessTeamInfo(MessageParser.ParseTeams(info));
                break;
            case "ChatMessage":
                ProcessChatMessage(info);
                break;
            case "TeamRequest":
                ProcessTeamRequest(MessageParser.ParseTeamRequest(info));
                break;
            case "TeamAssignation":
                ProcessTeamAssignation((ChessGameManager.EChessTeam)info[0]);
                break;
            case "IdAssignation":
                id = MessageParser.ParseInt(info);
                break;
            case "BoardState":
                ProcessBoardState(info);
                break;
            case "CurrentTurn":
                ProcessTeamTurn(info);
                break;
            default:
                Debug.Log("Unknown message type received: " + type);
                break;
        }
    }

    protected virtual void ProcessMove((ChessGameManager.EChessTeam, ChessGameManager.Move) move) {}

    //when the server tells us whose turn it is
    protected virtual void ProcessTeamTurn(string teamTurn) {}

    protected virtual void ProcessTeamInfo(List<ChessGameManager.EChessTeam> availableTeams) {}

    protected virtual void ProcessTeamRequest((int, ChessGameManager.EChessTeam) playerInfo) {}

    protected virtual void ProcessTeamAssignation(ChessGameManager.EChessTeam _assignedTeam) {}
    
    protected virtual void ProcessClientDisconnect(int clientId) {}

    protected virtual void ProcessChatMessage(string chatMessage)
    {
        int id = int.Parse(chatMessage.Substring(0, chatMessage.IndexOf(':')));
        string text = chatMessage.Substring(chatMessage.IndexOf(':') + 1);
        if(id != this.id)
            UIManager.chatbox.AddMessage(new Message(new ChessServer.ChessPlayer(id), text));
    }

    /// <summary>
    /// For when the local player has written something in the chatbox. Adds this message to the chatbox immediatly.
    /// </summary>
    protected virtual void OnMessageWritten(string message)
    {
        UIManager.chatbox.ClearInputField();
        if (message == string.Empty || message == null || message.Length > 200)
            return;
        UIManager.chatbox.AddMessage(new Message(new ChessServer.ChessPlayer(id), message));
    }

    protected virtual void ProcessBoardState(string boardState) {}

    /// <summary>
    /// pings the server every pingInterval seconds to keep the connection alive
    /// </summary>
    IEnumerator PingCoroutine(float delay)
    {
        pingTimer = 0f;
        while (true)
        {
            yield return new WaitForSeconds(delay);
            pingTimer += delay;
            if(pingTimer >= pingInterval)
            {
                pingTimer -= pingInterval;
                OnPing();
            }
        }
    }

    protected virtual void OnPing() {}

    protected virtual void ProcessShutdownMessage() {}

    protected virtual void Shutdown()
    {
        if(ChessGameManager.Instance)
            GameManager.ResetGame(); //since we're shutting down, reset the game and go back to the main menu
    }

    protected virtual void TryJoinTeam(ChessGameManager.EChessTeam newTeam) {}
}
