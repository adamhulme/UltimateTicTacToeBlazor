using UltimateTicTacToe.AI;
using UltimateTicTacToe.Models;
using System.Text.Json;

namespace UltimateTicTacToeBlazor.Services;

/// <summary>
/// Service for managing AI bots, training, and model persistence
/// </summary>
public class BotService
{
    private readonly string modelsDirectory = "wwwroot/models";
    private readonly string checkpointsDirectory = "wwwroot/checkpoints";
    private AlphaZeroTrainer? trainer;
    private readonly Dictionary<string, BotInfo> availableBots = new();
    private TrainingProgress? currentTraining;

    public event Action<TrainingProgress>? TrainingProgressUpdated;
    public event Action<string>? TrainingCompleted;
    public event Action<string>? TrainingError;

    public BotService()
    {
        InitializeDirectories();
        LoadAvailableBots();
    }

    /// <summary>
    /// Get all available trained bots
    /// </summary>
    public Dictionary<string, BotInfo> GetAvailableBots() => availableBots;

    /// <summary>
    /// Get current training progress
    /// </summary>
    public TrainingProgress? GetTrainingProgress() => currentTraining;

    /// <summary>
    /// Start training a new bot
    /// </summary>
    public async Task<bool> StartTraining(TrainingConfig config)
    {
        if (currentTraining?.IsActive == true)
        {
            return false; // Training already in progress
        }

        try
        {
            currentTraining = new TrainingProgress
            {
                BotName = config.BotName,
                IsActive = true,
                StartTime = DateTime.Now,
                TargetIterations = config.Iterations,
                CurrentIteration = 0,
                Status = "Initializing..."
            };

            // Create neural network
            var neuralNetwork = new UltimateTicTacToeNeuralNetwork();
            
            // Try to load existing model if resuming training
            if (config.ResumeFromExisting && availableBots.ContainsKey(config.BotName))
            {
                var existingBot = availableBots[config.BotName];
                neuralNetwork.Load(existingBot.ModelPath);
                currentTraining.Status = "Resuming from existing model...";
            }
            else
            {
                currentTraining.Status = "Creating new neural network...";
            }

            trainer = new AlphaZeroTrainer(neuralNetwork);
            
            // Start training in background
            _ = Task.Run(async () => await RunTraining(config));
            
            return true;
        }
        catch (Exception ex)
        {
            TrainingError?.Invoke($"Failed to start training: {ex.Message}");
            currentTraining = null;
            return false;
        }
    }

    /// <summary>
    /// Stop current training
    /// </summary>
    public void StopTraining()
    {
        if (currentTraining != null)
        {
            currentTraining.IsActive = false;
            currentTraining.Status = "Stopping...";
        }
    }

    /// <summary>
    /// Create a bot for gameplay
    /// </summary>
    public async Task<AlphaZeroBot?> CreateBot(string botName, BotDifficulty difficulty)
    {
        if (!availableBots.ContainsKey(botName))
        {
            return null;
        }

        try
        {
            var botInfo = availableBots[botName];
            var neuralNetwork = new UltimateTicTacToeNeuralNetwork();
            neuralNetwork.Load(botInfo.ModelPath);
            
            var trainer = new AlphaZeroTrainer(neuralNetwork);
            var simulations = GetSimulationsForDifficulty(difficulty);
            
            return new AlphaZeroBot(trainer, simulations, $"{botName} ({difficulty})");
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Delete a trained bot
    /// </summary>
    public bool DeleteBot(string botName)
    {
        if (!availableBots.ContainsKey(botName))
        {
            return false;
        }

        try
        {
            var botInfo = availableBots[botName];
            if (File.Exists(botInfo.ModelPath))
            {
                File.Delete(botInfo.ModelPath);
            }
            if (File.Exists(botInfo.InfoPath))
            {
                File.Delete(botInfo.InfoPath);
            }

            availableBots.Remove(botName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task RunTraining(TrainingConfig config)
    {
        try
        {
            if (trainer == null || currentTraining == null)
                return;

            currentTraining.Status = "Training in progress...";
            
            for (int iteration = 1; iteration <= config.Iterations && currentTraining.IsActive; iteration++)
            {
                currentTraining.CurrentIteration = iteration;
                currentTraining.Status = $"Iteration {iteration}/{config.Iterations}";
                
                // Generate self-play games
                currentTraining.Status = $"Iteration {iteration}: Generating self-play games...";
                await Task.Delay(100); // Allow UI to update
                
                // Simplified training step (in real implementation, this would be more complex)
                await Task.Delay(1000); // Simulate training time
                
                // Update progress
                TrainingProgressUpdated?.Invoke(currentTraining);
                
                // Save checkpoint every 10 iterations
                if (iteration % 10 == 0)
                {
                    await SaveCheckpoint(config.BotName, iteration);
                }
            }

            if (currentTraining.IsActive)
            {
                // Training completed successfully
                await SaveFinalModel(config.BotName);
                currentTraining.IsActive = false;
                currentTraining.Status = "Completed";
                currentTraining.EndTime = DateTime.Now;
                
                TrainingCompleted?.Invoke(config.BotName);
            }
            else
            {
                currentTraining.Status = "Stopped";
                currentTraining.EndTime = DateTime.Now;
            }
        }
        catch (Exception ex)
        {
            if (currentTraining != null)
            {
                currentTraining.IsActive = false;
                currentTraining.Status = $"Error: {ex.Message}";
                currentTraining.EndTime = DateTime.Now;
            }
            
            TrainingError?.Invoke($"Training failed: {ex.Message}");
        }
    }

    private async Task SaveCheckpoint(string botName, int iteration)
    {
        if (trainer == null) return;

        try
        {
            var checkpointPath = Path.Combine(checkpointsDirectory, $"{botName}_iteration_{iteration}.json");
            // In a real implementation, you would save the neural network here
            // trainer.GetNeuralNetwork().Save(checkpointPath);
            await Task.Delay(100); // Simulate save time
        }
        catch (Exception ex)
        {
            TrainingError?.Invoke($"Failed to save checkpoint: {ex.Message}");
        }
    }

    private async Task SaveFinalModel(string botName)
    {
        if (trainer == null) return;

        try
        {
            var modelPath = Path.Combine(modelsDirectory, $"{botName}.json");
            var infoPath = Path.Combine(modelsDirectory, $"{botName}_info.json");
            
            // Save neural network model
            // trainer.GetNeuralNetwork().Save(modelPath);
            
            // Save bot info
            var botInfo = new BotInfo
            {
                Name = botName,
                ModelPath = modelPath,
                InfoPath = infoPath,
                CreatedDate = DateTime.Now,
                TrainingIterations = currentTraining?.TargetIterations ?? 0,
                Description = $"AlphaZero bot trained for {currentTraining?.TargetIterations} iterations"
            };

            var infoJson = JsonSerializer.Serialize(botInfo, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(infoPath, infoJson);
            
            availableBots[botName] = botInfo;
        }
        catch (Exception ex)
        {
            TrainingError?.Invoke($"Failed to save final model: {ex.Message}");
        }
    }

    private void InitializeDirectories()
    {
        Directory.CreateDirectory(modelsDirectory);
        Directory.CreateDirectory(checkpointsDirectory);
    }

    private void LoadAvailableBots()
    {
        try
        {
            var infoFiles = Directory.GetFiles(modelsDirectory, "*_info.json");
            
            foreach (var infoFile in infoFiles)
            {
                try
                {
                    var json = File.ReadAllText(infoFile);
                    var botInfo = JsonSerializer.Deserialize<BotInfo>(json);
                    
                    if (botInfo != null && File.Exists(botInfo.ModelPath))
                    {
                        availableBots[botInfo.Name] = botInfo;
                    }
                }
                catch
                {
                    // Skip invalid bot info files
                }
            }
        }
        catch
        {
            // Directory doesn't exist or other error
        }
    }

    private static int GetSimulationsForDifficulty(BotDifficulty difficulty)
    {
        return difficulty switch
        {
            BotDifficulty.Easy => 50,
            BotDifficulty.Medium => 200,
            BotDifficulty.Hard => 500,
            BotDifficulty.Expert => 1000,
            _ => 200
        };
    }
}

/// <summary>
/// Configuration for training a new bot
/// </summary>
public class TrainingConfig
{
    public string BotName { get; set; } = "";
    public int Iterations { get; set; } = 100;
    public bool ResumeFromExisting { get; set; } = false;
    public string Description { get; set; } = "";
}

/// <summary>
/// Information about a trained bot
/// </summary>
public class BotInfo
{
    public string Name { get; set; } = "";
    public string ModelPath { get; set; } = "";
    public string InfoPath { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public int TrainingIterations { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Training progress information
/// </summary>
public class TrainingProgress
{
    public string BotName { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int CurrentIteration { get; set; }
    public int TargetIterations { get; set; }
    public string Status { get; set; } = "";
    public double ProgressPercentage => TargetIterations > 0 ? (double)CurrentIteration / TargetIterations * 100 : 0;
    public TimeSpan ElapsedTime => (EndTime ?? DateTime.Now) - StartTime;
}

/// <summary>
/// Bot difficulty levels
/// </summary>
public enum BotDifficulty
{
    Easy,    // 50 simulations
    Medium,  // 200 simulations  
    Hard,    // 500 simulations
    Expert   // 1000 simulations
}
