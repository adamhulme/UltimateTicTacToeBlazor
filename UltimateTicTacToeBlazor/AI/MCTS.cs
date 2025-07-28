using UltimateTicTacToe.Models;

namespace UltimateTicTacToe.AI;

/// <summary>
/// Monte Carlo Tree Search implementation for Ultimate Tic Tac Toe
/// Core component of AlphaZero algorithm
/// </summary>
public class MCTS
{
    private readonly INeuralNetwork neuralNetwork;
    private readonly Dictionary<string, MCTSNode> nodeCache;
    private readonly Random random;
    
    // MCTS Parameters
    private const float C_PUCT = 1.0f; // Exploration constant
    private const float DIRICHLET_ALPHA = 0.3f;
    private const float DIRICHLET_EPSILON = 0.25f;
    
    public MCTS(INeuralNetwork neuralNetwork)
    {
        this.neuralNetwork = neuralNetwork;
        this.nodeCache = new Dictionary<string, MCTSNode>();
        this.random = new Random();
    }
    
    /// <summary>
    /// Run MCTS simulations and return action probabilities
    /// </summary>
    public float[] Search(UltimateTicTacToeEngine gameState, int numSimulations)
    {
        var rootHash = gameState.GetPositionHash();
        
        // Get or create root node
        if (!nodeCache.ContainsKey(rootHash))
        {
            var (value, policy) = neuralNetwork.Predict(gameState.ToNeuralNetworkInput());
            nodeCache[rootHash] = new MCTSNode(gameState, policy);
        }
        
        var rootNode = nodeCache[rootHash];
        
        // Add Dirichlet noise to root node for exploration
        AddDirichletNoise(rootNode);
        
        // Run simulations
        for (int i = 0; i < numSimulations; i++)
        {
            Simulate(rootNode, gameState);
        }
        
        // Return visit counts as action probabilities
        return GetActionProbabilities(rootNode, gameState);
    }
    
    /// <summary>
    /// Single MCTS simulation
    /// </summary>
    private float Simulate(MCTSNode node, UltimateTicTacToeEngine gameState)
    {
        if (gameState.IsGameOver())
        {
            return gameState.GetGameResult();
        }
        
        var positionHash = gameState.GetPositionHash();
        
        if (!node.IsExpanded)
        {
            // Leaf node - expand and evaluate
            var (value, policy) = neuralNetwork.Predict(gameState.ToNeuralNetworkInput());
            node.Expand(gameState, policy);
            node.IsExpanded = true;
            
            return value;
        }
        
        // Select best child using PUCT algorithm
        var bestMove = SelectBestMove(node, gameState);
        var newGameState = gameState.MakeMove(bestMove);
        
        // Get or create child node
        var childHash = newGameState.GetPositionHash();
        if (!nodeCache.ContainsKey(childHash))
        {
            var (value, policy) = neuralNetwork.Predict(newGameState.ToNeuralNetworkInput());
            nodeCache[childHash] = new MCTSNode(newGameState, policy);
        }
        
        var childNode = nodeCache[childHash];
        
        // Recursive simulation
        var result = -Simulate(childNode, newGameState); // Negate for opponent
        
        // Backpropagate
        node.Update(bestMove, result);
        
        return result;
    }
    
    /// <summary>
    /// Select best move using PUCT (Predictor + Upper Confidence bounds applied to Trees)
    /// </summary>
    private Move SelectBestMove(MCTSNode node, UltimateTicTacToeEngine gameState)
    {
        var legalMoves = gameState.GetLegalMoves();
        float bestScore = float.NegativeInfinity;
        Move bestMove = legalMoves[0];
        
        foreach (var move in legalMoves)
        {
            var score = CalculatePUCTScore(node, move);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        
        return bestMove;
    }
    
    /// <summary>
    /// Calculate PUCT score for move selection
    /// </summary>
    private float CalculatePUCTScore(MCTSNode node, Move move)
    {
        var actionIndex = move.ToActionIndex();
        var visits = node.VisitCounts[actionIndex];
        var totalVisits = node.TotalVisits;
        
        if (visits == 0)
        {
            // Unvisited moves get high priority
            return float.PositiveInfinity;
        }
        
        var qValue = node.QValues[actionIndex] / visits;
        var prior = node.Priors[actionIndex];
        var exploration = C_PUCT * prior * MathF.Sqrt(totalVisits) / (1 + visits);
        
        return qValue + exploration;
    }
    
    /// <summary>
    /// Add Dirichlet noise to root node for exploration
    /// </summary>
    private void AddDirichletNoise(MCTSNode rootNode)
    {
        var noise = GenerateDirichletNoise(rootNode.Priors.Length, DIRICHLET_ALPHA);
        
        for (int i = 0; i < rootNode.Priors.Length; i++)
        {
            rootNode.Priors[i] = (1 - DIRICHLET_EPSILON) * rootNode.Priors[i] + 
                                 DIRICHLET_EPSILON * noise[i];
        }
    }
    
    /// <summary>
    /// Generate Dirichlet noise
    /// </summary>
    private float[] GenerateDirichletNoise(int length, float alpha)
    {
        var noise = new float[length];
        var sum = 0.0f;
        
        for (int i = 0; i < length; i++)
        {
            // Approximate Dirichlet using Gamma distribution
            noise[i] = (float)Math.Pow(random.NextDouble(), 1.0 / alpha);
            sum += noise[i];
        }
        
        // Normalize
        for (int i = 0; i < length; i++)
        {
            noise[i] /= sum;
        }
        
        return noise;
    }
    
    /// <summary>
    /// Get action probabilities from visit counts
    /// </summary>
    private float[] GetActionProbabilities(MCTSNode rootNode, UltimateTicTacToeEngine gameState)
    {
        var legalMoves = gameState.GetLegalMoves();
        var probabilities = new float[81]; // 3*3*3*3 possible actions
        
        var totalVisits = rootNode.TotalVisits;
        if (totalVisits == 0)
        {
            // Uniform distribution if no simulations
            foreach (var move in legalMoves)
            {
                probabilities[move.ToActionIndex()] = 1.0f / legalMoves.Count;
            }
        }
        else
        {
            foreach (var move in legalMoves)
            {
                var actionIndex = move.ToActionIndex();
                probabilities[actionIndex] = (float)rootNode.VisitCounts[actionIndex] / totalVisits;
            }
        }
        
        return probabilities;
    }
    
    public void ClearCache()
    {
        nodeCache.Clear();
    }
}

/// <summary>
/// MCTS Node representing a game state
/// </summary>
public class MCTSNode
{
    public float[] QValues { get; private set; }      // Action value estimates
    public int[] VisitCounts { get; private set; }   // Visit counts for each action
    public float[] Priors { get; set; }              // Prior probabilities from neural network
    public int TotalVisits { get; private set; }
    public bool IsExpanded { get; set; }
    
    private readonly int numActions = 81; // 3*3*3*3 possible actions
    
    public MCTSNode(UltimateTicTacToeEngine gameState, float[] priors)
    {
        QValues = new float[numActions];
        VisitCounts = new int[numActions];
        Priors = new float[numActions];
        
        // Set priors only for legal moves
        var legalMoves = gameState.GetLegalMoves();
        foreach (var move in legalMoves)
        {
            var actionIndex = move.ToActionIndex();
            Priors[actionIndex] = priors[actionIndex];
        }
        
        // Normalize priors
        NormalizePriors(legalMoves);
        
        TotalVisits = 0;
        IsExpanded = false;
    }
    
    public void Expand(UltimateTicTacToeEngine gameState, float[] networkPriors)
    {
        var legalMoves = gameState.GetLegalMoves();
        
        // Update priors with network output
        foreach (var move in legalMoves)
        {
            var actionIndex = move.ToActionIndex();
            Priors[actionIndex] = networkPriors[actionIndex];
        }
        
        NormalizePriors(legalMoves);
    }
    
    public void Update(Move move, float value)
    {
        var actionIndex = move.ToActionIndex();
        QValues[actionIndex] += value;
        VisitCounts[actionIndex]++;
        TotalVisits++;
    }
    
    private void NormalizePriors(List<Move> legalMoves)
    {
        var sum = 0.0f;
        foreach (var move in legalMoves)
        {
            sum += Priors[move.ToActionIndex()];
        }
        
        if (sum > 0)
        {
            foreach (var move in legalMoves)
            {
                var actionIndex = move.ToActionIndex();
                Priors[actionIndex] /= sum;
            }
        }
        else
        {
            // Uniform distribution if all priors are zero
            foreach (var move in legalMoves)
            {
                Priors[move.ToActionIndex()] = 1.0f / legalMoves.Count;
            }
        }
    }
}

/// <summary>
/// Interface for neural network
/// </summary>
public interface INeuralNetwork
{
    /// <summary>
    /// Predict value and policy for given board state
    /// </summary>
    /// <param name="boardState">Board state as 3D tensor</param>
    /// <returns>Tuple of (value, policy) where value is win probability and policy is action probabilities</returns>
    (float value, float[] policy) Predict(float[,,] boardState);
    
    /// <summary>
    /// Train the network on a batch of examples
    /// </summary>
    void Train(TrainingExample[] examples);
    
    /// <summary>
    /// Save the network to file
    /// </summary>
    void Save(string filepath);
    
    /// <summary>
    /// Load the network from file
    /// </summary>
    void Load(string filepath);
}

/// <summary>
/// Training example for neural network
/// </summary>
public record TrainingExample(
    float[,,] BoardState,    // Input board state
    float[] PolicyTarget,    // Target policy (MCTS visit counts)
    float ValueTarget        // Target value (game result)
);
