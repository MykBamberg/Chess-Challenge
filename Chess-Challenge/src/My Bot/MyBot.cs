using ChessChallenge.API;
using System;
using System.Linq.Expressions;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 350, 550, 900, 400 };
    float[] relativeValue = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,   //null
                              0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                              0.0f, 1.0f, 1.2f, 1.6f, 1.5f, 1.4f, 2.0f, 0.0f,   //pawn
                              1.0f, 1.0f, 1.1f, 1.3f, 1.3f, 1.1f, 1.0f, 1.0f,
                              0.8f, 0.9f, 1.3f, 1.0f, 1.0f, 1.3f, 0.9f, 0.8f,   //knight
                              0.7f, 0.9f, 1.3f, 1.0f, 1.0f, 1.3f, 0.9f, 0.7f,
                              0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,   //bishop
                              0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,
                              1.0f, 0.9f, 1.2f, 1.2f, 1.2f, 1.0f, 1.0f, 1.3f,   //rook
                              1.1f, 1.0f, 1.0f, 1.1f, 1.1f, 1.0f, 1.0f, 1.1f,
                              0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,   //queen
                              0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,
                              1.3f, 1.2f, 0.8f, 0.8f, 0.8f, 0.7f, 0.6f, 0.6f,   //king
                              1.2f, 1.4f, 1.0f, 0.8f, 1.0f, 1.0f, 1.4f, 1.2f};


    public Move Think(Board board, Timer timer)
    {
        float boardEvaluation(int depth, ulong initBoard, float abordOver = float.MaxValue)
        {
            if (board.IsInCheckmate())
                return float.MinValue;
            if (board.ZobristKey == initBoard)
                return float.NaN;
            if (board.IsDraw())
                return 0;
            
            if (depth <= 0)
            {
                float material = 0;

                for(int i = 0; i < 64; i++)
                {
                    Piece piece = board.GetPiece(new Square(i));    //i = rank(row) * 8 + file(column)
                    if (!piece.IsNull)
                    {
                        bool isWhite = piece.IsWhite;

                        int whiteMultiplier = isWhite ? 1 : -1;
                        int row = isWhite ? i / 8 : 7 - (i / 8);
                        int column = isWhite ? i % 8 : 7 - (i % 8);

                        material += whiteMultiplier *
                            MathF.Sqrt(
                                relativeValue[(16 * ((int)piece.PieceType)) + row] *
                                relativeValue[(16 * ((int)piece.PieceType)) + column + 8]) *
                            pieceValues[(int)piece.PieceType];
                    }
                }

                return (float)(board.IsWhiteToMove ? material : -material);
            }

            Move[] legalMoves = board.GetLegalMoves();

            float x = float.MinValue;
            float highestOpponentEval = float.MinValue;

            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);

                float eval = boardEvaluation(depth - 1, initBoard, highestOpponentEval);

                if (-eval > x && eval != float.NaN)
                {
                    x = -eval;
                    if (x > abordOver)
                    {
                        board.UndoMove(move);
                        break;
                    }
                    if (eval >= highestOpponentEval)
                    {
                        highestOpponentEval = eval;
                    }
                }
                

                board.UndoMove(move);
            }
            return x;
        }

        Move[] allMoves = board.GetLegalMoves();
        ulong boardHash = board.ZobristKey;

        Move moveToPlay = allMoves[0];
        float lowestOpponentEval = float.MaxValue;
        float highestOpponentEval = float.MinValue;

        try
        {
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);

                float eval = boardEvaluation(4, boardHash, highestOpponentEval);

                if (eval < lowestOpponentEval)
                {
                    lowestOpponentEval = eval;
                    moveToPlay = move;
                }
                if (eval > highestOpponentEval)
                {
                    highestOpponentEval = eval;
                }

                board.UndoMove(move);
            }
        }
        catch (Exception e) 
        {
            Console.WriteLine(e.Message);
        }


        return moveToPlay;
    }
}
