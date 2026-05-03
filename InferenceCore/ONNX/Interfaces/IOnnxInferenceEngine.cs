using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace InferenceCore.ONNX.Interfaces
{
    public interface IOnnxInferenceEngine: IDisposable
    {
        (IReadOnlyList<EncodedToken> Tokens, Tensor<float> Logits) ExecuteInference(string textToAnalyze); 
    }
}