using Domain.Search.Interfaces;
using Infrastructure.Language.Interfaces;
using Infrastructure.Lucene.Index.Interface;
using Payload.Document;

namespace Domain.Search.Impl
{
    public class SearchManager: ISearchManager
    {
        private readonly ILuceneEngine _luceneEngine;
        private readonly ILanguageDetector _languageDetector;

        public SearchManager(ILuceneEngine luceneEngine, ILanguageDetector languageDetector)
        {
            _languageDetector = languageDetector;
            _luceneEngine = luceneEngine;
        }

        public List<DocumentModel> GetAll() => _luceneEngine.GetAllDocuments();

        public List<DocumentModel> Search(string sentence)
        {
            string? language = _languageDetector.DetectLanguage(sentence);
            Console.WriteLine($"Detected language: {language}");
            throw new NotImplementedException();

        }
    }
}
