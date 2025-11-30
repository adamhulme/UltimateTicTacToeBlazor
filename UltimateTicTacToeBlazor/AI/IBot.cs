namespace UltimateTicTacToeBlazor.AI;

using UltimateTicTacToe.Models;

public interface IBot
{
    (int boardRow, int boardCol, int cellRow, int cellCol)? ChooseMove(UltimateBoard game);
}
