using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MessageParser
{
    public static (ChessGameManager.EChessTeam, ChessGameManager.Move) ParseMove(string message)
    {
        if (message.Length < 5)
        {
            Debug.LogError("Invalid message received: " + message);
            return (ChessGameManager.EChessTeam.None, new ChessGameManager.Move());
        }
        ChessGameManager.EChessTeam team;
        switch (message[0])
        {
            case 'W':
                team = ChessGameManager.EChessTeam.White;
                break;
            case 'B':
                team = ChessGameManager.EChessTeam.Black;
                break;
            default:
                Debug.LogError("Invalid team received: " + message[0]);
                return (ChessGameManager.EChessTeam.None, new ChessGameManager.Move());
        }
        //check if the team is correct
        if (team != ChessGameManager.Instance.teamTurn)
        {
            Debug.LogError("It's not " + team.ToString() + "'s turn!");
            return (team, new ChessGameManager.Move());
        }
        //get first move
        ChessGameManager.Move move = new ChessGameManager.Move();
        try
        {
            move.from = int.Parse(message.Substring(1, 2));
            move.to = int.Parse(message.Substring(3, 2));
        }
        catch (Exception e)
        {
            Debug.LogError("Invalid move received: " + message + " Exception: " + e.ToString());
            return (team, new ChessGameManager.Move());
        }
        return (team, move);
    }

    public static List<ChessGameManager.EChessTeam> ParseTeams(string message)
    {
        List<ChessGameManager.EChessTeam> teams = new List<ChessGameManager.EChessTeam>();
        foreach (char c in message)
        {
            try
            {
                teams.Add((ChessGameManager.EChessTeam)c);
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid team request received: " + message + " Exception: " + e.ToString());
            }
        }
        return teams;
    }

    public static (int, ChessGameManager.EChessTeam) ParseTeamRequest(string message)
    {
        return (ParseInt(message.Substring(1, message.Length - 1)), (ChessGameManager.EChessTeam)message[0]);
    }

    public static int ParseInt(string message)
    {
        int intValue = 0;
        float multiplier = Mathf.Pow(10f, (message.Length - 1));
        foreach (char c in message)
        {
            intValue += (int)(multiplier * Char.GetNumericValue(c));
            multiplier /= 10f;
        }
        return intValue;
    }
}
