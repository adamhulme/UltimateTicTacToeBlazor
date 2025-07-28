namespace UltimateTicTacToe.Models;

public class UltimateBoard
{
    public SmallBoard[,] Boards { get; } = new SmallBoard[3, 3];
    public BoardState State { get; private set; } = BoardState.Active;

    public int? NextBoardRow { get; private set; } = null;
    public int? NextBoardCol { get; private set; } = null;

    public CellState CurrentPlayer { get; private set; } = CellState.X;
    
    // Compatibility properties for UI and training
    public SmallBoard[,] SmallBoards => Boards;
    public BoardState GameState => State;
    public (int, int)? RequiredBoard => NextBoardRow.HasValue && NextBoardCol.HasValue 
        ? (NextBoardRow.Value, NextBoardCol.Value) : null;
    
    public CellState Winner => State switch
    {
        BoardState.WonByX => CellState.X,
        BoardState.WonByO => CellState.O,
        _ => CellState.Empty
    };
    
    public bool IsGameOver() => State != BoardState.Active;

    public UltimateBoard()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                Boards[r, c] = new SmallBoard();
    }

    public bool MakeMove(int boardRow, int boardCol, int cellRow, int cellCol)
    {
        if (State != BoardState.Active) return false;

        // Check playing in targeted board
        if (NextBoardRow.HasValue && NextBoardCol.HasValue)
        {
            if (boardRow != NextBoardRow.Value || boardCol != NextBoardCol.Value)
                return false;

            if (Boards[NextBoardRow.Value, NextBoardCol.Value].State != BoardState.Active)
                return false;
        }

        var board = Boards[boardRow, boardCol];
        bool success = board.Move(cellRow, cellCol, CurrentPlayer);

        if (!success) return false;

        UpdateGameState();

        var nextBoard = Boards[cellRow, cellCol];
        if (nextBoard.State == BoardState.Active)
        {
            NextBoardRow = cellRow;
            NextBoardCol = cellCol;
        }
        else
        {
            NextBoardRow = null;
            NextBoardCol = null;
        }

        SwapCurrentPlayer();

        return true;
    }

    private void UpdateGameState()
    {
        var meta = new CellState[3, 3];

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                switch (Boards[r, c].State)
                {
                    case BoardState.WonByX:
                        meta[r, c] = CellState.X;
                        break;
                    case BoardState.WonByO:
                        meta[r, c] = CellState.O;
                        break;
                    default:
                        meta[r, c] = CellState.Empty;
                        break;
                }
            }

        CellState CheckLine(CellState a, CellState b, CellState c)
        {
            if (a == CellState.Empty)
                return CellState.Empty;
            return (a == b && b == c) ? a : CellState.Empty;
        }

        for (int i = 0; i < 3; i++)
        {
            var row = CheckLine(meta[i, 0], meta[i, 1], meta[i, 2]);
            var col = CheckLine(meta[0, i], meta[1, i], meta[2, i]);
            if (row != CellState.Empty)
                SetWinner(row);
            if (col != CellState.Empty)
                SetWinner(col);
        }

        var diag1 = CheckLine(meta[0, 0], meta[1, 1], meta[2, 2]);
        var diag2 = CheckLine(meta[0, 2], meta[1, 1], meta[2, 0]);

        if (diag1 != CellState.Empty)
            SetWinner(diag1);
        if (diag2 != CellState.Empty)
            SetWinner(diag2);

        if (AllBoardsComplete())
            State = BoardState.Draw;
    }
    private void SwapCurrentPlayer()
    {
        CurrentPlayer = CurrentPlayer == CellState.X ? CellState.O : CellState.X;
    }

    private void SetWinner(CellState winner)
    {
        State = winner == CellState.X ? BoardState.WonByX : BoardState.WonByO;
    }

    private bool AllBoardsComplete()
    {
        foreach (var board in Boards)
        {
            if (board.State == BoardState.Active)
                return false;
        }

        return true;
    }

    public void SetStartingPlayer(CellState startingPlayer)
    {
        CurrentPlayer = startingPlayer;
    }
}
