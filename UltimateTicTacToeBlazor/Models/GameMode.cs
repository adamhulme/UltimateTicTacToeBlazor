namespace UltimateTicTacToe.Models;

public enum GameMode
{
    Unlimited,
    Blitz,      // 30 seconds per move
    Rapid,      // 60 seconds per move
    Classical,  // 3 minutes per move
    Chess5,     // 5 minutes per player total
    Chess10,    // 10 minutes per player total
    Chess15     // 15 minutes per player total
}
