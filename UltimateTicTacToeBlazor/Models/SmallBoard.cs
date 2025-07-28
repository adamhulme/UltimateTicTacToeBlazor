namespace UltimateTicTacToe.Models;

public class SmallBoard
{
    public Cell[,] Cells { get; } = new Cell[3, 3];
    public BoardState State { get; private set; } = BoardState.Active;

    public SmallBoard()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                Cells[r, c] = new Cell { Row = r, Col = c };
    }

    public bool Move(int row, int col, CellState player)
    {
        if (State != BoardState.Active)
            return false;
        if (Cells[row, col].State != CellState.Empty)
            return false;

        Cells[row, col].State = player;
        UpdateBoardState();
        return true;
    }

    private void UpdateBoardState()
    {
        var winner = GetWinner();
        if (winner != CellState.Empty)
        {
            State = winner == CellState.X ? BoardState.WonByX : BoardState.WonByO;
            return;
        }

        if (IsFull())
            State = BoardState.Draw;
    }

    private bool IsFull()
    {
        foreach (var cell in Cells)
            if (cell.State == CellState.Empty)
                return false;
        return true;
    }

    private CellState GetWinner()
    {
        CellState CheckLine(Cell a, Cell b, Cell c)
        {
            if (a.State == CellState.Empty)
                return CellState.Empty;
            if (a.State == b.State && b.State == c.State)
                return a.State;
            return CellState.Empty;
        }

        for (int i = 0; i < 3; i++)
        {
            var rowWin = CheckLine(Cells[i, 0], Cells[i, 1], Cells[i, 2]);
            var colWin = CheckLine(Cells[0, i], Cells[1, i], Cells[2, i]);
            if (rowWin != CellState.Empty)
                return rowWin;
            if (colWin != CellState.Empty)
                return colWin;
        }

        var diag1 = CheckLine(Cells[0, 0], Cells[1, 1], Cells[2, 2]);
        var diag2 = CheckLine(Cells[0, 2], Cells[1, 1], Cells[2, 0]);

        if (diag1 != CellState.Empty) return diag1;
        if (diag2 != CellState.Empty) return diag2;

        return CellState.Empty;
    }
}
