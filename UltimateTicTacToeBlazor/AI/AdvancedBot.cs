namespace UltimateTicTacToeBlazor.AI;

using UltimateTicTacToe.Models;

public class AdvancedBot : IBot
{
    public int SearchDepth { get; }
    public AdvancedBot(int searchDepth = 3)
    {
        SearchDepth = Math.Clamp(searchDepth, 1, 5);
    }

    public (int boardRow, int boardCol, int cellRow, int cellCol)? ChooseMove(UltimateBoard game)
    {
        if (game.State != BoardState.Active) return null;
        var current = game.CurrentPlayer;
        var best = EvaluateBestMove(game, SearchDepth, current);
        return best;
    }

    private (int br, int bc, int cr, int cc)? EvaluateBestMove(UltimateBoard root, int depth, CellState player)
    {
        double bestScore = double.NegativeInfinity;
        (int br, int bc, int cr, int cc)? bestMove = null;
        foreach (var move in GenerateMoves(root, player))
        {
            var cloned = CloneBoard(root);
            cloned.MakeMove(move.br, move.bc, move.cr, move.cc);
            double score = Minimax(cloned, depth - 1, false, player, double.NegativeInfinity, double.PositiveInfinity);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = (move.br, move.bc, move.cr, move.cc);
            }
        }
        return bestMove;
    }

    private double Minimax(UltimateBoard board, int depth, bool isMaximizing, CellState maximizingPlayer, double alpha, double beta)
    {
        if (depth == 0 || board.State != BoardState.Active)
        {
            return Evaluate(board, maximizingPlayer);
        }

        var current = board.CurrentPlayer;
        var moves = GenerateMoves(board, current);
        if (!moves.Any()) return Evaluate(board, maximizingPlayer);

        if (isMaximizing)
        {
            double value = double.NegativeInfinity;
            foreach (var mv in moves)
            {
                var cloned = CloneBoard(board);
                cloned.MakeMove(mv.br, mv.bc, mv.cr, mv.cc);
                value = Math.Max(value, Minimax(cloned, depth - 1, false, maximizingPlayer, alpha, beta));
                alpha = Math.Max(alpha, value);
                if (beta <= alpha) break;
            }
            return value;
        }
        else
        {
            double value = double.PositiveInfinity;
            foreach (var mv in moves)
            {
                var cloned = CloneBoard(board);
                cloned.MakeMove(mv.br, mv.bc, mv.cr, mv.cc);
                value = Math.Min(value, Minimax(cloned, depth - 1, true, maximizingPlayer, alpha, beta));
                beta = Math.Min(beta, value);
                if (beta <= alpha) break;
            }
            return value;
        }
    }

    private IEnumerable<(int br,int bc,int cr,int cc)> GenerateMoves(UltimateBoard game, CellState player)
    {
        var moves = new List<(int br,int bc,int cr,int cc)>();
        if (game.NextBoardRow.HasValue && game.NextBoardCol.HasValue)
        {
            var br = game.NextBoardRow.Value;
            var bc = game.NextBoardCol.Value;
            var sb = game.Boards[br, bc];
            if (sb.State == BoardState.Active)
            {
                for (int i=0;i<3;i++)
                    for (int j=0;j<3;j++)
                        if (sb.Cells[i,j].State == CellState.Empty)
                            moves.Add((br,bc,i,j));
            }
            else
            {
                // free move if targeted board complete
                AddAllBoardsMoves(game, moves);
            }
        }
        else
        {
            AddAllBoardsMoves(game, moves);
        }
        return moves;
    }

    private void AddAllBoardsMoves(UltimateBoard game, List<(int br,int bc,int cr,int cc)> moves)
    {
        for (int br=0;br<3;br++)
            for (int bc=0;bc<3;bc++)
            {
                var sb = game.Boards[br, bc];
                if (sb.State != BoardState.Active) continue;
                for (int i=0;i<3;i++)
                    for (int j=0;j<3;j++)
                        if (sb.Cells[i,j].State == CellState.Empty)
                            moves.Add((br,bc,i,j));
            }
    }

    private double Evaluate(UltimateBoard board, CellState player)
    {
        // Meta-board heuristic: favor small board wins and imminent threats
        double score = 0;
        var opponent = player == CellState.X ? CellState.O : CellState.X;
        for (int r=0;r<3;r++)
            for (int c=0;c<3;c++)
            {
                var sb = board.Boards[r,c];
                score += EvaluateSmallBoard(sb, player);
                score -= EvaluateSmallBoard(sb, opponent) * 1.05; // slightly weight blocking
            }
        // Bonus if meta-board is close to win
        score += MetaWinPotential(board, player) * 5;
        score -= MetaWinPotential(board, opponent) * 6; // prioritize blocking meta

        if (board.State == BoardState.WonByX || board.State == BoardState.WonByO)
        {
            return board.State == (player == CellState.X ? BoardState.WonByX : BoardState.WonByO) ? 100000 : -100000;
        }
        if (board.State == BoardState.Draw) return 0;
        return score;
    }

    private double EvaluateSmallBoard(SmallBoard sb, CellState player)
    {
        if (sb.State == BoardState.WonByX || sb.State == BoardState.WonByO)
        {
            var winner = sb.State == BoardState.WonByX ? CellState.X : CellState.O;
            return winner == player ? 200 : -200;
        }
        if (sb.State == BoardState.Draw) return 0;
        // line potentials
        double s = 0;
        CellState Opp(CellState p) => p == CellState.X ? CellState.O : CellState.X;
        int LineScore(CellState a, CellState b, CellState c, CellState p)
        {
            int pc = (a==p?1:0) + (b==p?1:0) + (c==p?1:0);
            int oc = (a==Opp(p)?1:0) + (b==Opp(p)?1:0) + (c==Opp(p)?1:0);
            if (oc>0 && pc>0) return 0; // contested line
            if (pc==2 && oc==0) return 10; // immediate threat
            if (pc==1 && oc==0) return 3;  // potential
            if (pc==3) return 100;         // win (should be handled earlier)
            if (oc==2 && pc==0) return -9; // need to block
            if (oc==1 && pc==0) return -2;
            return 0;
        }
        // rows and cols
        for (int i=0;i<3;i++)
        {
            s += LineScore(sb.Cells[i,0].State, sb.Cells[i,1].State, sb.Cells[i,2].State, player);
            s += LineScore(sb.Cells[0,i].State, sb.Cells[1,i].State, sb.Cells[2,i].State, player);
        }
        s += LineScore(sb.Cells[0,0].State, sb.Cells[1,1].State, sb.Cells[2,2].State, player);
        s += LineScore(sb.Cells[0,2].State, sb.Cells[1,1].State, sb.Cells[2,0].State, player);
        // positional preferences
        (int r,int c)[] order = { (1,1), (0,0), (0,2), (2,0), (2,2), (0,1), (1,0), (1,2), (2,1) };
        foreach (var (r,c) in order)
        {
            if (sb.Cells[r,c].State == player) s += (r==1&&c==1) ? 2 : (r%2==0 && c%2==0 ? 1 : 0.5);
        }
        return s;
    }

    private double MetaWinPotential(UltimateBoard board, CellState player)
    {
        // treat completed small boards as markers on meta grid
        CellState[,] meta = new CellState[3,3];
        for (int r=0;r<3;r++)
            for (int c=0;c<3;c++)
            {
                meta[r,c] = board.Boards[r,c].State switch
                {
                    BoardState.WonByX => CellState.X,
                    BoardState.WonByO => CellState.O,
                    _ => CellState.Empty
                };
            }
        double s = 0;
        s += MetaLine(meta[0,0], meta[0,1], meta[0,2], player);
        s += MetaLine(meta[1,0], meta[1,1], meta[1,2], player);
        s += MetaLine(meta[2,0], meta[2,1], meta[2,2], player);
        s += MetaLine(meta[0,0], meta[1,0], meta[2,0], player);
        s += MetaLine(meta[0,1], meta[1,1], meta[2,1], player);
        s += MetaLine(meta[0,2], meta[1,2], meta[2,2], player);
        s += MetaLine(meta[0,0], meta[1,1], meta[2,2], player);
        s += MetaLine(meta[0,2], meta[1,1], meta[2,0], player);
        return s;
    }

    private double MetaLine(CellState a, CellState b, CellState c, CellState p)
    {
        int pc = (a==p?1:0) + (b==p?1:0) + (c==p?1:0);
        int oc = (a==Opp(p)?1:0) + (b==Opp(p)?1:0) + (c==Opp(p)?1:0);
        if (pc==3) return 1000;
        if (oc==3) return -1000;
        if (oc>0 && pc>0) return 0;
        if (pc==2) return 40;
        if (pc==1) return 12;
        if (oc==2) return -35;
        if (oc==1) return -10;
        return 0;
    }

    private CellState Opp(CellState p) => p == CellState.X ? CellState.O : CellState.X;

    private UltimateBoard CloneBoard(UltimateBoard original)
    {
        var copy = new UltimateBoard();
        // Copy small boards
        for (int r=0;r<3;r++)
            for (int c=0;c<3;c++)
            {
                var src = original.Boards[r,c];
                var dst = copy.Boards[r,c];
                for (int i=0;i<3;i++)
                    for (int j=0;j<3;j++)
                        dst.Cells[i,j].State = src.Cells[i,j].State;
                // copy state by recalculating from cells
                // Ensure board state matches
                if (src.State != BoardState.Active)
                {
                    // simplest: set same state by checking winner/full
                    // rely on Move/Update logic once copied; here we approximate by setting explicitly
                    typeof(SmallBoard).GetProperty("State")!.SetValue(dst, src.State);
                }
            }
        // Copy meta properties
        typeof(UltimateBoard).GetProperty("State")!.SetValue(copy, original.State);
        typeof(UltimateBoard).GetProperty("NextBoardRow")!.SetValue(copy, original.NextBoardRow);
        typeof(UltimateBoard).GetProperty("NextBoardCol")!.SetValue(copy, original.NextBoardCol);
        typeof(UltimateBoard).GetProperty("CurrentPlayer")!.SetValue(copy, original.CurrentPlayer);
        return copy;
    }
}
