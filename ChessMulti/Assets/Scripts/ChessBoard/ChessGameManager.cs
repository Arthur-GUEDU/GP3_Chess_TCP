using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static ChessGameManager;

/*
 * This singleton manages the whole chess game
 *  - board data (see BoardState class)
 *  - piece models instantiation
 *  - player interactions (piece grab, drag and release)
 *  - AI update calls (see UpdateAITurn and ChessAI class)
 */

public partial class ChessGameManager : MonoBehaviour
{

    #region Singleton
    static ChessGameManager instance = null;
    public static ChessGameManager Instance {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<ChessGameManager>();
            return instance;
        }
    }
    #endregion

    [SerializeField]
    private bool isAIEnabled = true;

    public Transform cameraRoot;
    public bool bIsMultiplayer = false;
    public EChessTeam localPlayerTeam = EChessTeam.White;

    private ChessAI chessAI = null;
    private Transform boardTransform = null;
    private static int BOARD_SIZE = 8;
    private int pieceLayerMask;
    private int boardLayerMask;

    #region Enums
    public enum EPieceType : uint
    {
        Pawn = 0,
        King,
        Queen,
        Rook,
        Knight,
        Bishop,
        NbPieces,
        None
    }

    public enum EChessTeam
    {
        White = 'W',
        Black = 'B',
        None = 'N'
    }

    public enum ETeamFlag : uint
    {
        None = 1 << 0,
        Friend = 1 << 1,
        Enemy = 1 << 2
    }

    public int GetChessTeamValue(EChessTeam team)
    {
        int value = -1;
        switch (team)
        {
            case EChessTeam.White:
                value = 0;
                break;
            case EChessTeam.Black:
                value = 1;
                break;
            case EChessTeam.None:
                value = 2;
                break;
        }
        return value;
    }
    #endregion

    #region Structs & Classes
    public struct BoardSquare
    {
        public EPieceType piece;
        public EChessTeam team;

        public BoardSquare(EPieceType p, EChessTeam t)
        {
            piece = p;
            team = t;
        }

        public BoardSquare(string code)
        {
            //code is a 2 char string containing the piece and team code. for example, if the piece is a white pawn, it is "PW"
            if (code.Length != 2)
            {
                piece = EPieceType.None;
                team = EChessTeam.None;
                return;
            }
            char pieceChar = code[0];
            char teamChar = code[1];
            switch (pieceChar)
            {
                case 'P':
                    piece = EPieceType.Pawn;
                    break;
                case 'K':
                    piece = EPieceType.King;
                    break;
                case 'Q':
                    piece = EPieceType.Queen;
                    break;
                case 'R':
                    piece = EPieceType.Rook;
                    break;
                case 'N':
                    piece = EPieceType.Knight;
                    break;
                case 'B':
                    piece = EPieceType.Bishop;
                    break;
                default:
                    piece = EPieceType.None;
                    break;
            }
            switch (teamChar)
            {
                case 'W':
                    team = EChessTeam.White;
                    break;
                case 'B':
                    team = EChessTeam.Black;
                    break;
                default:
                    team = EChessTeam.None;
                    break;
            }
            //SetFromCode(code);
        }

        public void SetFromCode(string code)
        {
            //code is a 2 char string containing the piece and team code. for example, if the piece is a white pawn, it is "PW"
            if (code.Length != 2)
            {
                piece = EPieceType.None;
                team = EChessTeam.None;
                return;
            }
            char pieceChar = code[0];
            char teamChar = code[1];
            switch (pieceChar)
            {
                case 'P':
                    piece = EPieceType.Pawn;
                    break;
                case 'K':
                    piece = EPieceType.King;
                    break;
                case 'Q':
                    piece = EPieceType.Queen;
                    break;
                case 'R':
                    piece = EPieceType.Rook;
                    break;
                case 'N':
                    piece = EPieceType.Knight;
                    break;
                case 'B':
                    piece = EPieceType.Bishop;
                    break;
                default:
                    piece = EPieceType.None;
                    break;
            }
            switch (teamChar)
            {
                case 'W':
                    team = EChessTeam.White;
                    break;
                case 'B':
                    team = EChessTeam.Black;
                    break;
                default:
                    team = EChessTeam.None;
                    break;
            }
        }

        static public BoardSquare Empty()
        {
            BoardSquare res;
            res.piece = EPieceType.None;
            res.team = EChessTeam.None;
            return res;
        }

        public string GetCode()
        {
            //returns a string containing the piece and team code. if the piece is a white pawn, it returns "PW"
            char teamChar = (team == EChessTeam.White) ? 'W' : (team == EChessTeam.Black) ? 'B' : 'N';
            char pieceChar = 'N';
            switch (piece)
            {
                case EPieceType.Pawn:
                    pieceChar = 'P';
                    break;
                case EPieceType.King:
                    pieceChar = 'K';
                    break;
                case EPieceType.Queen:
                    pieceChar = 'Q';
                    break;
                case EPieceType.Rook:
                    pieceChar = 'R';
                    break;
                case EPieceType.Knight:
                    pieceChar = 'N';
                    break;
                case EPieceType.Bishop:
                    pieceChar = 'B';
                    break;
                case EPieceType.None:
                    pieceChar = 'E';
                    break;
            }
            return pieceChar.ToString() + teamChar.ToString();
        }
    }

    public struct Move
    {
        public int from;
        public int to;

        public override bool Equals(object o)
        {
            try
            {
                return (bool)(this == (Move)o);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return to+from;
        }
        public string GetMoveCode()
        {
            //returns a string containing the move coords. if the move is from 8 to 16, it returns "0816"
            return from.ToString("D2") + to.ToString("D2");
        }


        public static bool operator ==(Move move1, Move move2)
        {
            return move1.from == move2.from && move1.to == move2.to;
        }

        public static bool operator !=(Move move1, Move move2)
        {
            return move1.from != move2.from || move1.to != move2.to;
        }
    }

    #endregion

    #region Chess Game Methods

    BoardState boardState = null;
    public BoardState GetBoardState() { return boardState; }

    public EChessTeam teamTurn { get; private set; }

    List<uint> scores;

    public delegate void PlayerTurnEvent(bool isWhiteMove);
    public event PlayerTurnEvent OnPlayerTurn = null;

    public delegate void ScoreUpdateEvent(uint whiteScore, uint blackScore);
    public event ScoreUpdateEvent OnScoreUpdated = null;

    public UnityEvent<Move, EChessTeam> playerTurn = new();

    public void PrepareGame(bool resetScore = true)
    {
        chessAI = ChessAI.Instance;

        // Start game
        boardState.Reset();

        teamTurn = EChessTeam.White;
        if (scores == null)
        {
            scores = new List<uint>();
            scores.Add(0);
            scores.Add(0);
        }
        if (resetScore)
        {
            scores.Clear();
            scores.Add(0);
            scores.Add(0);
        }
    }

    public void PrepareAIGame()
    {
        SetAiEnabled(true);
        localPlayerTeam = EChessTeam.White;
        PrepareGame();
    }

    public void PrepareMultiplayerGame()
    {
        SetAiEnabled(false);
        bIsMultiplayer = true;
        PrepareGame();
    }

    public void PlayTurnMultiplayer(Move move)
    {
        if (boardState.IsValidMove(teamTurn, move))
        {
            playerTurn.Invoke(move, teamTurn);
        }
    }

    public void PlayTurn(Move move)
    {
        
        if (boardState.IsValidMove(teamTurn, move))
        {
            BoardState.EMoveResult result = boardState.PlayUnsafeMove(move);
            if (result == BoardState.EMoveResult.Promotion)
            {
                // instantiate promoted queen gameobject
                AddQueenAtPos(move.to);
            }

            EChessTeam otherTeam = (teamTurn == EChessTeam.White) ? EChessTeam.Black : EChessTeam.White;
            if (boardState.DoesTeamLose(otherTeam))
            {
                GUIManager.Instance.chatbox.AddErrorMessage(teamTurn.ToString() + " Has won!", Color.green);

                // increase score and reset board
                scores[GetChessTeamValue(teamTurn)]++;
                if (OnScoreUpdated != null)
                    OnScoreUpdated(scores[0], scores[1]);

                PrepareGame(false);
                // remove extra piece instances if pawn promotions occured
                teamPiecesArray[0].ClearPromotedPieces();
                teamPiecesArray[1].ClearPromotedPieces();
            }
            else
            {
                teamTurn = otherTeam;
            }
            // raise event
            if (OnPlayerTurn != null)
                OnPlayerTurn(teamTurn == EChessTeam.White);
        }
        UpdatePieces();
    }

    public void SetBoardState(string state)
    {
        boardState.SetFromCode(state);
        UpdatePieces();
    }

    // used to instantiate newly promoted queen
    private void AddQueenAtPos(int pos)
    {
        teamPiecesArray[GetChessTeamValue(teamTurn)].AddPiece(EPieceType.Queen);
        GameObject[] crtTeamPrefabs = (teamTurn == EChessTeam.White) ? whitePiecesPrefab : blackPiecesPrefab;
        GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)EPieceType.Queen]);
        teamPiecesArray[GetChessTeamValue(teamTurn)].StorePiece(crtPiece, EPieceType.Queen);
        crtPiece.transform.position = GetWorldPos(pos);
    }

    public bool IsPlayerTurn()
    {
        return teamTurn == EChessTeam.White;
    }

    public BoardSquare GetSquare(int pos)
    {
        return boardState.squares[pos];
    }

    public uint GetScore(EChessTeam team)
    {
        return scores[(int)team];
    }

    private void UpdateBoardPiece(Transform pieceTransform, int destPos)
    {
        pieceTransform.position = GetWorldPos(destPos);
    }

    private Vector3 GetWorldPos(int pos)
    {
        Vector3 piecePos = boardTransform.position;
        piecePos.y += zOffset;
        piecePos.x = -widthOffset + pos % BOARD_SIZE;
        piecePos.z = -widthOffset + pos / BOARD_SIZE;

        return piecePos;
    }

    private int GetBoardPos(Vector3 worldPos)
    {
        int xPos = Mathf.FloorToInt(worldPos.x + widthOffset) % BOARD_SIZE;
        int zPos = Mathf.FloorToInt(worldPos.z + widthOffset);

        return xPos + zPos * BOARD_SIZE;
    }

    public void SetTeamTurn(EChessTeam team)
    {
        teamTurn = team;
    }

    #endregion

    #region MonoBehaviour

    private TeamPieces[] teamPiecesArray = new TeamPieces[2];
    private float zOffset = 0.5f;
    private float widthOffset = 3.5f;

    void Start()
    {
        pieceLayerMask = 1 << LayerMask.NameToLayer("Piece");
        boardLayerMask = 1 << LayerMask.NameToLayer("Board");

        boardTransform = GameObject.FindGameObjectWithTag("Board").transform;

        LoadPiecesPrefab();

        boardState = new BoardState();

        PrepareGame();

        teamPiecesArray[0] = null;
        teamPiecesArray[1] = null;

        CreatePieces();

        if (OnPlayerTurn != null)
            OnPlayerTurn(teamTurn == EChessTeam.White);
        if (OnScoreUpdated != null)
            OnScoreUpdated(scores[0], scores[1]);
    }

    void Update()
    {
        if(bIsMultiplayer)
            MultiplayerUpdate();
        else
            SinglePlayerUpdate();
    }

    void SinglePlayerUpdate()
    {
        // human player always plays white
        if (teamTurn == EChessTeam.White)
            UpdatePlayerTurn();
        // AI plays black
        else if (isAIEnabled)
            UpdateAITurn();
        else
            UpdatePlayerTurn();
    }

    void MultiplayerUpdate()
    {
        if (teamTurn == localPlayerTeam)
            UpdatePlayerTurn();
        //if it isnt your turn, simply wait for it to be your turn again
    }

    #endregion

    #region Pieces

    GameObject[] whitePiecesPrefab = new GameObject[6];
    GameObject[] blackPiecesPrefab = new GameObject[6];

    void LoadPiecesPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhitePawn");
        whitePiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKing");
        whitePiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteQueen");
        whitePiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteRook");
        whitePiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKnight");
        whitePiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteBishop");
        whitePiecesPrefab[(uint)EPieceType.Bishop] = prefab;

        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackPawn");
        blackPiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKing");
        blackPiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackQueen");
        blackPiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackRook");
        blackPiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKnight");
        blackPiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackBishop");
        blackPiecesPrefab[(uint)EPieceType.Bishop] = prefab;
    }

    void CreatePieces()
    {
        // Instantiate all pieces according to board data
        if (teamPiecesArray[0] == null)
            teamPiecesArray[0] = new TeamPieces();
        if (teamPiecesArray[1] == null)
            teamPiecesArray[1] = new TeamPieces();

        GameObject[] crtTeamPrefabs = null;
        int crtPos = 0;
        foreach (BoardSquare square in boardState.squares)
        {
            crtTeamPrefabs = (square.team == EChessTeam.White) ? whitePiecesPrefab : blackPiecesPrefab;
            if (square.piece != EPieceType.None)
            {
                GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)square.piece]);
                teamPiecesArray[GetChessTeamValue(square.team)].StorePiece(crtPiece, square.piece);

                // set position
                Vector3 piecePos = boardTransform.position;
                piecePos.y += zOffset;
                piecePos.x = -widthOffset + crtPos % BOARD_SIZE;
                piecePos.z = -widthOffset + crtPos / BOARD_SIZE;
                crtPiece.transform.position = piecePos;
            }
            crtPos++;
        }
    }

    void UpdatePieces()
    {
        teamPiecesArray[0].Hide();
        teamPiecesArray[1].Hide();

        for (int i = 0; i < boardState.squares.Count; i++)
        {
            BoardSquare square = boardState.squares[i];
            if (square.team == EChessTeam.None)
                continue;

            int teamId = GetChessTeamValue(square.team);
            EPieceType pieceType = square.piece;

            teamPiecesArray[teamId].SetPieceAtPos(pieceType, GetWorldPos(i));
        }
    }

    public void ResetGame()
    {
        boardState.Reset();
        NetworkingManager networkingManager = NetworkingManager.Instance;
        if (networkingManager != null)
            networkingManager.DestroyNetworkObjects();
        GUIManager guiManager = GUIManager.Instance;
        if (guiManager != null)
            guiManager.SetMenu();
    }

    #endregion

    #region Gameplay

    Transform grabbed = null;
    float maxDistance = 100f;
    int startPos = 0;
    int destPos = 0;

    public void SetAiEnabled(bool enabled)
    {
        isAIEnabled = enabled;
    }

    public void SetLocalTeam(EChessTeam team)
    {
        localPlayerTeam = team;
        if(cameraRoot)
        {
            switch(localPlayerTeam)
            {
                case EChessTeam.White:
                    cameraRoot.rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case EChessTeam.Black:
                    cameraRoot.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                default:
                    cameraRoot.rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
            }
        }
    }

    void UpdateAITurn()
    {
        Move move = chessAI.ComputeMove();
        PlayTurn(move);
    }

    void UpdatePlayerTurn()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbed)
                ComputeDrag();
            else
                ComputeGrab();
        }
        else if (grabbed != null)
        {
            // find matching square when releasing grabbed piece
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, boardLayerMask))
            {
                grabbed.root.position = hit.transform.position + Vector3.up * zOffset;
            }

            destPos = GetBoardPos(grabbed.root.position);
            if(bIsMultiplayer)
                grabbed.root.position = GetWorldPos(startPos); //snap back to square. in multiplayer, we dont validate the move here, we wait for server confirmation
            if (startPos != destPos)
            {
                Move move = new Move();
                move.from = startPos;
                move.to = destPos;

                if (bIsMultiplayer)
                    PlayTurnMultiplayer(move);
                else
                {
                    PlayTurn(move);
                }
            }
            else
            {
                grabbed.root.position = GetWorldPos(startPos);
            }
            grabbed = null;
        }
    }

    void ComputeDrag()
    {
        // drag grabbed piece on board
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, boardLayerMask))
        {
            grabbed.root.position = hit.point;
        }
    }

    void ComputeGrab()
    {
        // grab a new chess piece from board
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, maxDistance, pieceLayerMask))
        {
            if(hit.transform.tag != localPlayerTeam.ToString())
                return; //not your piece, cannot grab it
            grabbed = hit.transform;
            startPos = GetBoardPos(hit.transform.position);
        }
    }

    #endregion
}
