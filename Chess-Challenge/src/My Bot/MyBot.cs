using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 350, 550, 900, 0 };
    float[] relativeValue = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,   //null
                              0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
                              0.0f, 1.0f, 1.2f, 1.6f, 1.5f, 1.4f, 2.0f, 0.0f,   //pawn
                              1.0f, 1.0f, 1.1f, 1.3f, 1.3f, 1.1f, 1.0f, 1.0f,
                              0.9f, 0.9f, 1.1f, 1.0f, 1.0f, 1.1f, 0.9f, 0.9f,   //knight
                              0.6f, 0.9f, 1.3f, 1.0f, 1.0f, 1.3f, 0.9f, 0.6f,
                              0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,   //bishop
                              0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,
                              1.0f, 0.9f, 1.2f, 1.2f, 1.2f, 1.0f, 1.0f, 1.3f,   //rook
                              1.1f, 1.0f, 1.0f, 1.1f, 1.1f, 1.0f, 1.0f, 1.1f,
                              1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,   //queen
                              0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,
                              1.3f, 1.2f, 0.8f, 0.8f, 0.8f, 0.7f, 0.6f, 0.6f,   //king
                              1.2f, 1.2f, 1.4f, 0.8f, 1.0f, 1.0f, 1.4f, 1.2f};


    public Move Think(Board board, Timer timer)
    {
        float boardEvaluation()
        {
            float material = 0;

            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(new Square(i));    //i = rank(row) * 8 + file(column)
                if (!piece.IsNull)
                {
                    bool isWhite = piece.IsWhite;

                    int whiteMultiplier = isWhite ? 1 : -1;
                    int row = i / 8;
                    int column = isWhite ? i % 8 : 7 - (i % 8);

                    material += whiteMultiplier *
                        relativeValue[(16 * ((int)piece.PieceType)) + row] *
                        relativeValue[(16 * ((int)piece.PieceType)) + column + 8] *
                        pieceValues[(int)piece.PieceType];
                }
            }
            return material;
        }

        Move bestMove(out float evaluation, int depth, float alpha = float.MinValue, float beta = float.MaxValue)
        {
            List<Move> legalMoves = new(board.GetLegalMoves());
            if (legalMoves.Count == 0)
            {
                if (board.IsInCheckmate())
                {
                    evaluation = board.IsWhiteToMove ? float.MinValue : float.MaxValue;
                    return Move.NullMove;
                }

                evaluation = 0.0f;
                return Move.NullMove;
            }
            if (depth >= 2)
            {
                for (int i = 0; i < legalMoves.Count; i++)
                {
                    if (legalMoves[i].IsCapture || legalMoves[i].IsPromotion || legalMoves[i].IsCastles)
                    {
                        Move t = legalMoves[i];
                        legalMoves.RemoveAt(i);
                        legalMoves.Insert(i, t);
                    }
                }
            }
            Move moveToPlay = legalMoves[0];

            if (board.IsWhiteToMove)
            {
                float maxEval = float.NegativeInfinity;
                foreach (Move move in legalMoves)
                {
                    board.MakeMove(move);
                    float eval;
                    if (depth > 0)
                        bestMove(out eval, depth - 1, alpha, beta);
                    else
                        eval = boardEvaluation();
                    if (eval > maxEval)
                    {
                        maxEval = eval;
                        moveToPlay = move;
                    }
                    alpha = MathF.Max(alpha, eval);
                    if (beta <= alpha)
                    {
                        board.UndoMove(move);
                        break;
                    }
                    board.UndoMove(move);
                }
                evaluation = maxEval;
                return moveToPlay;
            }
            else
            {
                float minEval = float.PositiveInfinity;
                foreach (Move move in legalMoves)
                {
                    board.MakeMove(move);
                    float eval;
                    if (depth > 0)
                        bestMove(out eval, depth - 1, alpha, beta);
                    else
                        eval = boardEvaluation();
                    if (eval < minEval)
                    {
                        minEval = eval;
                        moveToPlay = move;
                    }
                    beta = MathF.Min(beta, eval);
                    if (beta <= alpha)
                    {
                        board.UndoMove(move);
                        break;
                    }
                    board.UndoMove(move);
                }
                evaluation = minEval;
                return moveToPlay;
            }
        }


        Move moveToPlay = Move.NullMove;
        try
        {
            /*Console.Write("\nSimple evaluation:");
            Console.WriteLine(boardEvaluation() / 100.0f);*/
            float eval;
            moveToPlay = bestMove(out eval, 5);

            /*Console.WriteLine(eval / 100.0f);
            Console.WriteLine(timer.MillisecondsElapsedThisTurn);
            Console.WriteLine(moveToPlay);*/
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }


        return moveToPlay;
    }
}
