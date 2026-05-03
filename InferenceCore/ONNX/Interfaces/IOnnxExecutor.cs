namespace InferenceCore.ONNX.Interfaces
{
    public interface IOnnxExecutor : IDisposable
    {
        string BuildPrompt(string input);
        string NerInference(string prompt = null);
        float[] GenerateEmbedding(string prompt = null);
    }
}