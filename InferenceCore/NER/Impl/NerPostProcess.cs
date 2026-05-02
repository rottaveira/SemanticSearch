using InferenceCore.Entity;
using InferenceCore.NER.Interfaces;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using System.Text.Json;

namespace InferenceCore.NER.Impl
{
    public class NerPostProcessor : INerPostProcessor
    {
        // As categorias de entidades reconhecidas pelo modelo ONNX exportado do Hugging Face (Babelscape/wikineural-multilingual-ner).
        // optimum-cli export onnx --model Babelscape/wikineural-multilingual-ner --task token-classification ./multilingual-onnx/
        // O modelo é treinado para reconhecer as seguintes categorias de entidades:
        private readonly string[] _labels = new string[]
        {
            "O", "B-PER", "I-PER", "B-ORG", "I-ORG", "B-LOC", "I-LOC", "B-MISC", "I-MISC"
        };

        /// <summary>
        /// Aplica Truecasing multilíngue para garantir que o modelo identifique entidades.
        /// </summary>
        public string NormalizarTextoMultilingueParaNer(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;

            var palavras = texto.Split(' ');

            for (int i = 0; i < palavras.Length; i++)
            {
                string palavraOriginal = palavras[i];

                if (palavraOriginal.Length <= 2) continue;

                string palavraLimpa = new string(palavraOriginal.Where(c => !char.IsPunctuation(c)).ToArray());

                if (string.IsNullOrEmpty(palavraLimpa) || char.IsUpper(palavraLimpa[0]) || char.IsDigit(palavraLimpa[0]))
                {
                    continue;
                }

                palavras[i] = char.ToUpper(palavraOriginal[0]) + palavraOriginal.Substring(1);
            }

            return string.Join(" ", palavras);
        }

        /// <summary>
        /// Converte os logits do ONNX de volta em objetos C# serializados como JSON.
        /// </summary>
        public string ParseModelOutput(string originalText, IReadOnlyList<EncodedToken> encodedTokens, Tensor<float> logits)
        {
            var rawEntities = new List<(string Word, string Tag)>();
            int numLabels = _labels.Length;

            int sequenceLength = logits.Dimensions[1];
            int labelsCount = logits.Dimensions[2];

            string currentWord = "";
            string currentTag = "O";

            for (int i = 1; i < Math.Min(encodedTokens.Count + 1, sequenceLength); i++)
            {
                var token = encodedTokens[i - 1];

                if (token.Id == 101 || token.Id == 102 || token.Value == "[CLS]" || token.Value == "[SEP]") continue;

                var offset = token.Offset;
                if (offset.End.Value <= offset.Start.Value) continue;

                string wordValue = originalText.Substring(offset.Start.Value, offset.End.Value - offset.Start.Value);

                int maxLabelIndex = 0;
                float maxLogit = float.MinValue;

                for (int j = 0; j < Math.Min(numLabels, labelsCount); j++)
                {
                    float logit = logits[0, i, j];
                    if (logit > maxLogit)
                    {
                        maxLogit = logit;
                        maxLabelIndex = j;
                    }
                }

                string predictedTag = _labels[maxLabelIndex];

                if (token.Value.StartsWith("##"))
                {
                    currentWord += wordValue.Replace("##", "");
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentWord) && currentTag != "O")
                    {
                        string cleanWord = new string(currentWord.Where(c => !char.IsPunctuation(c)).ToArray());
                        if (!string.IsNullOrWhiteSpace(cleanWord))
                        {
                            rawEntities.Add((cleanWord, currentTag));
                        }
                    }

                    currentWord = wordValue;
                    currentTag = predictedTag;
                }
            }

            if (!string.IsNullOrEmpty(currentWord) && currentTag != "O")
            {
                string cleanWord = new string(currentWord.Where(c => !char.IsPunctuation(c)).ToArray());
                if (!string.IsNullOrWhiteSpace(cleanWord))
                {
                    rawEntities.Add((cleanWord, currentTag));
                }
            }

            var mergedEntities = new List<NerEntity>();
            string textAcumulado = "";
            string categoriaAcumulada = "";

            foreach (var (word, tag) in rawEntities)
            {
                string cleanCategory = tag.Replace("B-", "").Replace("I-", "");
                string portugueseCategory = MapToPortugueseCategory(cleanCategory);

                if (!string.IsNullOrEmpty(textAcumulado) && tag.StartsWith("I-") && portugueseCategory == categoriaAcumulada)
                {
                    textAcumulado += " " + word;
                }
                else
                {
                    if (!string.IsNullOrEmpty(textAcumulado))
                    {
                        AddCleanEntity(mergedEntities, textAcumulado, categoriaAcumulada);
                    }
                    textAcumulado = word;
                    categoriaAcumulada = portugueseCategory;
                }
            }

            if (!string.IsNullOrEmpty(textAcumulado))
            {
                AddCleanEntity(mergedEntities, textAcumulado, categoriaAcumulada);
            }

            var response = new NerResponse { Entities = mergedEntities };
            return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        }

        private void AddCleanEntity(List<NerEntity> list, string text, string category)
        {
            if (!string.IsNullOrEmpty(text) && !list.Any(e => e.Value.Equals(text, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(new NerEntity { Value = text, Category = category });
            }
        }

        private string MapToPortugueseCategory(string category)
        {
            return category switch
            {
                "PER" => "PESSOA",
                "LOC" => "LOCAL",
                "ORG" => "ORGANIZACAO",
                _ => category
            };
        }
    }
}