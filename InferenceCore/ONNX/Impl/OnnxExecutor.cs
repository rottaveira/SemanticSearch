using InferenceCore.NER.Interfaces;
using InferenceCore.ONNX.Interfaces;

#pragma warning disable IDE0290

namespace InferenceCore.ONNX.Impl
{
    public class OnnxExecutor : IOnnxExecutor
    {
        private readonly IOnnxInferenceEngine _embeddingInferenceEngine;
        private readonly IOnnxInferenceEngine _nerInferenceEngine;
        private readonly INerPostProcessor _nerPostProcessor;
        private string _builtPrompt = string.Empty;

        public OnnxExecutor(INerPostProcessor nerPostProcessor)
        { 
            _nerPostProcessor = nerPostProcessor;

            string baseDir = AppContext.BaseDirectory;

            // wikineural
            _nerInferenceEngine = new OnnxInferenceEngine(
                modelPath: Path.Combine(baseDir, "NER", "onnx", "model.onnx"),
                vocabPath: Path.Combine(baseDir, "NER", "onnx", "vocab.txt")
            );

            // all-MiniLM-L6-v2
            _embeddingInferenceEngine = new OnnxInferenceEngine(
                modelPath: Path.Combine(baseDir, "Embeddings", "onnx", "model.onnx"),
                vocabPath: Path.Combine(baseDir, "Embeddings", "onnx", "vocab.txt")
            );
        }

        public string BuildPrompt(string input)
        {
            _builtPrompt = $"{input} :: {_nerPostProcessor.NormalizarTextoMultilingueParaNer(input)}";
            return _builtPrompt;
        }

        public string NerInference(string prompt = null)
        {
            string textToAnalyze = prompt ?? _builtPrompt;

            if (string.IsNullOrEmpty(textToAnalyze))
                throw new InvalidOperationException("Nenhum texto fornecido para análise.");

            var (tokens, logits) = _nerInferenceEngine.ExecuteInference(textToAnalyze);

            return _nerPostProcessor.ParseModelOutput(textToAnalyze, tokens, logits);
        }

        /// <summary>
        /// Gera e normaliza o vetor de embedding para o texto fornecido.
        /// </summary>
        public float[] GenerateEmbedding(string prompt = null)
        {
            string textToAnalyze = prompt ?? _builtPrompt;

            if (string.IsNullOrEmpty(textToAnalyze))
                throw new InvalidOperationException("Nenhum texto fornecido para análise.");
             
            var(tokens, outputTensor) = _embeddingInferenceEngine.ExecuteInference(textToAnalyze);

            int embeddingDimension = outputTensor.Dimensions[2]; 
            float[] embedding = new float[embeddingDimension];
             
            for (int i = 0; i < embeddingDimension; i++)
            {
                embedding[i] = outputTensor[0, 0, i];
            }
             
            return Normalize(embedding);
        }

        private float[] Normalize(float[] vector)
        {
            double sum = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                sum += vector[i] * vector[i];
            }

            float magnitude = (float)Math.Sqrt(sum);
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] /= magnitude;
                }
            }
            return vector;
        }

        public void Dispose()
        {
            _embeddingInferenceEngine?.Dispose();
            _nerInferenceEngine?.Dispose();
        }
    }
}

#pragma warning restore IDE0290