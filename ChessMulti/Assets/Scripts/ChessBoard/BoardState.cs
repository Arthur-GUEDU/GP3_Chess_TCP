using System.Collections.Generic;

/*
 * The BoardState class stores the internal data values of the board
 * It holds a list of BoardSquare structs that contains info for each square : the type of piece (pawn, king, ... , none) and the team of the piece
 * It also contains methods to get valid moves for each type of piece accoring to the current board configuration
 * It can apply a selected move for a piece and eventually reset its values to default
 */

public partial class ChessGameManager
{
    public struct BoardPos
    {
        public int X { get; set; }
        public int Y { get; set; }

        public BoardPos(int pos) { X = pos % BOARD_SIZE; Y = pos / BOARD_SIZE; }
        public BoardPos(int _x, int _y) { X = _x; Y = _y; }

        public static implicit operator int(BoardPos pos) { return pos.X + pos.Y * BOARD_SIZE; }

        static public int operator +(BoardPos pos1, BoardPos pos2)
        {
            int x = pos1.X + pos2.X;
            int y = pos1.Y + pos2.Y;

            return (x >= 0 && x < BOARD_SIZE && y >= 0 && y < BOARD_SIZE) ? new BoardPos(x, y) : -1;
        }

        public int GetRight()
        {
            return (X == BOARD_SIZE - 1) ? -1 : new BoardPos(X + 1, Y);
        }

        public int GetLeft()
        {
            return (X == 0) ? -1 : new BoardPos(X - 1, Y);
        }

        public int GetTop()
        {
            return (Y == BOARD_SIZE - 1) ? -1 : new BoardPos(X, Y + 1);
        }

        public int GetBottom()
        {
            return (Y == 0) ? -1 : new BoardPos(X, Y - 1);
        }
    }

    public class BoardState
    {
        public enum EMoveResult
        {
            Normal,
            Promotion,
            Castling_Long,
            Castling_Short
        }

        public List<BoardSquare> squares = null;

        private bool isWhiteCastlingDone = false;
        private bool isBlackCastlingDone = false;

        public string GetCode()
        {
            string code = "";
            foreach(BoardSquare square in squares)
            {
                code += square.GetCode();
            }
            return code;
        }

        public void SetFromCode(string code)
        {
            if (code.Length != BOARD_SIZE * BOARD_SIZE * 2)
                return;
            if (squares == null)
            {
                squares = new List<BoardSquare>();
                for (int i = 0; i < BOARD_SIZE * BOARD_SIZE; ++i)
                {
                    squares.Add(new BoardSquare());
                }
            }
            for (int i = 0; i < squares.Count; ++i)
            {
                string squareCode = code.Substring(i * 2, 2);
                BoardSquare square = squares[i];
                square.SetFromCode(squareCode);
                squares[i] = square;
            }
        }

        public bool IsValidSquare(int pos, EChessTeam team, int teamFlag)
        {
            if (pos < 0)
                return false;

            bool isTeamValid = ((squares[pos].team == EChessTeam.None) && ((teamFlag & (int)ETeamFlag.None) > 0)) ||
                ((squares[pos].team != team && squares[pos].team != EChessTeam.None) && ((teamFlag & (int)ETeamFlag.Enemy) > 0));

            return isTeamValid;
        }

        public void AddMoveIfValidSquare(EChessTeam team, int from, int to, List<Move> moves, int teamFlag = (int)ETeamFlag.Enemy | (int)ETeamFlag.None)
        {
            if (IsValidSquare(to, team, teamFlag))
            {
                Move move;
                move.from = from;
                move.to = to;
                moves.Add(move);
            }
        }

        public void GetValidKingMoves(EChessTeam team, int pos, List<Move> moves)
        {
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, 0)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(0, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, 1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, 0)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(-1, -1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(0, -1)), moves);
            AddMoveIfValidSquare(team, pos, (new BoardPos(pos) + new BoardPos(1, -1)), moves);
        }

        public void GetValidQueenMoves(EChessTeam team, int pos, List<Move> moves)
        {
            GetValidRookMoves(team, pos, moves);
            GetValidBishopMoves(team, pos, moves);
        }

        public void GetValidPawnMoves(EChessTeam team, int pos, List<Move> moves)
        {
            int frontPos = -1, leftFrontPos = -1, rightFrontPos = -1;
            if (team == EChessTeam.White)
            {
                frontPos = new BoardPos(pos).GetTop();
                if (frontPos != -1)
                {
                    leftFrontPos = new BoardPos(frontPos).GetLeft();
                    rightFrontPos = new BoardPos(frontPos).GetRight();
                }
                if ( new BoardPos(pos).Y == 1 && squares[pos + BOARD_SIZE].piece == EPieceType.None)
                {
                    AddMoveIfValidSquare(team, pos, new BoardPos(frontPos).GetTop(), moves, (int)ETeamFlag.None);
                }
            }
            else
            {
                frontPos = new BoardPos(pos).GetBottom();
                if (frontPos != -1)
                {
                    rightFrontPos = new BoardPos(frontPos).GetLeft();
                    leftFrontPos = new BoardPos(frontPos).GetRight();
                }

                if (new BoardPos(pos).Y == 6 && squares[pos - BOARD_SIZE].piece == EPieceType.None)
                {
                    AddMoveIfValidSquare(team, pos, new BoardPos(frontPos).GetBottom(), moves, (int)ETeamFlag.None);
                }
            }

            AddMoveIfValidSquare(team, pos, frontPos, moves, (int)ETeamFlag.None);
            AddMoveIfValidSquare(team, pos, leftFrontPos, moves, (int)ETeamFlag.Enemy);
            AddMoveIfValidSquare(team, pos, rightFrontPos, moves, (int)ETeamFlag.Enemy);
        }

        public void GetValidRookMoves(EChessTeam team, int pos, List<Move> moves)
        {
            bool doBreak = false;
            int topPos = new BoardPos(pos).GetTop();
            while (!doBreak && topPos >= 0 && squares[topPos].team != team)
            {
                AddMoveIfValidSquare(team, pos, topPos, moves);
                doBreak = squares[topPos].team != EChessTeam.None;
                topPos = new BoardPos(topPos).GetTop();
            }

            doBreak = false;
            int bottomPos = new BoardPos(pos).GetBottom();
            while (!doBreak && bottomPos >= 0 && squares[bottomPos].team != team)
            {
                AddMoveIfValidSquare(team, pos, bottomPos, moves);
                doBreak = squares[bottomPos].team != EChessTeam.None;
                bottomPos = new BoardPos(bottomPos).GetBottom();
            }

            doBreak = false;
            int leftPos = new BoardPos(pos).GetLeft();
            while (!doBreak && leftPos >= 0 && squares[leftPos].team != team)
            {
                AddMoveIfValidSquare(team, pos, leftPos, moves);
                doBreak = squares[leftPos].team != EChessTeam.None;
                leftPos = new BoardPos(leftPos).GetLeft();
            }

            doBreak = false;
            int rightPos = new BoardPos(pos).GetRight();
            while (!doBreak && rightPos >= 0 && squares[rightPos].team != team)
            {
                AddMoveIfValidSquare(team, pos, rightPos, moves);
                doBreak = squares[rightPos].team != EChessTeam.None;
                rightPos = new BoardPos(rightPos).GetRight();
            }
        }

        public void GetValidBishopMoves(EChessTeam team, int pos, List<Move> moves)
        {
            bool doBreak = false;
            int topRightPos = new BoardPos(pos) + new BoardPos(1, 1);
            while (!doBreak && topRightPos >= 0 && squares[topRightPos].team != team)
            {

                AddMoveIfValidSquare(team, pos, topRightPos, moves);
                doBreak = squares[topRightPos].team != EChessTeam.None;
                topRightPos = new BoardPos(topRightPos) + new BoardPos(1, 1);
            }

            doBreak = false;
            int topLeftPos = new BoardPos(pos) + new BoardPos(-1, 1);
            while (!doBreak && topLeftPos >= 0 && squares[topLeftPos].team != team)
            {

                AddMoveIfValidSquare(team, pos, topLeftPos, moves);
                doBreak = squares[topLeftPos].team != EChessTeam.None;
                topLeftPos = new BoardPos(topLeftPos) + new BoardPos(-1, 1);
            }

            doBreak = false;
            int bottomRightPos = new BoardPos(pos) + new BoardPos(1, -1);
            while (!doBreak && bottomRightPos >= 0 && squares[bottomRightPos].team != team)
            {

                AddMoveIfValidSquare(team, pos, bottomRightPos, moves);
                doBreak = squares[bottomRightPos].team != EChessTeam.None;
                bottomRightPos = new BoardPos(bottomRightPos) + new BoardPos(1, -1);
            }

            doBreak = false;
            int bottomLeftPos = new BoardPos(pos) + new BoardPos(-1, -1);
            while (!doBreak && bottomLeftPos >= 0 && squares[bottomLeftPos].team != team)
            {

                AddMoveIfValidSquare(team, pos, bottomLeftPos, moves);
                doBreak = squares[bottomLeftPos].team != EChessTeam.None;
                bottomLeftPos = new BoardPos(bottomLeftPos) + new BoardPos(-1, -1);
            }
        }

        public void GetValidKnightMoves(EChessTeam team, int pos, List<Move> moves)
        {
            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(1, 2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(2, 1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-1, 2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-2, 1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(1, -2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(2, -1), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-1, -2), moves);

            AddMoveIfValidSquare(team, pos, new BoardPos(pos) + new BoardPos(-2, -1), moves);
        }

        public void GetValidMoves(EChessTeam team, List<Move> moves)
        {
            for (int i = 0; i < BOARD_SIZE * BOARD_SIZE; ++i)
            {
                if (squares[i].team == team)
                {
                    switch (squares[i].piece)
                    {
                        case EPieceType.King: GetValidKingMoves(team, i, moves); break;
                        case EPieceType.Queen: GetValidQueenMoves(team, i, moves); break;
                        case EPieceType.Pawn: GetValidPawnMoves(team, i, moves); break;
                        case EPieceType.Rook: GetValidRookMoves(team, i, moves); break;
                        case EPieceType.Bishop: GetValidBishopMoves(team, i, moves); break;
                        case EPieceType.Knight: GetValidKnightMoves(team, i, moves); break;
                        default: break;
                    }
                }
            }
        }

        public bool IsValidMove(EChessTeam team, Move move)
        {
            List<Move> validMoves = new List<Move>();
            GetValidMoves(team, validMoves);

            return validMoves.Contains(move);
        }

        // returns move result if a special move occured (pawn promotion, castling...)
        public EMoveResult PlayUnsafeMove(Move move)
        {
            squares[move.to] = squares[move.from];

            BoardSquare square = squares[move.from];
            square.piece = EPieceType.None;
            square.team = EChessTeam.None;
            squares[move.from] = square;

            if (CanPromotePawn(move))
            {
                // promote pawn to queen
                BoardSquare destSquare = squares[move.to];
                SetPieceAtSquare(move.to, destSquare.team, EPieceType.Queen);
                return EMoveResult.Promotion;
            }
            // Castling move
            return ComputeCastling(move);
        }

        private bool CanPromotePawn(Move move)
        {
            BoardSquare destSquare = squares[move.to];
            if (destSquare.piece == EPieceType.Pawn)
            {
                BoardPos pos = new BoardPos(move.to);
                if (destSquare.team == EChessTeam.Black && pos.Y == 0 || destSquare.team == EChessTeam.White && pos.Y == (BOARD_SIZE - 1))
                    return true;
            }
            return false;
        }

        // compute castling move if applicable
        private EMoveResult ComputeCastling(Move move)
        {
            BoardSquare destSquare = squares[move.to];

            if ((destSquare.team == EChessTeam.White && isWhiteCastlingDone)
             || (destSquare.team == EChessTeam.Black && isBlackCastlingDone))
                return EMoveResult.Normal;

            // rook piece
            if (destSquare.piece == EPieceType.Rook)
            {
                // short castling case
                if ((destSquare.team == EChessTeam.White && move.from == (BOARD_SIZE - 1) && move.to == 5) // white line
                 || (destSquare.team == EChessTeam.Black && move.from == (squares.Count - 1) && move.to == squares.Count - 3)) // black line
                {
                    if (TryExecuteCastling(move.to, true))
                        return EMoveResult.Castling_Short;
                }
                // long castling case
                if ((destSquare.team == EChessTeam.White && move.from == 0 && move.to == 3) // white line
                || (destSquare.team == EChessTeam.Black && move.from == (squares.Count - 8) && move.to == squares.Count - 5)) // black line
                {
                    if (TryExecuteCastling(move.to, false))
                        return EMoveResult.Castling_Long;
                }
            }

            return EMoveResult.Normal;
        }

        private bool TryExecuteCastling(int moveToIndex, bool isShortCastling)
        {
            int kingSquareIndex = isShortCastling ? (moveToIndex - 1) : moveToIndex + 1;
            int kingFinalSquareIndex = isShortCastling ? (moveToIndex + 1) : moveToIndex - 1;
            BoardSquare destSquare = squares[moveToIndex];
            BoardSquare kingSquare = squares[kingSquareIndex];
            if (kingSquare.piece == EPieceType.King && kingSquare.team == destSquare.team)
            {
                BoardSquare tempSquare = kingSquare; // king square to be moved
                squares[kingSquareIndex] = BoardSquare.Empty(); // replace by empty square
                squares[kingFinalSquareIndex] = tempSquare;

                if (destSquare.team == EChessTeam.White)
                    isWhiteCastlingDone = true;
                else
                    isBlackCastlingDone = true;

                return true;
            }

            return false;
        }

        // approximation : opponent king must be "eaten" to win instead of detecting checkmate state
        public bool DoesTeamLose(EChessTeam team)
        {
            for (int i = 0; i < squares.Count; ++i)
            {
                if (squares[i].team == team && squares[i].piece == EPieceType.King)
                {
                    return false;
                }
            }
            return true;
        }

        private void SetPieceAtSquare(int index, EChessTeam team, EPieceType piece)
        {
            if (index > squares.Count)
                return;
            BoardSquare square = squares[index];
            square.piece = piece;
            square.team = team;
            squares[index] = square;
        }

        public void Reset()
        {
            isWhiteCastlingDone = false;
            isBlackCastlingDone = false;

            if (squares == null)
            {
                squares = new List<BoardSquare>();

                // init squares
                for (int i = 0; i < BOARD_SIZE * BOARD_SIZE; i++)
                {
                    BoardSquare square = new BoardSquare();
                    square.piece = EPieceType.None;
                    square.team = EChessTeam.None;
                    squares.Add(square);
                }
            }
            else
            {
                for (int i = 0; i < squares.Count; ++i)
                {
                    SetPieceAtSquare(i, EChessTeam.None, EPieceType.None);
                }
            }

             // White
            for (int i = BOARD_SIZE; i < BOARD_SIZE*2; ++i)
            {
                SetPieceAtSquare(i, EChessTeam.White, EPieceType.Pawn);
            }
            SetPieceAtSquare(0, EChessTeam.White, EPieceType.Rook);
            SetPieceAtSquare(1, EChessTeam.White, EPieceType.Knight);
            SetPieceAtSquare(2, EChessTeam.White, EPieceType.Bishop);
            SetPieceAtSquare(3, EChessTeam.White, EPieceType.Queen);
            SetPieceAtSquare(4, EChessTeam.White, EPieceType.King);
            SetPieceAtSquare(5, EChessTeam.White, EPieceType.Bishop);
            SetPieceAtSquare(6, EChessTeam.White, EPieceType.Knight);
            SetPieceAtSquare(7, EChessTeam.White, EPieceType.Rook);

            // Black
            for (int i = BOARD_SIZE * (BOARD_SIZE - 2) ; i < BOARD_SIZE * (BOARD_SIZE - 1); ++i)
            {
                SetPieceAtSquare(i, EChessTeam.Black, EPieceType.Pawn);
            }
            int startIndex = BOARD_SIZE * (BOARD_SIZE - 1);
            SetPieceAtSquare(startIndex, EChessTeam.Black, EPieceType.Rook);
            SetPieceAtSquare(startIndex + 1, EChessTeam.Black, EPieceType.Knight);
            SetPieceAtSquare(startIndex + 2, EChessTeam.Black, EPieceType.Bishop);
            SetPieceAtSquare(startIndex + 3, EChessTeam.Black, EPieceType.Queen);
            SetPieceAtSquare(startIndex + 4, EChessTeam.Black, EPieceType.King);
            SetPieceAtSquare(startIndex + 5, EChessTeam.Black, EPieceType.Bishop);
            SetPieceAtSquare(startIndex + 6, EChessTeam.Black, EPieceType.Knight);
            SetPieceAtSquare(startIndex + 7, EChessTeam.Black, EPieceType.Rook);
        }
    }
}

