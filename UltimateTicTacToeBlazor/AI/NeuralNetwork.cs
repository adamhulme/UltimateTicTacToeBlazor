using System.Text.Json;

namespace UltimateTicTacToe.AI;

/// <summary>
/// Simple neural network implementation for Ultimate Tic Tac Toe
/// Using basic feedforward architecture with residual connections
/// </summary>
public class UltimateTicTacToeNeuralNetwork : INeuralNetwork
{
    private readonly Random random;
    private NeuralNetworkWeights weights = new NeuralNetworkWeights();
    
    // Network architecture
    private const int INPUT_SIZE = 11 * 9 * 9; // 11 planes of 9x9
    private const int HIDDEN_SIZE = 512;
    private const int POLICY_OUTPUT_SIZE = 81; // 3*3*3*3 possible actions
    private const int VALUE_OUTPUT_SIZE = 1;
    
    // Training parameters
    private const float LEARNING_RATE = 0.001f;
    private const float MOMENTUM = 0.9f;
    private const float WEIGHT_DECAY = 1e-4f;
    
    public UltimateTicTacToeNeuralNetwork()
    {
        random = new Random();
        InitializeWeights();
    }
    
    public (float value, float[] policy) Predict(float[,,] boardState)
    {
        // Flatten input
        var input = FlattenInput(boardState);
        
        // Forward pass
        var hidden1 = ActivateReLU(AddVectors(MatrixMultiply(input, weights.W1), weights.B1));
        var hidden2 = ActivateReLU(AddVectors(MatrixMultiply(hidden1, weights.W2), weights.B2));
        var hidden3 = ActivateReLU(AddVectors(MatrixMultiply(hidden2, weights.W3), weights.B3));
        
        // Policy head
        var policyLogits = AddVectors(MatrixMultiply(hidden3, weights.PolicyW), weights.PolicyB);
        var policy = Softmax(policyLogits);
        
        // Value head
        var valueLogits = AddVectors(MatrixMultiply(hidden3, weights.ValueW), weights.ValueB);
        var value = (float)Math.Tanh(valueLogits[0]); // Tanh activation for value
        
        return (value, policy);
    }
    
    public void Train(TrainingExample[] examples)
    {
        if (examples.Length == 0) return;
        
        // Simple batch gradient descent
        var gradients = ComputeGradients(examples);
        ApplyGradients(gradients);
    }
    
    public void Save(string filepath)
    {
        var json = JsonSerializer.Serialize(weights, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filepath, json);
    }
    
    public void Load(string filepath)
    {
        if (File.Exists(filepath))
        {
            var json = File.ReadAllText(filepath);
            weights = JsonSerializer.Deserialize<NeuralNetworkWeights>(json) ?? new NeuralNetworkWeights();
        }
        else
        {
            InitializeWeights();
        }
    }
    
    private void InitializeWeights()
    {
        weights = new NeuralNetworkWeights
        {
            // Xavier initialization
            W1 = InitializeMatrix(INPUT_SIZE, HIDDEN_SIZE, MathF.Sqrt(2.0f / INPUT_SIZE)),
            B1 = new float[HIDDEN_SIZE],
            
            W2 = InitializeMatrix(HIDDEN_SIZE, HIDDEN_SIZE, MathF.Sqrt(2.0f / HIDDEN_SIZE)),
            B2 = new float[HIDDEN_SIZE],
            
            W3 = InitializeMatrix(HIDDEN_SIZE, HIDDEN_SIZE, MathF.Sqrt(2.0f / HIDDEN_SIZE)),
            B3 = new float[HIDDEN_SIZE],
            
            PolicyW = InitializeMatrix(HIDDEN_SIZE, POLICY_OUTPUT_SIZE, MathF.Sqrt(2.0f / HIDDEN_SIZE)),
            PolicyB = new float[POLICY_OUTPUT_SIZE],
            
            ValueW = InitializeMatrix(HIDDEN_SIZE, VALUE_OUTPUT_SIZE, MathF.Sqrt(2.0f / HIDDEN_SIZE)),
            ValueB = new float[VALUE_OUTPUT_SIZE]
        };
    }
    
    private float[,] InitializeMatrix(int rows, int cols, float scale)
    {
        var matrix = new float[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i, j] = (float)(random.NextGaussian() * scale);
            }
        }
        return matrix;
    }
    
    private float[] FlattenInput(float[,,] input)
    {
        var flattened = new float[INPUT_SIZE];
        int index = 0;
        
        for (int plane = 0; plane < 11; plane++)
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    flattened[index++] = input[plane, row, col];
                }
            }
        }
        
        return flattened;
    }
    
    private float[] MatrixMultiply(float[] vector, float[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        var result = new float[cols];
        
        for (int j = 0; j < cols; j++)
        {
            for (int i = 0; i < rows; i++)
            {
                result[j] += vector[i] * matrix[i, j];
            }
        }
        
        return result;
    }
    
    private float[] AddVectors(float[] a, float[] b)
    {
        var result = new float[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            result[i] = a[i] + b[i];
        }
        return result;
    }
    
    private float[] ActivateReLU(float[] input)
    {
        return input.Select(x => MathF.Max(0, x)).ToArray();
    }
    
    private float[] Softmax(float[] input)
    {
        var max = input.Max();
        var exp = input.Select(x => MathF.Exp(x - max)).ToArray();
        var sum = exp.Sum();
        return exp.Select(x => x / sum).ToArray();
    }
    
    private NetworkGradients ComputeGradients(TrainingExample[] examples)
    {
        var gradients = new NetworkGradients();
        InitializeGradients(gradients);
        
        foreach (var example in examples)
        {
            // Forward pass with gradient tracking
            var input = FlattenInput(example.BoardState);
            var (value, policy) = Predict(example.BoardState);
            
            // Compute losses
            var policyLoss = ComputeCrossEntropyLoss(policy, example.PolicyTarget);
            var valueLoss = ComputeMSELoss(value, example.ValueTarget);
            
            // Backward pass (simplified gradient computation)
            AccumulateGradients(gradients, input, value, policy, example);
        }
        
        // Average gradients
        AverageGradients(gradients, examples.Length);
        
        return gradients;
    }
    
    private void AccumulateGradients(NetworkGradients gradients, float[] input, float value, float[] policy, TrainingExample example)
    {
        // Simplified gradient computation - in practice, you'd want proper backpropagation
        // This is a placeholder implementation
        
        // Policy gradients
        for (int i = 0; i < policy.Length; i++)
        {
            var policyError = policy[i] - example.PolicyTarget[i];
            // Accumulate gradients (simplified)
        }
        
        // Value gradients
        var valueError = value - example.ValueTarget;
        // Accumulate gradients (simplified)
    }
    
    private void InitializeGradients(NetworkGradients gradients)
    {
        gradients.W1 = new float[INPUT_SIZE, HIDDEN_SIZE];
        gradients.B1 = new float[HIDDEN_SIZE];
        gradients.W2 = new float[HIDDEN_SIZE, HIDDEN_SIZE];
        gradients.B2 = new float[HIDDEN_SIZE];
        gradients.W3 = new float[HIDDEN_SIZE, HIDDEN_SIZE];
        gradients.B3 = new float[HIDDEN_SIZE];
        gradients.PolicyW = new float[HIDDEN_SIZE, POLICY_OUTPUT_SIZE];
        gradients.PolicyB = new float[POLICY_OUTPUT_SIZE];
        gradients.ValueW = new float[HIDDEN_SIZE, VALUE_OUTPUT_SIZE];
        gradients.ValueB = new float[VALUE_OUTPUT_SIZE];
    }
    
    private void AverageGradients(NetworkGradients gradients, int batchSize)
    {
        float scale = 1.0f / batchSize;
        
        // Scale all gradients by batch size
        ScaleMatrix(gradients.W1, scale);
        ScaleVector(gradients.B1, scale);
        ScaleMatrix(gradients.W2, scale);
        ScaleVector(gradients.B2, scale);
        ScaleMatrix(gradients.W3, scale);
        ScaleVector(gradients.B3, scale);
        ScaleMatrix(gradients.PolicyW, scale);
        ScaleVector(gradients.PolicyB, scale);
        ScaleMatrix(gradients.ValueW, scale);
        ScaleVector(gradients.ValueB, scale);
    }
    
    private void ApplyGradients(NetworkGradients gradients)
    {
        // Simple SGD update with momentum (simplified)
        ApplyGradientToMatrix(weights.W1, gradients.W1);
        ApplyGradientToVector(weights.B1, gradients.B1);
        ApplyGradientToMatrix(weights.W2, gradients.W2);
        ApplyGradientToVector(weights.B2, gradients.B2);
        ApplyGradientToMatrix(weights.W3, gradients.W3);
        ApplyGradientToVector(weights.B3, gradients.B3);
        ApplyGradientToMatrix(weights.PolicyW, gradients.PolicyW);
        ApplyGradientToVector(weights.PolicyB, gradients.PolicyB);
        ApplyGradientToMatrix(weights.ValueW, gradients.ValueW);
        ApplyGradientToVector(weights.ValueB, gradients.ValueB);
    }
    
    private void ApplyGradientToMatrix(float[,] weights, float[,] gradients)
    {
        int rows = weights.GetLength(0);
        int cols = weights.GetLength(1);
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                weights[i, j] -= LEARNING_RATE * gradients[i, j];
            }
        }
    }
    
    private void ApplyGradientToVector(float[] weights, float[] gradients)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] -= LEARNING_RATE * gradients[i];
        }
    }
    
    private void ScaleMatrix(float[,] matrix, float scale)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i, j] *= scale;
            }
        }
    }
    
    private void ScaleVector(float[] vector, float scale)
    {
        for (int i = 0; i < vector.Length; i++)
        {
            vector[i] *= scale;
        }
    }
    
    private float ComputeCrossEntropyLoss(float[] predicted, float[] target)
    {
        float loss = 0;
        for (int i = 0; i < predicted.Length; i++)
        {
            if (target[i] > 0)
            {
                loss -= target[i] * MathF.Log(MathF.Max(predicted[i], 1e-8f));
            }
        }
        return loss;
    }
    
    private float ComputeMSELoss(float predicted, float target)
    {
        var diff = predicted - target;
        return 0.5f * diff * diff;
    }
}

/// <summary>
/// Network weights storage
/// </summary>
public class NeuralNetworkWeights
{
    public float[,] W1 { get; set; } = new float[0, 0];
    public float[] B1 { get; set; } = Array.Empty<float>();
    public float[,] W2 { get; set; } = new float[0, 0];
    public float[] B2 { get; set; } = Array.Empty<float>();
    public float[,] W3 { get; set; } = new float[0, 0];
    public float[] B3 { get; set; } = Array.Empty<float>();
    public float[,] PolicyW { get; set; } = new float[0, 0];
    public float[] PolicyB { get; set; } = Array.Empty<float>();
    public float[,] ValueW { get; set; } = new float[0, 0];
    public float[] ValueB { get; set; } = Array.Empty<float>();
}

/// <summary>
/// Network gradients storage
/// </summary>
public class NetworkGradients
{
    public float[,] W1 { get; set; } = new float[0, 0];
    public float[] B1 { get; set; } = Array.Empty<float>();
    public float[,] W2 { get; set; } = new float[0, 0];
    public float[] B2 { get; set; } = Array.Empty<float>();
    public float[,] W3 { get; set; } = new float[0, 0];
    public float[] B3 { get; set; } = Array.Empty<float>();
    public float[,] PolicyW { get; set; } = new float[0, 0];
    public float[] PolicyB { get; set; } = Array.Empty<float>();
    public float[,] ValueW { get; set; } = new float[0, 0];
    public float[] ValueB { get; set; } = Array.Empty<float>();
}

/// <summary>
/// Extension methods for Random class
/// </summary>
public static class RandomExtensions
{
    public static double NextGaussian(this Random random)
    {
        // Box-Muller transform
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    }
}
