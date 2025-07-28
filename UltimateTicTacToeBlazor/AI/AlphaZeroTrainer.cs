using System.Collections.Concurrent;
using UltimateTicTacToe.Models;

namespace UltimateTicTacToe.AI;

/// <summary>
/// AlphaZero training system for Ultimate Tic Tac Toe
/// Implements self-play data generation and neural network training
/// </summary>
public class AlphaZeroTrainer
{
    private readonly INeuralNetwork neuralNetwork;
    private readonly MCTS mcts;
    private readonly Random random;
    
    // Training parameters
    private const int SELF_PLAY_GAMES = 100;
    private const int MCTS_SIMULATIONS = 800;
    private const int TRAINING_ITERATIONS = 1000;
    private const int EXAMPLES_BUFFER_SIZE = 100000;
    private const int BATCH_SIZE = 32;
    private const int TRAINING_EPOCHS = 10;
    private const int CHECKPOINT_FREQUENCY = 10;
    
    // Temperature parameters
    private const float TEMPERATURE_THRESHOLD = 30; // First 30 moves use temperature
    private const float TEMPERATURE = 1.0f;
    
    private readonly ConcurrentQueue<TrainingExample> trainingExamples;
    private int totalGamesPlayed = 0;
    
    public AlphaZeroTrainer(INeuralNetwork neuralNetwork)
    {
        this.neuralNetwork = neuralNetwork;
        this.mcts = new MCTS(neuralNetwork);
        this.random = new Random();
        this.trainingExamples = new ConcurrentQueue<TrainingExample>();
    }
    
    /// <summary>
    /// Run the complete AlphaZero training loop
    /// </summary>
    public async Task Train(int iterations = TRAINING_ITERATIONS)
    {
        Console.WriteLine($"Starting AlphaZero training for {iterations} iterations...");
        
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            Console.WriteLine($"\n=== Training Iteration {iteration + 1}/{iterations} ===");
            
            // Self-play phase
            Console.WriteLine("Generating self-play games...");
            await GenerateSelfPlayGames(SELF_PLAY_GAMES);
            
            // Training phase
            Console.WriteLine("Training neural network...");
            TrainNeuralNetwork();
            
            // Save checkpoint
            if ((iteration + 1) % CHECKPOINT_FREQUENCY == 0)
            {
                SaveCheckpoint(iteration + 1);
            }
            
            // Print statistics
            PrintTrainingStats(iteration + 1);
        }
        
        Console.WriteLine("\nTraining completed!");
    }
    
    /// <summary>
    /// Generate self-play games for training data
    /// </summary>
    private async Task GenerateSelfPlayGames(int numGames)
    {
        var tasks = new List<Task>();
        
        // Generate games in parallel
        for (int i = 0; i < numGames; i++)
        {
            tasks.Add(Task.Run(() => PlaySelfPlayGame()));
        }
        
        await Task.WhenAll(tasks);
    }
    
    /// <summary>
    /// Play a single self-play game
    /// </summary>
    private void PlaySelfPlayGame()
    {
        var gameEngine = new UltimateTicTacToeEngine();
        var gameHistory = new List<GameState>();
        var moveCount = 0;
        
        while (!gameEngine.IsGameOver())
        {
            // Get action probabilities from MCTS
            var actionProbs = mcts.Search(gameEngine, MCTS_SIMULATIONS);
            
            // Apply temperature for move selection
            var temperature = moveCount < TEMPERATURE_THRESHOLD ? TEMPERATURE : 0.1f;
            var move = SelectMoveWithTemperature(gameEngine.GetLegalMoves(), actionProbs, temperature);
            
            // Store game state for training
            var gameState = new GameState
            {
                BoardState = (float[,,])gameEngine.ToNeuralNetworkInput().Clone(),
                ActionProbabilities = (float[])actionProbs.Clone(),
                CurrentPlayer = gameEngine.CurrentPlayer
            };
            gameHistory.Add(gameState);
            
            // Make the move
            gameEngine = gameEngine.MakeMove(move);
            moveCount++;
        }
        
        // Get game result
        var gameResult = gameEngine.GetGameResult();
        
        // Create training examples from game history
        CreateTrainingExamples(gameHistory, gameResult);
        
        Interlocked.Increment(ref totalGamesPlayed);
    }
    
    /// <summary>
    /// Select move using temperature-based sampling
    /// </summary>
    private Move SelectMoveWithTemperature(List<Move> legalMoves, float[] actionProbs, float temperature)
    {
        if (temperature == 0.0f)
        {
            // Greedy selection
            var maxProb = 0.0f;
            var bestMove = legalMoves[0];
            
            foreach (var move in legalMoves)
            {
                var prob = actionProbs[move.ToActionIndex()];
                if (prob > maxProb)
                {
                    maxProb = prob;
                    bestMove = move;
                }
            }
            
            return bestMove;
        }
        else
        {
            // Temperature-based sampling
            var temperatures = new float[legalMoves.Count];
            var sum = 0.0f;
            
            for (int i = 0; i < legalMoves.Count; i++)
            {
                var prob = actionProbs[legalMoves[i].ToActionIndex()];
                temperatures[i] = MathF.Pow(prob, 1.0f / temperature);
                sum += temperatures[i];
            }
            
            // Normalize and sample
            var randomValue = (float)random.NextDouble() * sum;
            var cumulativeSum = 0.0f;
            
            for (int i = 0; i < legalMoves.Count; i++)
            {
                cumulativeSum += temperatures[i];
                if (randomValue <= cumulativeSum)
                {
                    return legalMoves[i];
                }
            }
            
            return legalMoves[^1]; // Fallback
        }
    }
    
    /// <summary>
    /// Create training examples from game history
    /// </summary>
    private void CreateTrainingExamples(List<GameState> gameHistory, float gameResult)
    {
        for (int i = 0; i < gameHistory.Count; i++)
        {
            var gameState = gameHistory[i];
            
            // Calculate value target based on game result and current player
            var valueTarget = gameState.CurrentPlayer == CellState.X ? gameResult : -gameResult;
            
            var example = new TrainingExample(
                gameState.BoardState,
                gameState.ActionProbabilities,
                valueTarget
            );
            
            trainingExamples.Enqueue(example);
            
            // Maintain buffer size
            if (trainingExamples.Count > EXAMPLES_BUFFER_SIZE)
            {
                trainingExamples.TryDequeue(out _);
            }
        }
    }
    
    /// <summary>
    /// Train the neural network on collected examples
    /// </summary>
    private void TrainNeuralNetwork()
    {
        if (trainingExamples.Count < BATCH_SIZE)
        {
            Console.WriteLine("Not enough training examples for training.");
            return;
        }
        
        var examples = trainingExamples.ToArray();
        var batches = CreateBatches(examples, BATCH_SIZE);
        
        for (int epoch = 0; epoch < TRAINING_EPOCHS; epoch++)
        {
            // Shuffle batches
            Shuffle(batches);
            
            foreach (var batch in batches)
            {
                neuralNetwork.Train(batch);
            }
        }
    }
    
    /// <summary>
    /// Create training batches from examples
    /// </summary>
    private TrainingExample[][] CreateBatches(TrainingExample[] examples, int batchSize)
    {
        var shuffled = examples.OrderBy(x => random.Next()).ToArray();
        var numBatches = (shuffled.Length + batchSize - 1) / batchSize;
        var batches = new TrainingExample[numBatches][];
        
        for (int i = 0; i < numBatches; i++)
        {
            var start = i * batchSize;
            var end = Math.Min(start + batchSize, shuffled.Length);
            var batchLength = end - start;
            
            batches[i] = new TrainingExample[batchLength];
            Array.Copy(shuffled, start, batches[i], 0, batchLength);
        }
        
        return batches;
    }
    
    /// <summary>
    /// Shuffle array in place
    /// </summary>
    private void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
    
    /// <summary>
    /// Save training checkpoint
    /// </summary>
    private void SaveCheckpoint(int iteration)
    {
        var checkpointDir = "checkpoints";
        Directory.CreateDirectory(checkpointDir);
        
        var filename = Path.Combine(checkpointDir, $"model_iteration_{iteration}.json");
        neuralNetwork.Save(filename);
        
        Console.WriteLine($"Checkpoint saved: {filename}");
    }
    
    /// <summary>
    /// Print training statistics
    /// </summary>
    private void PrintTrainingStats(int iteration)
    {
        Console.WriteLine($"Iteration {iteration} completed:");
        Console.WriteLine($"  Total games played: {totalGamesPlayed}");
        Console.WriteLine($"  Training examples: {trainingExamples.Count}");
    }
    
    /// <summary>
    /// Get the best move for a given position
    /// </summary>
    public Move GetBestMove(UltimateTicTacToeEngine gameState, int simulations = MCTS_SIMULATIONS)
    {
        var actionProbs = mcts.Search(gameState, simulations);
        var legalMoves = gameState.GetLegalMoves();
        
        var bestMove = legalMoves[0];
        var bestProb = 0.0f;
        
        foreach (var move in legalMoves)
        {
            var prob = actionProbs[move.ToActionIndex()];
            if (prob > bestProb)
            {
                bestProb = prob;
                bestMove = move;
            }
        }
        
        return bestMove;
    }
    
    /// <summary>
    /// Evaluate a position (get win probability)
    /// </summary>
    public float EvaluatePosition(UltimateTicTacToeEngine gameState)
    {
        var (value, _) = neuralNetwork.Predict(gameState.ToNeuralNetworkInput());
        return value;
    }
}

/// <summary>
/// Game state for training data collection
/// </summary>
public class GameState
{
    public float[,,] BoardState { get; set; } = new float[0, 0, 0];
    public float[] ActionProbabilities { get; set; } = Array.Empty<float>();
    public CellState CurrentPlayer { get; set; }
}

/// <summary>
/// AlphaZero bot that can play against humans
/// </summary>
public class AlphaZeroBot
{
    private readonly AlphaZeroTrainer trainer;
    private readonly int simulations;
    
    public string Name { get; }
    public int Strength => simulations;
    
    public AlphaZeroBot(AlphaZeroTrainer trainer, int simulations = 400, string name = "AlphaZero")
    {
        this.trainer = trainer;
        this.simulations = simulations;
        Name = $"{name} ({simulations} sims)";
    }
    
    /// <summary>
    /// Get the bot's move for current position
    /// </summary>
    public Move GetMove(UltimateBoard board)
    {
        var gameEngine = UltimateTicTacToeEngine.FromUltimateBoard(board);
        return trainer.GetBestMove(gameEngine, simulations);
    }
    
    /// <summary>
    /// Get position evaluation
    /// </summary>
    public float GetEvaluation(UltimateBoard board)
    {
        var gameEngine = UltimateTicTacToeEngine.FromUltimateBoard(board);
        return trainer.EvaluatePosition(gameEngine);
    }
}
