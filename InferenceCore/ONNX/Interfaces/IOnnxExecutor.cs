namespace InferenceCore.ONNX.Interfaces
{
    public interface IOnnxExecutor : IDisposable
    {
        string BuildPrompt(string input);
        string PromptInference(string prompt = null);
    }
}