using DICore;
using Infrastructure.Language.Impl;
using Infrastructure.Language.Interfaces;
using Infrastructure.Lucene.Index.Impl;
using Infrastructure.Lucene.Index.Interface;
using Infrastructure.NLP.Impl;
using Infrastructure.NLP.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public class Configuration
    {
        public void Configure()
        {
            var container = DIContainer.GetInstance();
            container.Register<ILuceneEngine, LuceneEngine>(ServiceLifetime.Singleton); 
            container.Register<ILanguageDetector, LanguageDetector>(ServiceLifetime.Singleton); 
            container.Register<INlpService, NlpService>(ServiceLifetime.Singleton); 
        }
    }
}
