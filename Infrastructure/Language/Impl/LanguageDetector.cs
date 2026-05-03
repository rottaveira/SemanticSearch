using Panlingo.LanguageIdentification.FastText;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Infrastructure.Language.Interfaces;

namespace Infrastructure.Language.Impl
{
    internal class LanguageDetector : ILanguageDetector
    {

        public LanguageDetector()
        {
        }

        public string? DetectLanguage(string text)
        {
            // 1. Inicializa o detector do FastText
            using var detector = new FastTextDetector();

            string baseDir = AppContext.BaseDirectory;
            var modelDir = Path.Combine(baseDir, "Language", "Model");
            var modelPath = Path.Combine(modelDir, "lid.176.ftz");

            // 2. Carrega o modelo de 176 idiomas baixado
            detector.LoadModel(modelPath);

            // 3. Prediz o idioma (solicitando a maior probabilidade)
            var pred = detector.Predict(text, count: 1).FirstOrDefault();

            // 4. Exibe os resultados
            // O FastText retorna o label no formato "__label__pt" ou "__label__en"
            string? language = pred?.Label?.Replace("__label__", "");
            return language;
        }
    }
}
