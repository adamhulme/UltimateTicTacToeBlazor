namespace UltimateTicTacToeBlazor.AI;

using UltimateTicTacToe.Models;

public class SimpleBot : IBot
{
    public (int boardRow, int boardCol, int cellRow, int cellCol)? ChooseMove(UltimateBoard game)
    {
        if (game.State != BoardState.Active) return null;

        var targetBoard = GetTargetBoard(game);
        var current = game.CurrentPlayer;
        var opponent = current == CellState.X ? CellState.O : CellState.X;

        // 1) Try immediate win in target board
        var win = FindTacticalMove(targetBoard, current);
        if (win.HasValue) return (targetBoard.r, targetBoard.c, win.Value.row, win.Value.col);

        // 2) Block opponent win in target board
        var block = FindTacticalMove(targetBoard, opponent);
        if (block.HasValue) return (targetBoard.r, targetBoard.c, block.Value.row, block.Value.col);

        // 3) Prefer center, then corners, then sides in target board
        var pref = PreferCells(targetBoard.board);
        if (pref.HasValue) return (targetBoard.r, targetBoard.c, pref.Value.row, pref.Value.col);

        // 4) If no target board (free move) or target full, pick a reasonable alternative
        foreach (var candidate in EnumerateAllBoards(game))
        {
            if (candidate.board.State != BoardState.Active) continue;
            var tactical = FindTacticalMove(candidate, current) ?? FindTacticalMove(candidate, opponent) ?? PreferCells(candidate.board);
            if (tactical.HasValue) return (candidate.r, candidate.c, tactical.Value.row, tactical.Value.col);
        }

        // 5) Fallback: first available move
        foreach (var candidate in EnumerateAllBoards(game))
        {
            if (candidate.board.State != BoardState.Active) continue;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (candidate.board.Cells[i, j].State == CellState.Empty)
                        return (candidate.r, candidate.c, i, j);
        }

        return null;
    }

    private (int r, int c, SmallBoard board) GetTargetBoard(UltimateBoard game)
    {
        if (game.NextBoardRow.HasValue && game.NextBoardCol.HasValue)
        {
            var r = game.NextBoardRow.Value;
            var c = game.NextBoardCol.Value;
            return (r, c, game.Boards[r, c]);
        }
        // Free move: pick a board with best potential: center, corners, sides
        var order = new (int r, int c)[] { (1,1), (0,0), (0,2), (2,0), (2,2), (0,1), (1,0), (1,2), (2,1) };
        foreach (var (r,c) in order)
        {
            if (game.Boards[r, c].State == BoardState.Active)
                return (r, c, game.Boards[r, c]);
        }
        // If none active, just return first
        return (0, 0, game.Boards[0,0]);
    }

    private (int row, int col)? FindTacticalMove((int r, int c, SmallBoard board) target, CellState player)
    {
        var b = target.board;
        // Try each empty cell; if placing player there wins the small board, choose it
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (b.Cells[i, j].State != CellState.Empty) continue;
                // simulate
                b.Cells[i, j].State = player;
                var win = CheckSmallWin(b);
                b.Cells[i, j].State = CellState.Empty;
                if (win == player) return (i, j);
            }
        }
        return null;
    }

    private CellState CheckSmallWin(SmallBoard b)
    {
        // Minimal check copied from logic: check lines for a win
        CellState Check(CellState a, CellState c, CellState d)
            => a == CellState.Empty ? CellState.Empty : (a == c && c == d ? a : CellState.Empty);
        for (int i = 0; i < 3; i++)
        {
            var r = Check(b.Cells[i,0].State, b.Cells[i,1].State, b.Cells[i,2].State);
            if (r != CellState.Empty) return r;
            var c = Check(b.Cells[0,i].State, b.Cells[1,i].State, b.Cells[2,i].State);
            if (c != CellState.Empty) return c;
        }
        var d1 = Check(b.Cells[0,0].State, b.Cells[1,1].State, b.Cells[2,2].State);
        if (d1 != CellState.Empty) return d1;
        var d2 = Check(b.Cells[0,2].State, b.Cells[1,1].State, b.Cells[2,0].State);
        if (d2 != CellState.Empty) return d2;
        return CellState.Empty;
    }

    private (int row, int col)? PreferCells(SmallBoard b)
    {
        // center, corners, sides
        var order = new (int r, int c)[] { (1,1), (0,0), (0,2), (2,0), (2,2), (0,1), (1,0), (1,2), (2,1) };
        foreach (var (r,c) in order)
        {
            if (b.Cells[r, c].State == CellState.Empty)
                return (r, c);
        }
        return null;
    }

    private System.Collections.Generic.IEnumerable<(int r, int c, SmallBoard board)> EnumerateAllBoards(UltimateBoard game)
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                yield return (r, c, game.Boards[r, c]);
    }
}
