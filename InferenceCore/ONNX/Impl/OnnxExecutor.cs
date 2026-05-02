using InferenceCore.NER.Impl;
using InferenceCore.NER.Interfaces;
using InferenceCore.ONNX.Interfaces;

#pragma warning disable IDE0290

namespace InferenceCore.ONNX.Impl
{
    public class OnnxExecutor : IOnnxExecutor
    {
        private readonly IOnnxInferenceEngine _inferenceEngine;
        private readonly INerPostProcessor _postProcessor;
        private string _builtPrompt = string.Empty;

        public OnnxExecutor(IOnnxInferenceEngine onnxInferenceEngine, INerPostProcessor nerPostProcessor)
        {
            _inferenceEngine = onnxInferenceEngine;
            _postProcessor = nerPostProcessor;
        }

        public string BuildPrompt(string input)
        {
            _builtPrompt = _postProcessor.NormalizarTextoMultilingueParaNer(input);
            return _builtPrompt;
        }

        public string PromptInference(string prompt = null)
        {
            string textToAnalyze = prompt ?? _builtPrompt;

            if (string.IsNullOrEmpty(textToAnalyze))
                throw new InvalidOperationException("Nenhum texto fornecido para análise.");

            // 1. O motor executa a inferência e retorna os dados brutos de saída
            var (tokens, logits) = _inferenceEngine.ExecuteInference(textToAnalyze);

            // 2. O processador lê esses dados e monta a saída final formatada
            return _postProcessor.ParseModelOutput(textToAnalyze, tokens, logits);
        }

        public void Dispose()
        {
            _inferenceEngine?.Dispose();
        }
    }
}

#pragma warning restore IDE0290