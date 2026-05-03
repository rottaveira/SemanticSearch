using InferenceCore.ONNX.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace InferenceCore.ONNX.Impl
{
    public class OnnxInferenceEngine : IOnnxInferenceEngine
    {
        private InferenceSession _session = null!;
        private WordPieceTokenizer _tokenizer = null!;

        public OnnxInferenceEngine(string modelPath, string vocabPath)
        {
            LoadModel(modelPath, vocabPath);
        }

        private void LoadModel(string modelPath, string vocabPath)
        { 
            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Modelo não encontrado em: {modelPath}");

            if (!File.Exists(vocabPath))
                throw new FileNotFoundException($"Arquivo vocab.txt não encontrado em: {vocabPath}");

            _tokenizer = WordPieceTokenizer.Create(vocabPath);

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC;

            _session = new InferenceSession(modelPath, options);
        }

        public (IReadOnlyList<EncodedToken> Tokens, Tensor<float> Logits) ExecuteInference(string textToAnalyze)
        {
            if (_session == null || _tokenizer == null)
                throw new InvalidOperationException("O modelo não foi carregado.");

            var (inputTensor, maskTensor, typeTensor, encodedTokens) = PrepareTensors(textToAnalyze);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
            };

            using var results = _session.Run(inputs);
            var outputTensor = results.First().AsTensor<float>().Clone();

            return (encodedTokens, outputTensor);
        } 

        /// <summary>
        /// Centraliza a tokenização e criação de tensores para reaproveitamento nos dois métodos.
        /// </summary>
        private (DenseTensor<long> inputTensor, DenseTensor<long> maskTensor, DenseTensor<long> typeTensor, IReadOnlyList<EncodedToken> encodedTokens) PrepareTensors(string text)
        {
            IReadOnlyList<int> encodedIds = _tokenizer.EncodeToIds(text);
            IReadOnlyList<EncodedToken> encodedTokens = _tokenizer.EncodeToTokens(text, out _);

            var inputIds = encodedIds.Select(id => (long)id).ToList();
            if (inputIds.Count == 0 || inputIds[0] != 101) inputIds.Insert(0, 101);
            if (inputIds.Last() != 102) inputIds.Add(102);

            var attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToArray();
            var tokenTypeIds = Enumerable.Repeat(0L, inputIds.Count).ToArray();

            long[] dimensions = new long[] { 1, inputIds.Count };
            var inputTensor = new DenseTensor<long>(inputIds.ToArray(), dimensions.Select(x => (int)x).ToArray());
            var maskTensor = new DenseTensor<long>(attentionMask, dimensions.Select(x => (int)x).ToArray());
            var typeTensor = new DenseTensor<long>(tokenTypeIds, dimensions.Select(x => (int)x).ToArray());

            return (inputTensor, maskTensor, typeTensor, encodedTokens);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}