using System.Linq;
using Catalyst;
using Catalyst.Models;
using Infrastructure.NLP.Interfaces;
using Mosaik.Core;

namespace Infrastructure.NLP.Impl
{
    public class NlpService : INlpService
    {
        public NlpService()
        {
            Catalyst.Models.English.Register();
            Catalyst.Models.Portuguese.Register();
        }

        public async Task<IEnumerable<IToken>> GetProcessedTextAsync(string text, Mosaik.Core.Language lang)
        { 
            Storage.Current = new DiskStorage("catalyst-models");
            var nlp = await Pipeline.ForAsync(lang);

            var doc = new Document(text, lang);
            nlp.ProcessSingle(doc);

            return doc.FirstOrDefault().GetCapturedTokens();
        }

        public async Task<Dictionary<string, EntityType>> GetNerAsync(string text, Mosaik.Core.Language lang)
        { 
            Storage.Current = new DiskStorage("catalyst-models");
            var nlp = await Pipeline.ForAsync(lang);

            var perceptronNer = await AveragePerceptronEntityRecognizer.FromStoreAsync(
                language: lang,
                version: Mosaik.Core.Version.Latest,
                tag: "WikiNER"
            );
             
            nlp.Add(perceptronNer);
            var doc = new Document(text, lang);
            nlp.ProcessSingle(doc);

            var entities = doc.SelectMany(span => span.GetEntities());
            var entityDict = entities.ToDictionary(e => e.Value, e => e.EntityType);
            return entityDict;
        }

        public async Task<Mosaik.Core.Language> GetLanguageAsync(string text)
        {  
            Storage.Current = new DiskStorage("catalyst-models");
              
            var languageDetector = await LanguageDetector.FromStoreAsync(
                language: Mosaik.Core.Language.Any,
                version: Mosaik.Core.Version.Latest,
                tag: ""
            );  

            var doc = new Document(text);
            languageDetector.Process(doc); 

            return doc.Language;
        }
         
    }
}
