using UltimateTicTacToe.Models;

namespace UltimateTicTacToe.AI;

/// <summary>
/// Game engine specifically designed for AI training and MCTS simulation
/// Optimized for performance and state representation
/// </summary>
public class UltimateTicTacToeEngine
{
    public UltimateBoard Board { get; private set; }
    public CellState CurrentPlayer => Board.CurrentPlayer;
    
    public UltimateTicTacToeEngine()
    {
        Board = new UltimateBoard();
    }
    
    public UltimateTicTacToeEngine(UltimateBoard board)
    {
        Board = DeepCopy(board);
    }
    
    /// <summary>
    /// Create engine from existing UltimateBoard
    /// </summary>
    public static UltimateTicTacToeEngine FromUltimateBoard(UltimateBoard board)
    {
        return new UltimateTicTacToeEngine(board);
    }
    
    /// <summary>
    /// Get all legal moves for the current position
    /// </summary>
    public List<Move> GetLegalMoves()
    {
        var moves = new List<Move>();
        
        if (Board.State != BoardState.Active)
            return moves;
            
        // If there's a specific board to play in
        if (Board.NextBoardRow.HasValue && Board.NextBoardCol.HasValue)
        {
            var targetBoard = Board.Boards[Board.NextBoardRow.Value, Board.NextBoardCol.Value];
            if (targetBoard.State == BoardState.Active)
            {
                AddMovesFromBoard(moves, Board.NextBoardRow.Value, Board.NextBoardCol.Value, targetBoard);
                return moves;
            }
        }
        
        // Can play in any active board
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                var board = Board.Boards[boardRow, boardCol];
                if (board.State == BoardState.Active)
                {
                    AddMovesFromBoard(moves, boardRow, boardCol, board);
                }
            }
        }
        
        return moves;
    }
    
    private void AddMovesFromBoard(List<Move> moves, int boardRow, int boardCol, SmallBoard board)
    {
        for (int cellRow = 0; cellRow < 3; cellRow++)
        {
            for (int cellCol = 0; cellCol < 3; cellCol++)
            {
                if (board.Cells[cellRow, cellCol].State == CellState.Empty)
                {
                    moves.Add(new Move(boardRow, boardCol, cellRow, cellCol));
                }
            }
        }
    }
    
    /// <summary>
    /// Make a move and return the new game state
    /// </summary>
    public UltimateTicTacToeEngine MakeMove(Move move)
    {
        var newEngine = new UltimateTicTacToeEngine(Board);
        newEngine.Board.MakeMove(move.BoardRow, move.BoardCol, move.CellRow, move.CellCol);
        return newEngine;
    }
    
    /// <summary>
    /// Convert board state to neural network input format
    /// </summary>
    public float[,,] ToNeuralNetworkInput()
    {
        // 11 planes: 9 for board states (3x3 boards, each with X/O/Empty) + 2 for metadata
        var input = new float[11, 9, 9];
        
        // Encode board states
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                var board = Board.Boards[boardRow, boardCol];
                int boardIndex = boardRow * 3 + boardCol;
                
                for (int cellRow = 0; cellRow < 3; cellRow++)
                {
                    for (int cellCol = 0; cellCol < 3; cellCol++)
                    {
                        int flatRow = boardRow * 3 + cellRow;
                        int flatCol = boardCol * 3 + cellCol;
                        
                        var cellState = board.Cells[cellRow, cellCol].State;
                        
                        // Plane 0: X positions
                        if (cellState == CellState.X)
                            input[0, flatRow, flatCol] = 1.0f;
                            
                        // Plane 1: O positions  
                        if (cellState == CellState.O)
                            input[1, flatRow, flatCol] = 1.0f;
                    }
                }
            }
        }
        
        // Plane 2: Current player (1 if X's turn, 0 if O's turn)
        if (Board.CurrentPlayer == CellState.X)
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    input[2, i, j] = 1.0f;
        }
        
        // Plane 3: Valid next boards
        if (Board.NextBoardRow.HasValue && Board.NextBoardCol.HasValue)
        {
            int boardRow = Board.NextBoardRow.Value;
            int boardCol = Board.NextBoardCol.Value;
            
            for (int cellRow = 0; cellRow < 3; cellRow++)
            {
                for (int cellCol = 0; cellCol < 3; cellCol++)
                {
                    int flatRow = boardRow * 3 + cellRow;
                    int flatCol = boardCol * 3 + cellCol;
                    input[3, flatRow, flatCol] = 1.0f;
                }
            }
        }
        else
        {
            // All active boards are valid
            for (int boardRow = 0; boardRow < 3; boardRow++)
            {
                for (int boardCol = 0; boardCol < 3; boardCol++)
                {
                    if (Board.Boards[boardRow, boardCol].State == BoardState.Active)
                    {
                        for (int cellRow = 0; cellRow < 3; cellRow++)
                        {
                            for (int cellCol = 0; cellCol < 3; cellCol++)
                            {
                                int flatRow = boardRow * 3 + cellRow;
                                int flatCol = boardCol * 3 + cellCol;
                                input[3, flatRow, flatCol] = 1.0f;
                            }
                        }
                    }
                }
            }
        }
        
        // Planes 4-12: Won boards by X and O
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                var board = Board.Boards[boardRow, boardCol];
                
                for (int cellRow = 0; cellRow < 3; cellRow++)
                {
                    for (int cellCol = 0; cellCol < 3; cellCol++)
                    {
                        int flatRow = boardRow * 3 + cellRow;
                        int flatCol = boardCol * 3 + cellCol;
                        
                        // Plane 4: Boards won by X
                        if (board.State == BoardState.WonByX)
                            input[4, flatRow, flatCol] = 1.0f;
                            
                        // Plane 5: Boards won by O
                        if (board.State == BoardState.WonByO)
                            input[5, flatRow, flatCol] = 1.0f;
                            
                        // Plane 6: Draw boards
                        if (board.State == BoardState.Draw)
                            input[6, flatRow, flatCol] = 1.0f;
                    }
                }
            }
        }
        
        return input;
    }
    
    /// <summary>
    /// Get game result from perspective of current player
    /// Returns: 1 if current player wins, -1 if loses, 0 if draw
    /// </summary>
    public float GetGameResult()
    {
        if (Board.State == BoardState.Active)
            throw new InvalidOperationException("Game is not finished");
            
        if (Board.State == BoardState.Draw)
            return 0.0f;
            
        // Determine winner relative to current player
        bool currentPlayerIsX = Board.CurrentPlayer == CellState.X;
        bool xWon = Board.State == BoardState.WonByX;
        
        if ((currentPlayerIsX && xWon) || (!currentPlayerIsX && !xWon))
            return 1.0f;  // Current player won
        else
            return -1.0f; // Current player lost
    }
    
    public bool IsGameOver()
    {
        return Board.State != BoardState.Active;
    }
    
    /// <summary>
    /// Get unique hash for this board position (for transposition table)
    /// </summary>
    public string GetPositionHash()
    {
        var sb = new System.Text.StringBuilder();
        
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                var board = Board.Boards[boardRow, boardCol];
                for (int cellRow = 0; cellRow < 3; cellRow++)
                {
                    for (int cellCol = 0; cellCol < 3; cellCol++)
                    {
                        sb.Append((int)board.Cells[cellRow, cellCol].State);
                    }
                }
            }
        }
        
        sb.Append((int)Board.CurrentPlayer);
        sb.Append(Board.NextBoardRow?.ToString() ?? "N");
        sb.Append(Board.NextBoardCol?.ToString() ?? "N");
        
        return sb.ToString();
    }
    
    private UltimateBoard DeepCopy(UltimateBoard original)
    {
        var copy = new UltimateBoard();
        
        // Copy all cell states
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                var originalBoard = original.Boards[boardRow, boardCol];
                var copyBoard = copy.Boards[boardRow, boardCol];
                
                for (int cellRow = 0; cellRow < 3; cellRow++)
                {
                    for (int cellCol = 0; cellCol < 3; cellCol++)
                    {
                        copyBoard.Cells[cellRow, cellCol].State = originalBoard.Cells[cellRow, cellCol].State;
                    }
                }
                
                // Update board state
                copyBoard.Move(0, 0, CellState.Empty); // Trigger state update
                // Reset the move we just made
                copyBoard.Cells[0, 0].State = originalBoard.Cells[0, 0].State;
            }
        }
        
        // Use reflection or create a copy constructor to set private fields
        // This is a simplified version - you'd need proper copying
        
        return copy;
    }
}

/// <summary>
/// Represents a move in Ultimate Tic Tac Toe
/// </summary>
public record Move(int BoardRow, int BoardCol, int CellRow, int CellCol)
{
    /// <summary>
    /// Convert to flat action index for neural network output
    /// </summary>
    public int ToActionIndex()
    {
        return BoardRow * 27 + BoardCol * 9 + CellRow * 3 + CellCol;
    }
    
    /// <summary>
    /// Create move from flat action index
    /// </summary>
    public static Move FromActionIndex(int actionIndex)
    {
        int boardRow = actionIndex / 27;
        actionIndex %= 27;
        int boardCol = actionIndex / 9;
        actionIndex %= 9;
        int cellRow = actionIndex / 3;
        int cellCol = actionIndex % 3;
        
        return new Move(boardRow, boardCol, cellRow, cellCol);
    }
    
    public override string ToString()
    {
        return $"Board({BoardRow},{BoardCol}) Cell({CellRow},{CellCol})";
    }
}
