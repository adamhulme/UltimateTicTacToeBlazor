namespace UltimateTicTacToe.Models;

public class Cell
{
    public int Row { get; set; }
    public int Col { get; set; }
    public CellState State { get; set; } = CellState.Empty;
}
