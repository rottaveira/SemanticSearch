using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace InferenceCore.NER.Interfaces
{
    public interface INerPostProcessor
    {
        string NormalizarTextoMultilingueParaNer(string texto);
        string ParseModelOutput(string originalText, IReadOnlyList<EncodedToken> encodedTokens, Tensor<float> logits);
    }
}