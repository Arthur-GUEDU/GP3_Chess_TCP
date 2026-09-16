using UnityEngine;
using System.Collections.Generic;

/*
 * This class computes AI move decision
 * ComputeMove method is called from ChessGameMananger during AI update turn
 */

public class ChessAI : MonoBehaviour
{
    #region Singleton
    static ChessAI instance = null;
    public static ChessAI Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<ChessAI>();
            return instance;
        }
    }
    #endregion

    #region AI

    public ChessGameManager.Move ComputeMove()
    {
        // random AI move

        ChessGameManager.Move move;
        move.from = 0;
        move.to = 1;

        List<ChessGameManager.Move> moves = new List<ChessGameManager.Move>();
        ChessGameManager.Instance.GetBoardState().GetValidMoves(ChessGameManager.EChessTeam.Black, moves);

        if (moves.Count > 0)
            move = moves[Random.Range(0, moves.Count - 1)];

        return move;
    }

    #endregion
}
