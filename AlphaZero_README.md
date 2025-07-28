# AlphaZero Implementation for Ultimate Tic Tac Toe

This project implements the AlphaZero algorithm to train AI bots for Ultimate Tic Tac Toe. AlphaZero combines Monte Carlo Tree Search (MCTS) with deep neural networks to learn optimal play through self-play training.

## Overview

AlphaZero is a breakthrough AI algorithm that:
- Learns to play games from scratch through self-play
- Combines neural networks with Monte Carlo Tree Search
- Requires no human knowledge or game-specific heuristics
- Has achieved superhuman performance in Chess, Go, and Shogi

## Architecture

### 1. Game Engine (`UltimateTicTacToeEngine.cs`)
Optimized game representation for AI training:
- Efficient legal move generation
- Neural network input format (11 planes of 9x9 data)
- Position hashing for transposition tables
- Game result evaluation

### 2. Neural Network (`NeuralNetwork.cs`)
Deep neural network with two heads:
- **Policy Head**: Predicts action probabilities for each possible move
- **Value Head**: Estimates win probability for the current position
- Uses 3 hidden layers with 512 neurons each
- Residual connections and ReLU activations

### 3. Monte Carlo Tree Search (`MCTS.cs`)
- Selects moves using PUCT algorithm (Predictor + UCB applied to Trees)
- Balances exploration vs exploitation
- Uses neural network to guide search
- Applies Dirichlet noise for exploration in root node

### 4. Training System (`AlphaZeroTrainer.cs`)
Complete training pipeline:
- Self-play game generation
- Training data collection and buffering
- Neural network training on collected examples
- Model checkpointing and evaluation

## How AlphaZero Works

### Training Loop
1. **Self-Play**: Bot plays games against itself using current neural network
2. **Data Collection**: Store (state, action_probabilities, game_result) tuples
3. **Training**: Update neural network on collected data
4. **Evaluation**: Test new model against previous version
5. **Repeat**: Continue for thousands of iterations

### Move Selection
1. **MCTS Simulation**: Run multiple simulations from current position
2. **Neural Network Evaluation**: Evaluate leaf positions with network
3. **Backpropagation**: Update search tree with evaluation results
4. **Move Selection**: Choose move based on visit counts

### Neural Network Input
The network receives an 11-plane representation of the board:
- Planes 0-8: One plane per small board (9 total)
  - Each plane is 9x9 showing cell states for that small board
- Plane 9: Current player indicator (all 1s for X, all 0s for O)
- Plane 10: Required board indicator (1s where play is required)

## Usage

### Training a Bot

1. **Start Training**:
```bash
cd UltimateTicTacToeBlazor/Training
dotnet run train
```

2. **Monitor Progress**:
Training creates checkpoints in the `checkpoints/` folder and saves the final model to `models/latest_model.json`.

3. **Training Parameters**:
- 1000 training iterations
- 100 self-play games per iteration
- 800 MCTS simulations per move
- 32 batch size for neural network training

### Playing Against Trained Bot

1. **Play Console Game**:
```bash
cd UltimateTicTacToeBlazor/Training
dotnet run play
```

2. **Integration with Blazor UI**:
```csharp
// Load trained model
var neuralNetwork = new UltimateTicTacToeNeuralNetwork();
neuralNetwork.Load("models/latest_model.json");

// Create bot
var trainer = new AlphaZeroTrainer(neuralNetwork);
var bot = new AlphaZeroBot(trainer, simulations: 400);

// Get bot move
var move = bot.GetMove(ultimateBoard);
var evaluation = bot.GetEvaluation(ultimateBoard);
```

## Training Performance

### Hardware Requirements
- **Minimum**: Modern CPU, 8GB RAM
- **Recommended**: GPU with CUDA support, 16GB+ RAM
- **Training Time**: 4-12 hours on CPU, 1-3 hours on GPU

### Expected Strength Progression
- **Iterations 1-100**: Random to beginner level
- **Iterations 100-500**: Intermediate play, learns basic tactics
- **Iterations 500-1000**: Strong play, advanced strategy
- **Iterations 1000+**: Expert level, near-optimal play

## Key Parameters

### MCTS Parameters
```csharp
private const float C_PUCT = 1.0f;              // Exploration constant
private const float DIRICHLET_ALPHA = 0.3f;    // Dirichlet noise parameter
private const float DIRICHLET_EPSILON = 0.25f; // Noise mixing ratio
```

### Training Parameters
```csharp
private const int SELF_PLAY_GAMES = 100;        // Games per iteration
private const int MCTS_SIMULATIONS = 800;       // Simulations per move
private const int EXAMPLES_BUFFER_SIZE = 100000; // Training data buffer
private const int BATCH_SIZE = 32;              // Neural network batch size
```

### Temperature Schedule
```csharp
private const float TEMPERATURE_THRESHOLD = 30; // First 30 moves use temperature
private const float TEMPERATURE = 1.0f;         // Exploration temperature
```

## Advanced Features

### 1. Position Evaluation
Get win probability for any position:
```csharp
float winProbability = bot.GetEvaluation(board);
// Returns value between -1 (losing) and +1 (winning)
```

### 2. Multiple Bot Strengths
Create bots with different thinking times:
```csharp
var quickBot = new AlphaZeroBot(trainer, 100);   // 100 simulations
var strongBot = new AlphaZeroBot(trainer, 1000); // 1000 simulations
```

### 3. Model Checkpointing
Training automatically saves checkpoints every 10 iterations in `checkpoints/` folder.

### 4. Self-Play Analysis
View generated training games to understand bot learning:
- Move sequences and evaluations
- Strategic pattern development
- Tactical improvement over time

## Troubleshooting

### Common Issues

1. **Memory Usage**: Reduce `EXAMPLES_BUFFER_SIZE` if running out of memory
2. **Training Speed**: Decrease `MCTS_SIMULATIONS` for faster training
3. **Model Performance**: Increase training iterations or adjust neural network architecture

### Performance Optimization

1. **Parallel Self-Play**: Games are generated in parallel by default
2. **Efficient Board Representation**: Optimized for neural network input
3. **Position Caching**: MCTS uses position hashing to avoid recomputation

## Theory and References

### AlphaZero Algorithm
Based on the paper "Mastering Chess and Shogi by Self-Play with a General Reinforcement Learning Algorithm" by Silver et al.

### Key Innovations
1. **No Human Knowledge**: Learns purely from self-play
2. **Single Neural Network**: Combined policy and value estimation
3. **MCTS Integration**: Neural network guides tree search
4. **Continuous Learning**: Improves through iterative training

### Ultimate Tic Tac Toe Specifics
- **Action Space**: 81 possible moves (3x3 boards × 3x3 cells)
- **State Space**: Approximately 10^18 possible positions
- **Game Length**: Typically 20-60 moves
- **Branching Factor**: 1-9 legal moves per position

## Future Improvements

### Performance Enhancements
1. **GPU Acceleration**: Implement CUDA neural network training
2. **Distributed Training**: Multiple machines for parallel self-play
3. **Model Architecture**: Experiment with convolutional or attention layers

### Algorithm Extensions
1. **MuZero**: Model-based planning with learned environment dynamics
2. **Multi-Agent Training**: Train against different playing styles
3. **Opening Books**: Specialized training on opening positions

### Integration Features
1. **Real-time Analysis**: Live position evaluation during human games
2. **Training Visualization**: Web interface for monitoring training progress
3. **Tournament System**: Automated bot strength evaluation
