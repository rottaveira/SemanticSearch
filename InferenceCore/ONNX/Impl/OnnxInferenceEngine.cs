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

        public OnnxInferenceEngine()
        {
            LoadModel();
        }

        private void LoadModel()
        {
            string baseDir = AppContext.BaseDirectory;
            var modelDir = Path.Combine(baseDir, "NER", "onnx");

            var modelPath = Path.Combine(modelDir, "model.onnx");
            var vocabPath = Path.Combine(modelDir, "vocab.txt");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException($"Modelo não encontrado em: {modelPath}");

            if (!File.Exists(vocabPath))
                throw new FileNotFoundException($"Arquivo vocab.txt não encontrado em: {vocabPath}");

            _tokenizer = WordPieceTokenizer.Create(vocabPath);

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_BASIC;

            _session = new InferenceSession(modelPath, options);
        }

        /// <summary>
        /// Executa a inferência no ONNX e retorna os tokens e o Tensor de saída (logits).
        /// </summary>
        public (IReadOnlyList<EncodedToken> Tokens, Tensor<float> Logits) ExecuteInference(string textToAnalyze)
        {
            if (_session == null || _tokenizer == null)
                throw new InvalidOperationException("O modelo não foi carregado.");

            // 1. Obtém os IDs e os Tokens detalhados usando a API oficial do Tokenizer
            IReadOnlyList<int> encodedIds = _tokenizer.EncodeToIds(textToAnalyze);
            IReadOnlyList<EncodedToken> encodedTokens = _tokenizer.EncodeToTokens(textToAnalyze, out _);

            // 2. Garante os tokens de controle [CLS] (101) e [SEP] (102)
            var inputIds = encodedIds.Select(id => (long)id).ToList();
            if (inputIds.Count == 0 || inputIds[0] != 101) inputIds.Insert(0, 101);
            if (inputIds.Last() != 102) inputIds.Add(102);

            // 3. Cria as máscaras necessárias pelo BERT
            var attentionMask = Enumerable.Repeat(1L, inputIds.Count).ToArray();
            var tokenTypeIds = Enumerable.Repeat(0L, inputIds.Count).ToArray();

            long[] dimensions = new long[] { 1, inputIds.Count };
            var inputTensor = new DenseTensor<long>(inputIds.ToArray(), dimensions.Select(x => (int)x).ToArray());
            var maskTensor = new DenseTensor<long>(attentionMask, dimensions.Select(x => (int)x).ToArray());
            var typeTensor = new DenseTensor<long>(tokenTypeIds, dimensions.Select(x => (int)x).ToArray());

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", maskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", typeTensor)
            };

            // 4. Executa a inferência
            using var results = _session.Run(inputs);
            var outputTensor = results.First().AsTensor<float>().Clone(); 

            return (encodedTokens, outputTensor);
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}