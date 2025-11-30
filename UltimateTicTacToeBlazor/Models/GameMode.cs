namespace UltimateTicTacToe.Models;

public enum GameMode
{
    Unlimited,
    Blitz,      // 30 seconds per move
    Rapid,      // 60 seconds per move
    Chess5,     // 5 minutes per player total
    Chess10,    // 10 minutes per player total
    Chess5Inc3, // 5 minutes + 3 seconds increment
    Chess10Inc5,// 10 minutes + 5 seconds increment
    Custom      // Custom total time and increment
}
