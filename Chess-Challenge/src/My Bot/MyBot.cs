using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class MyBot : IChessBot {
    /* Piece values: null, pawn, knight, bishop, rook, queen, king */
    int[] pieceValues = { 0, 100, 300, 350, 550, 900, 0 };
    float[] relativeValue = {
        0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,   /* null */
        0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f,
        0.0f, 1.0f, 1.2f, 1.6f, 1.5f, 1.4f, 2.0f, 0.0f,   /* pawn */
        1.0f, 1.0f, 1.1f, 1.3f, 1.3f, 1.1f, 1.0f, 1.0f,
        0.9f, 0.9f, 1.1f, 1.0f, 1.0f, 1.1f, 0.9f, 0.9f,   /* knight */
        0.6f, 0.9f, 1.1f, 1.0f, 1.0f, 1.1f, 0.9f, 0.6f,
        0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,   /* bishop */
        0.7f, 1.0f, 1.2f, 1.4f, 1.4f, 1.2f, 1.0f, 0.7f,
        1.0f, 0.9f, 1.2f, 1.2f, 1.2f, 1.0f, 1.0f, 1.3f,   /* rook */
        1.1f, 1.0f, 1.0f, 1.1f, 1.1f, 1.0f, 1.0f, 1.1f,
        1.1f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,   /* queen */
        0.8f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 0.8f,
        1.3f, 1.2f, 0.8f, 0.8f, 0.8f, 0.7f, 0.6f, 0.6f,   /* king */
        1.2f, 1.2f, 1.4f, 0.8f, 1.0f, 1.0f, 1.4f, 1.2f
    };

    Dictionary<ulong, float> evaluations = new();

    float boardEvaluation(Board board) {
        ulong boardHash = board.ZobristKey;
        
        if (evaluations.ContainsKey(boardHash)) {
            return evaluations[boardHash];
        }

        if (board.IsInCheckmate()) {
            evaluations[boardHash] = board.IsWhiteToMove ? float.MinValue : float.MaxValue;
        } else if (board.IsDraw()) {
            evaluations[boardHash] = 0;
        } else {
            float material = 0;

            for (int i = 0; i < 64; i++) {
                Piece piece = board.GetPiece(new Square(i));    /* i = rank(row) * 8 + file(column) */
                if (!piece.IsNull) {
                    bool isWhite = piece.IsWhite;

                    int whiteMultiplier = isWhite ? 1 : -1;
                    int row = isWhite ? i / 8 : 7 - i / 8;
                    int column = i % 8;

                    material += whiteMultiplier *
                        relativeValue[(16 * ((int)piece.PieceType)) + row] *
                        relativeValue[(16 * ((int)piece.PieceType)) + column + 8] *
                        pieceValues[(int)piece.PieceType];
                }
            }
            evaluations[boardHash] = material;
        }

        return evaluations[boardHash];
    }

    Dictionary<ulong, (float eval, Move move)> bestMoves = new();

    Move bestMove(out float evaluation, Board board, int depth, float alpha = float.MinValue, float beta = float.MaxValue) {
        ulong hash = board.ZobristKey - (ulong)depth;

        if (bestMoves.ContainsKey(hash)) {
            evaluation = bestMoves[hash].eval;
            return bestMoves[hash].move;
        }

        Move[] legalMoves = board.GetLegalMoves();

        if (legalMoves.Length == 0) {
            if (board.IsInCheckmate()) {
                evaluation = board.IsWhiteToMove ? float.MinValue : float.MaxValue;
                return Move.NullMove;
            }

            evaluation = 0.0f;
            return Move.NullMove;
        }

        if (depth >= 1) {
            List<(Move move, float eval)> moveEvalPairs = new(legalMoves.Length);

            for (int i = 0; i < legalMoves.Length; i++) {
                board.MakeMove(legalMoves[i]);
                float eval = (board.IsWhiteToMove? 1 : -1) * boardEvaluation(board);
                board.UndoMove(legalMoves[i]);

                moveEvalPairs.Add((legalMoves[i], eval));
            }

            moveEvalPairs.Sort((a, b) => a.eval.CompareTo(b.eval));

            legalMoves = moveEvalPairs.ConvertAll((e) => e.move).ToArray();
        }

        Move moveToPlay = legalMoves[0];

        float bestEval = board.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity;
        foreach (Move move in legalMoves) {
            board.MakeMove(move);
            float eval;
            if (depth > 0) {
                bestMove(out eval, board, depth - 1, alpha, beta);
            } else {
                eval = boardEvaluation(board);
            }
            board.UndoMove(move);

            if ((board.IsWhiteToMove && eval > bestEval) || (!board.IsWhiteToMove && eval < bestEval)) {
                bestEval = eval;
                moveToPlay = move;
            }

            if (board.IsWhiteToMove) {
                alpha = MathF.Max(alpha, eval);
            } else {
                beta = MathF.Min(beta, eval);
            }
            if (beta <= alpha) {
                break;
            }
        }

        bestMoves[hash] = (bestEval, moveToPlay);
        evaluation = bestMoves[hash].eval;
        return bestMoves[hash].move;
    }

    public Move Think(Board board, Timer timer) {
        int depth = 5 + (int)(Math.Log2(timer.MillisecondsRemaining / 40000.0) / 2.0);
        if (board.GetLegalMoves().Length < 50) {
            depth = Math.Max(depth, 4);
        }
        depth = Math.Max(depth, 2);

        Console.WriteLine("Simple evaluation: {0}", boardEvaluation(board) / 100.0f);

        float eval;
        Move moveToPlay = bestMove(out eval, board, depth);

        Console.WriteLine("Evaluation: {0}", eval / 100.0f);
        Console.WriteLine("Time elapsed: {0}ms", timer.MillisecondsElapsedThisTurn);
        Console.WriteLine("Move: {0}", moveToPlay);
        Console.Write("\n");

        return moveToPlay;
    }
}
