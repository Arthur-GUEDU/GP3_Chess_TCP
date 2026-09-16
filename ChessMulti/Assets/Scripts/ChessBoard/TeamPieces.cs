using UnityEngine;

/*
 * This class holds chess piece gameObjects for a team
 * Instantiated for each team by the ChessGameMgr
 * It can hide or set a piece at a given position
 */

public partial class ChessGameManager
{
    public class TeamPieces
    {
        private GameObject[][] pieceTypeArray;

        public TeamPieces()
        {
            pieceTypeArray = new GameObject[(uint)EPieceType.NbPieces][];
            pieceTypeArray[(uint)EPieceType.Pawn] = new GameObject[BOARD_SIZE];
            pieceTypeArray[(uint)EPieceType.King] = new GameObject[1];
            pieceTypeArray[(uint)EPieceType.Queen] = new GameObject[1];
            pieceTypeArray[(uint)EPieceType.Bishop] = new GameObject[2];
            pieceTypeArray[(uint)EPieceType.Knight] = new GameObject[2];
            pieceTypeArray[(uint)EPieceType.Rook] = new GameObject[2];
        }

        // Add a piece during gameplay - used for pawn promotion
        public void AddPiece(EPieceType type)
        {
            GameObject[] pieces = new GameObject[pieceTypeArray[(uint)type].Length + 1];
            for (int i = 0; i < pieceTypeArray[(uint)type].Length; i++)
                pieces[i] = pieceTypeArray[(uint)type][i];

            pieceTypeArray[(uint)type] = pieces;
        }

        public void ClearPromotedPieces()
        {
            GameObject[] pieces = new GameObject[1];
            pieces[0] = pieceTypeArray[(uint)EPieceType.Queen][0];

            for (int i = 1; i < pieceTypeArray[(uint)EPieceType.Queen].Length; i++)
                Destroy(pieceTypeArray[(uint)EPieceType.Queen][i]);

            pieceTypeArray[(uint)EPieceType.Queen] = pieces;
        }

        public void Hide()
        {
            foreach (GameObject[] gaoArray in pieceTypeArray)
            {
                foreach (GameObject gao in gaoArray)
                {
                    if (gao)
                        gao.SetActive(false);
                }
            }
        }

        private void StorePieceInCategory(GameObject crtPiece, GameObject[] pieceArray)
        {
            int i = 0;
            while (i < pieceArray.Length && pieceArray[i] != null) i++;
            pieceArray[i] = crtPiece;
        }

        public void StorePiece(GameObject crtPiece, EPieceType pieceType)
        {
            StorePieceInCategory(crtPiece, pieceTypeArray[(uint)pieceType]);
        }

        private void SetPieceCategoryAt(GameObject[] pieceArray, Vector3 pos)
        {
            int i = 0;
            while (i < pieceArray.Length && pieceArray[i].activeSelf) i++;

            pieceArray[i].SetActive(true);
            pieceArray[i].transform.position = pos;
        }

        public void SetPieceAtPos(EPieceType pieceType, Vector3 pos)
        {
            SetPieceCategoryAt(pieceTypeArray[(uint)pieceType], pos);
        }
    }
}
